using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams.IO;
using Snd.Sdk.Kubernetes.Base;
using Snd.Sdk.Kubernetes.Exceptions;
using Snd.Sdk.Kubernetes.Streaming.Sources;
using Snd.Sdk.Storage.Base;

namespace Snd.Sdk.Kubernetes
{
    /// <inheritdoc />
    public class KubernetesCluster : IKubeCluster
    {
        /// <summary>
        /// Internal logger instance.
        /// </summary>
        protected readonly ILogger<KubernetesCluster> logger;

        /// <summary>
        /// Reference to a ReadWriteMany file system mount used by this fleet member.
        /// </summary>
        protected ISharedFileSystemService sharedFileSystem;

        /// <summary>
        /// Create an instance of <see cref="KubernetesCluster"/> with KubeApi ready for use.
        /// </summary>
        /// <param name="kubeConfigLocation"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public KubernetesCluster(string kubeConfigLocation, ILoggerFactory loggerFactory)
        {
            var k8sConf = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigLocation);
            this.ClusterName = k8sConf.CurrentContext;
            this.AccessToken = k8sConf.AccessToken;
            this.SslCaCerts = k8sConf.SslCaCerts;

            this.KubeApi = new k8s.Kubernetes(k8sConf);
            this.logger = loggerFactory.CreateLogger<KubernetesCluster>();
        }

        private KubernetesCluster(string name, IKubernetes api, ILoggerFactory loggerFactory)
        {
            this.ClusterName = name;
            this.KubeApi = api;
            this.logger = loggerFactory.CreateLogger<KubernetesCluster>();
        }

        /// <summary>
        /// Allows instantiation of KubeFleetMember directly from k8s api proxy.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="api"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static KubernetesCluster CreateFromApi(string name, IKubernetes api, ILoggerFactory loggerFactory) =>
            new KubernetesCluster(name, api, loggerFactory);

        /// <summary>
        /// Kubernetes API client for this member.
        /// </summary>
        public IKubernetes KubeApi { get; }

        /// <inheritdoc />
        public virtual ISharedFileSystemService SharedFileSystem()
        {
            throw new SharedFileSystemNotInitializedException(
                "Shared filesystem not supported for this fleet type. Please use one of the fleet types that supports external provider for a ReadWriteMany mount.");
        }

        /// <inheritdoc />
        public Task<V1JobStatus> SendJob(V1Job job, string jobNamespace, CancellationToken cancellationToken = default)
        {
            var sendJobCall = (CancellationToken ct) => this.KubeApi.BatchV1
                .CreateNamespacedJobAsync(job, jobNamespace, cancellationToken: ct)
                .Map(result => result.Status);
            return sendJobCall.RetryConnectionError(this.logger, cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<V1Node> GetNodes(CancellationToken cancellationToken = default)
        {
            return this.KubeApi.CoreV1.ListNodeAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            ).GetAwaiter().GetResult().Items;
        }

        /// <inheritdoc />
        public IEnumerable<NodeMetrics> GetNodeMetrics(CancellationToken cancellationToken = default)
        {
            return ((JsonElement)this.KubeApi.CustomObjects.GetClusterCustomObjectAsync(
                "metrics.k8s.io",
                "v1beta1",
                "nodes",
                string.Empty,
                cancellationToken
            ).GetAwaiter().GetResult()).Deserialize<NodeMetricsList>().Items;
        }

        /// <inheritdoc />
        public Task<IEnumerable<NodeMetrics>> GetNodeMetricsAsync(CancellationToken cancellationToken = default)
        {
            var metricsListApiCall = (CancellationToken ct) => this.KubeApi.CustomObjects.GetClusterCustomObjectAsync(
                "metrics.k8s.io",
                "v1beta1",
                "nodes",
                string.Empty,
                ct
            ).TryMap(result => ((JsonElement)result).Deserialize<NodeMetricsList>().Items, errorHandler: exception =>
            {
                this.logger.LogError(exception, "Failed to list Node metrics");
                return Enumerable.Empty<NodeMetrics>();
            });

            return metricsListApiCall.RetryHttp429(this.logger, cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<PodMetrics> GetPodMetrics(CancellationToken cancellationToken = default)
        {
            return ((JsonElement)this.KubeApi.CustomObjects.GetClusterCustomObjectAsync(
                "metrics.k8s.io",
                "v1beta1",
                "pods",
                string.Empty,
                cancellationToken
            ).GetAwaiter().GetResult()).Deserialize<PodMetricsList>().Items;
        }

        /// <inheritdoc />
        public Task<IEnumerable<PodMetrics>> GetPodMetricsAsync(CancellationToken cancellationToken = default)
        {
            var metricsListApiCall = (CancellationToken ct) => this.KubeApi.CustomObjects.GetClusterCustomObjectAsync(
                "metrics.k8s.io",
                "v1beta1",
                "pods",
                string.Empty,
                ct
            ).TryMap(result => ((JsonElement)result).Deserialize<PodMetricsList>().Items, exception =>
            {
                this.logger.LogError(exception, "Failed to list Pod metrics");
                return Enumerable.Empty<PodMetrics>();
            });

            return metricsListApiCall.RetryHttp429(this.logger, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> GetCustomResource<T>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            string name,
            Func<JsonElement, T> converter = null,
            CancellationToken cancellationToken = default)
        {
            return this.KubeApi.CustomObjects.GetNamespacedCustomObjectAsync(
                group,
                version,
                crdNamespace,
                plural,
                name,
                cancellationToken).TryMap(
                result => converter == null ? ((JsonElement)result).Deserialize<T>() : converter((JsonElement)result),
                exception =>
                {
                    this.logger.LogError(exception,
                        "Failed to read a resource {crdNamespace}/{group}/{version}/{plural}/{name}",
                        crdNamespace, group, version, plural, name);
                    return default;
                });
        }

        /// <inheritdoc />
        public Task<IEnumerable<T>> ListCustomResources<T>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            Func<JsonElement, List<T>> converter = null,
            CancellationToken cancellationToken = default)
        {
            return this.KubeApi.CustomObjects.ListNamespacedCustomObjectAsync(
                group,
                version,
                crdNamespace,
                plural,
                cancellationToken: cancellationToken).TryMap(
                result => converter == null
                    ? ((JsonElement)result).GetProperty("items").Deserialize<List<T>>()
                    : converter((JsonElement)result), exception =>
                {
                    this.logger.LogError(exception, "Failed to read a resource list {group}/{version}/{plural}", group,
                        version, plural);
                    return Enumerable.Empty<T>();
                });
        }

        /// <inheritdoc />
        public Task<TResult> UpdateCustomResourceStatus<TResult, TStatus>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            string resourceName,
            TStatus status,
            Func<JsonElement, TResult> converter = null)
        {
            var body = new V1Patch(
                JsonSerializer.Serialize(new
                {
                    status
                }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                V1Patch.PatchType.MergePatch);

            return this.KubeApi.CustomObjects
                .PatchNamespacedCustomObjectStatusAsync(body, group, version, crdNamespace, plural, resourceName)
                .TryMap(
                    result => converter == null
                        ? ((JsonElement)result).Deserialize<TResult>()
                        : converter((JsonElement)result), exception =>
                    {
                        this.logger.LogError(exception,
                            "Failed to update status for a resource {group}/{version}/{plural}/{resourceName}", group,
                            version, plural, resourceName);
                        return default;
                    });
        }

        /// <inheritdoc />
        public Source<V1Node, NotUsed> GetNodes(int pageSize, CancellationToken cancellationToken = default)
        {
            return Source.FromTask(this.KubeApi.CoreV1.ListNodeAsync(cancellationToken: cancellationToken,
                    continueParameter: default, limit: pageSize))
                .ConcatMany(firstResult =>
                {
                    var firstBatch = firstResult.Items;
                    return Source.From(firstBatch)
                        .Concat(Source.UnfoldAsync(firstResult.Continue(), (nextBatchContinuator) =>
                        {
                            if (string.IsNullOrEmpty(nextBatchContinuator))
                            {
                                return Task.FromResult(Option<(string, IList<V1Node>)>.None);
                            }
                            else
                            {
                                return this.KubeApi.CoreV1.ListNodeAsync(cancellationToken: cancellationToken,
                                        continueParameter: nextBatchContinuator, limit: pageSize)
                                    .Map(result =>
                                        Option<(string, IList<V1Node>)>.Create((result.Continue(), result.Items)));
                            }
                        }).SelectMany(v => v));
                });
        }

        /// <inheritdoc />
        public Source<V1Job, NotUsed> GetJobs(string jobNamespace, int pageSize = 1000)
        {
            return Source.FromTask(this.KubeApi.BatchV1.ListNamespacedJobAsync(namespaceParameter: jobNamespace,
                    cancellationToken: default, continueParameter: default, limit: pageSize))
                .ConcatMany(firstResult =>
                {
                    var firstBatch = firstResult.Items;
                    return Source.From(firstBatch)
                        .Concat(Source.UnfoldAsync(firstResult.Continue(), (nextBatchContinuator) =>
                        {
                            if (string.IsNullOrEmpty(nextBatchContinuator))
                            {
                                return Task.FromResult(Option<(string, IList<V1Job>)>.None);
                            }
                            else
                            {
                                return this.KubeApi.BatchV1.ListNamespacedJobAsync(namespaceParameter: jobNamespace,
                                        cancellationToken: default, continueParameter: nextBatchContinuator,
                                        limit: pageSize)
                                    .Map(result =>
                                        Option<(string, IList<V1Job>)>.Create((result.Continue(), result.Items)));
                            }
                        }).SelectMany(v => v));
                });
        }

        /// <inheritdoc />
        public Source<(WatchEventType, V1Job), NotUsed> StreamJobEvents(string jobNamespace,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy, TimeSpan? reconnectDelay = null)
        {
            return KubernetesResourceEventSource<V1Job>.Create(
                (onEvent, onError, onClosed) => this.KubeApi
                    .BatchV1
                    .ListNamespacedJobWithHttpMessagesAsync(jobNamespace, watch: true)
                    .Watch(onEvent, onError),
                maxBufferCapacity, overflowStrategy, reconnectDelay, this.logger);
        }

        /// <inheritdoc />
        public Source<(WatchEventType, V1Pod), NotUsed> StreamPodEvents(string podNamespace,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy, TimeSpan? reconnectDelay = null)
        {
            return KubernetesResourceEventSource<V1Pod>.Create(
                (onEvent, onError, onClosed) => this.KubeApi
                    .CoreV1
                    .ListNamespacedPodWithHttpMessagesAsync(podNamespace, watch: true)
                    .Watch<V1Pod, V1PodList>(onEvent, onError),
                maxBufferCapacity, overflowStrategy, reconnectDelay, this.logger);
        }

        /// <inheritdoc />
        public Source<(WatchEventType, T), NotUsed> StreamCustomResourceEvents<T>(
            string crdNamespace,
            string group,
            string version,
            string plural,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy = OverflowStrategy.Fail,
            TimeSpan? reconnectDelay = null) where T : IKubernetesObject<V1ObjectMeta>
        {
            return KubernetesResourceEventSource<T>.Create(
                (onEvent, onError, onClosed) => this.KubeApi
                    .CustomObjects
                    .ListNamespacedCustomObjectWithHttpMessagesAsync(group, version, crdNamespace, plural, watch: true)
                    .Watch(onEvent, onError),
                maxBufferCapacity, overflowStrategy, reconnectDelay, this.logger);
        }

        /// <inheritdoc />
        public Task<V1Job> GetJob(string jobId, string jobNamespace, CancellationToken cancellationToken = default)
        {
            return this.KubeApi.BatchV1
                .ReadNamespacedJobStatusAsync(name: jobId, namespaceParameter: jobNamespace,
                    cancellationToken: cancellationToken)
                .TryMap(job => job, exception =>
                {
                    this.logger.LogWarning("Failed to find a job {jobNamespace}/{jobId}", jobNamespace, jobId);
                    return null;
                });
        }

        /// <inheritdoc />
        public Task<V1Status> DeleteJob(string jobId, string jobNamespace,
            CancellationToken cancellationToken = default,
            PropagationPolicy propagationPolicy = PropagationPolicy.Foreground)
        {
            var deleteJobDelegate = (CancellationToken ct) => this.KubeApi.BatchV1
                .DeleteNamespacedJobAsync(name: jobId, namespaceParameter: jobNamespace,
                    propagationPolicy: propagationPolicy.ToString(), cancellationToken: ct)
                .TryMap(response => response, exception =>
                {
                    this.logger.LogWarning(exception, "Failed to delete a job {jobNamespace}/{jobId}", jobNamespace,
                        jobId);
                    return default;
                });
            return deleteJobDelegate.RetryHttp429(this.logger, cancellationToken);
        }

        /// <inheritdoc />
        public Source<V1Pod, NotUsed> ListPods(string podNamespace)
        {
            return Source
                .FromTask(this.KubeApi.CoreV1.ListNamespacedPodAsync(podNamespace))
                .SelectMany(podList => podList.Items);
        }

        /// <inheritdoc />
        public Flow<IEnumerable<string>, (string, string, int), NotUsed> SendCommandToPod(string podName,
            string podNamespace)
        {
            return Flow.Create<IEnumerable<string>>()
                .SelectAsync(1,
                    commandParts => this.KubeApi.MuxedStreamNamespacedPodExecAsync(name: podName,
                        @namespace: podNamespace,
                        command: commandParts, tty: false, cancellationToken: default))
                .Select(ProcessMuxedStream);
        }

        private static V1Patch UpdateReplicasPatch(int replicaCount)
        {
            return new V1Patch(
                body: JsonSerializer.Serialize(
                    new object[]
                    {
                        new
                        {
                            op = "replace",
                            path = "/spec/replicas",
                            value = replicaCount
                        }
                    }
                ),
                type: V1Patch.PatchType.JsonPatch
            );
        }

        /// <inheritdoc />
        public Source<V1StatefulSet, NotUsed> ScaleOutStatefulSet(string statefulSet, string ns, int replicaCount,
            TimeSpan minBackOff, TimeSpan maxBackOff, double randomFactor, int maxRestarts, TimeSpan maxRetryInterval)
        {
            var restartSettings = RestartSettings.Create(minBackoff: minBackOff,
                    maxBackoff: maxBackOff,
                    randomFactor: randomFactor)
                .WithMaxRestarts(maxRestarts, maxRetryInterval);

            var patchSource = Source.FromTask(this.KubeApi
                .AppsV1
                .PatchNamespacedStatefulSetScaleAsync(UpdateReplicasPatch(replicaCount), statefulSet, ns)
                .Map(_ => Option<V1StatefulSet>.None));
            var scaleSource = Source.FromTask(this.KubeApi.AppsV1.ListNamespacedStatefulSetAsync(ns))
                .SelectMany(lst => lst.Items)
                .Where(ss => ss.Metadata.Name == statefulSet)
                .SelectAsync(1, ss => this.KubeApi.AppsV1.ReadNamespacedStatefulSetStatusAsync(ss.Metadata.Name, ns))
                .Select(ss =>
                {
                    switch (replicaCount, ss.Status.ReadyReplicas.GetValueOrDefault(0))
                    {
                        // scaling to 0 should emit success immediately.
                        case (0, _):
                            this.logger.LogInformation($"Successfully scaled {statefulSet} in {ns} to {replicaCount}");
                            return Option<V1StatefulSet>.Create(ss);
                        // scaling to non-zero, while ready replicas is zero, should cause a retry.
                        case (_, 0):
                            throw new StatefulSetNotReadyException($"Stateful set {statefulSet} is not ready yet");
                        // scaling to non-zero, while ready replicas is not zero, should emit success.
                        default:
                            this.logger.LogInformation($"Successfully scaled {statefulSet} in {ns} to {replicaCount}");
                            return Option<V1StatefulSet>.Create(ss);
                    }
                });


            var restartScaleSource =
                RestartSource.OnFailuresWithBackoff(sourceFactory: () => scaleSource, settings: restartSettings);

            return patchSource
                .Concat(restartScaleSource)
                .Collect(ss => ss.HasValue, ss => ss.Value);
        }

        /// <inheritdoc />
        public Task<V1Scale> ScaleOutStatefulSet(string statefulSet, string ns, int replicaCount)
        {
            return this.KubeApi.AppsV1.PatchNamespacedStatefulSetScaleAsync(UpdateReplicasPatch(replicaCount),
                statefulSet,
                ns).TryMap(scale => scale, err =>
            {
                this.logger.LogError(err, "Unable to scale a statefulset {ns}.{statefulSet} to {replicas}", ns,
                    statefulSet, replicaCount);
                return null;
            });
        }

        /// <inheritdoc />
        public Task<List<V1StatefulSet>> ListStatefulSets(string ns)
        {
            return this.KubeApi.AppsV1.ListNamespacedStatefulSetAsync(ns).Map(v => v.Items.ToList());
        }

        private (string, string, int) ProcessMuxedStream(IStreamDemuxer muxedStream)
        {
            using (var stdout = muxedStream.GetStream(ChannelIndex.StdOut, null))
            using (var stderr = muxedStream.GetStream(ChannelIndex.StdErr, null))
            using (var error = muxedStream.GetStream(ChannelIndex.Error, null))
            using (var errorReader = new StreamReader(error))
            using (var outReader = new StreamReader(stdout))
            using (var errReader = new StreamReader(stderr))
            {
                muxedStream.Start();
                var errors = errorReader.ReadToEnd();

                // StatusError is defined here:
                // https://github.com/kubernetes/kubernetes/blob/068e1642f63a1a8c48c16c18510e8854a4f4e7c5/staging/src/k8s.io/apimachinery/pkg/api/errors/errors.go#L37
                var returnMessage = JsonSerializer.Deserialize<V1Status>(errors);
                return (outReader.ReadToEnd(), errReader.ReadToEnd(), k8s.Kubernetes.GetExitCodeOrThrow(returnMessage));
            }
        }


        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Source<V2HorizontalPodAutoscaler, NotUsed> ListHpa(string ns)
        {
            return Source.FromTask(this.KubeApi.AutoscalingV2.ListNamespacedHorizontalPodAutoscalerAsync(ns))
                .SelectMany(hpaList => hpaList.Items);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<V2HorizontalPodAutoscaler> GetHpa(string hpaName, string ns)
        {
            return this.KubeApi.AutoscalingV2
                .ReadNamespacedHorizontalPodAutoscalerAsync(name: hpaName, namespaceParameter: ns)
                .TryMap(
                    hpa => hpa,
                    error =>
                    {
                        this.logger.LogError("Failed to locate HPA {hpa} in {namespace}", hpaName, ns);
                        return default;
                    });
        }

        private static V1Patch ObjectMetadataJsonPatch(string operation, string metadataSection,
            string metadataSectionKey,
            string metadataSectionValue = null)
        {
            object patchContent = metadataSectionValue switch
            {
                null => new
                {
                    op = operation,
                    path = $"/metadata/{metadataSection}/{metadataSectionKey.Replace("/", "~1")}"
                },
                _ => new
                {
                    op = operation,
                    path = $"/metadata/{metadataSection}/{metadataSectionKey.Replace("/", "~1")}",
                    value = metadataSectionValue
                }
            };

            return new V1Patch(
                type: V1Patch.PatchType.JsonPatch,
                body: JsonSerializer.Serialize(
                    new object[]
                    {
                        patchContent
                    })
            );
        }

        /// <inheritdoc />
        public Task<V1Pod> LabelPod(string labelKey, string labelValue, string podName, string podNamespace)
        {
            return this.KubeApi.CoreV1.PatchNamespacedPodAsync(
                ObjectMetadataJsonPatch("add", "labels", labelKey, labelValue),
                podName, podNamespace);
        }

        /// <inheritdoc />
        public Task<object> AnnotateObject(NamespacedCrd namespacedCrd, string annotationKey, string annotationValue,
            string objName, string objNamespace)
        {
            return this.KubeApi.CustomObjects.PatchNamespacedCustomObjectAsync(
                    new V1Patch(
                        body: JsonSerializer.Serialize(new
                        {
                            metadata = new V1ObjectMeta
                            {
                                Annotations = new Dictionary<string, string>
                                {
                                    { annotationKey, annotationValue }
                                }
                            }
                        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                        V1Patch.PatchType.MergePatch),
                    namespacedCrd.Group,
                    namespacedCrd.Version,
                    objNamespace,
                    namespacedCrd.Plural,
                    objName)
                .TryMap(result => result, err =>
                {
                    if (err is k8s.Autorest.HttpOperationException typedErr)
                    {
                        this.logger.LogError(err,
                            "Unable to patch a custom resource: {group}/{plural}/{version}. Request body: {body}, response: {phrase}/{response}",
                            namespacedCrd.Group, namespacedCrd.Plural, namespacedCrd.Version, typedErr.Body,
                            typedErr.Response.ReasonPhrase, typedErr.Response.Content);
                    }
                    else
                    {
                        this.logger.LogError(err, "Unable to patch a custom resource: {group}/{plural}/{version}",
                            namespacedCrd.Group, namespacedCrd.Plural, namespacedCrd.Version);
                    }

                    return null;
                });
        }

        /// <inheritdoc />
        public Task<V1Job> AnnotateJob(string jobName, string jobNamespace, string annotationKey,
            string annotationValue)
        {
            var body = new V1Patch(
                JsonSerializer.Serialize(new
                {
                    metadata = new V1ObjectMeta
                    {
                        Annotations = new Dictionary<string, string>
                        {
                            { annotationKey, annotationValue }
                        }
                    }
                }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                V1Patch.PatchType.MergePatch);
            return this.KubeApi.BatchV1.PatchNamespacedJobWithHttpMessagesAsync(body, jobName, jobNamespace)
                .TryMap(result => result.Body, err =>
                {
                    if (err is k8s.Autorest.HttpOperationException typedErr)
                    {
                        this.logger.LogError(err,
                            "Unable to patch a job: {namespace}/{name}. Request body: {body}, response: {phrase}/{response}",
                            jobNamespace,
                            jobName,
                            typedErr.Body,
                            typedErr.Response.ReasonPhrase,
                            typedErr.Response.Content);
                    }
                    else
                    {
                        this.logger.LogError(err, "Unable to patch a job: {namespace}/{name}", jobNamespace, jobName);
                    }

                    return null;
                });
        }

        /// <inheritdoc />
        public Task<object> RemoveObjectAnnotation(NamespacedCrd namespacedCrd, string annotationKey, string objName,
            string objNamespace)
        {
            return this.KubeApi.CustomObjects.PatchNamespacedCustomObjectAsync(
                    ObjectMetadataJsonPatch("remove", "annotations", annotationKey),
                    namespacedCrd.Group,
                    namespacedCrd.Version,
                    objNamespace,
                    namespacedCrd.Plural,
                    objName)
                .TryMap(result => result, err =>
                {
                    if (err is k8s.Autorest.HttpOperationException typedErr)
                    {
                        this.logger.LogError(err,
                            "Unable to patch a custom resource: {group}/{plural}/{version}. Request body: {body}, response: {phrase}/{response}",
                            namespacedCrd.Group, namespacedCrd.Plural, namespacedCrd.Version, typedErr.Body,
                            typedErr.Response.ReasonPhrase, typedErr.Response.Content);
                    }
                    else
                    {
                        this.logger.LogError(err, "Unable to patch a custom resource: {group}/{plural}/{version}",
                            namespacedCrd.Group, namespacedCrd.Plural, namespacedCrd.Version);
                    }

                    return null;
                });
        }

        /// <inheritdoc />
        public Task<V1Pod> RemoveLabelFromPod(string labelKey, string podName, string podNamespace)
        {
            return this.KubeApi.CoreV1.PatchNamespacedPodAsync(ObjectMetadataJsonPatch("remove", "labels", labelKey),
                podName,
                podNamespace);
        }

        private Task<IMetadata<V1ObjectMeta>> GetCoreV1ObjectMeta(V1Object v1Object, string objectName,
            string objectNamespace)
        {
            return v1Object switch
            {
                V1Object.POD => this.KubeApi.CoreV1.ReadNamespacedPodAsync(objectName, objectNamespace)
                    .Map(p => p as IMetadata<V1ObjectMeta>),
                V1Object.CONFIG_MAP =>
                    this.KubeApi.CoreV1.ReadNamespacedConfigMapAsync(objectName, objectNamespace)
                        .Map(p => p as IMetadata<V1ObjectMeta>),
                V1Object.SECRET => this.KubeApi.CoreV1.ReadNamespacedSecretAsync(objectName, objectNamespace)
                    .Map(p => p as IMetadata<V1ObjectMeta>),
                V1Object.STATEFUL_SET => this.KubeApi.AppsV1.ReadNamespacedStatefulSetAsync(objectName, objectNamespace)
                    .Map(p => p as IMetadata<V1ObjectMeta>),
                V1Object.DEPLOYMENT => this.KubeApi.AppsV1.ReadNamespacedDeploymentAsync(objectName, objectNamespace)
                    .Map(p => p as IMetadata<V1ObjectMeta>),
                _ => null
            };
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<string> GetV1ObjectLabelValue(string labelKey, string objectName, string objectNamespace,
            V1Object v1Object)
        {
            return GetCoreV1ObjectMeta(v1Object, objectName, objectNamespace)
                .Map(objectMeta => objectMeta?.GetLabel(labelKey));
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<string> GetV1ObjectAnnotationValue(string annotationKey, string objectName, string objectNamespace,
            V1Object v1Object)
        {
            return GetCoreV1ObjectMeta(v1Object, objectName, objectNamespace)
                .Map(objectMeta => objectMeta?.GetAnnotation(annotationKey));
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<V1StatefulSet> GetStatefulSet(string setName, string setNamespace)
        {
            return this.KubeApi.AppsV1.ReadNamespacedStatefulSetStatusAsync(setName, setNamespace);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<V1StatefulSet> GetStatefulSetInfo(string setName, string setNamespace)
        {
            return this.KubeApi.AppsV1.ReadNamespacedStatefulSetAsync(setName, setNamespace);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<V1Pod> GetPod(string podName, string podNamespace)
        {
            return this.KubeApi.CoreV1.ReadNamespacedPodAsync(podName, podNamespace);
        }

        /// <inheritdoc />
        public Task<List<V1Pod>> GetPods(Dictionary<string, string> podLabels, string podNamespace,
            CancellationToken cancellationToken = default)
        {
            var labelStr = string.Join(",", podLabels.Select(kv => $"{kv.Key}={kv.Value}"));
            var podListFactory = (CancellationToken ct) => this.KubeApi.CoreV1.ListNamespacedPodAsync(
                namespaceParameter: podNamespace,
                labelSelector: labelStr, cancellationToken: ct);
            return podListFactory.RetryHttp429(this.logger, cancellationToken)
                .TryMap(podList => podList.Items.ToList(), err =>
                {
                    this.logger.LogError(err, "Unable to find pods with matching labels {label}", labelStr);
                    return new List<V1Pod>();
                });
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<IDictionary<string, string>> GetConfigMapData(string configMapName, string configMapNamespace)
        {
            return this.KubeApi
                .CoreV1
                .ReadNamespacedConfigMapAsync(name: configMapName, namespaceParameter: configMapNamespace)
                .TryMap(cmap => cmap.Data, err =>
                {
                    this.logger.LogError(err, "Unable to find config map {configMap} in {configMapNamespace}",
                        configMapName, configMapNamespace);
                    return new Dictionary<string, string>();
                });
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Source<ByteString, NotUsed> StreamPodLog(string podName, string podNamespace,
            CancellationToken cancellationToken)
        {
            return Source
                .FromTask(
                    this.KubeApi.CoreV1.ReadNamespacedPodLogAsync(
                        name: podName,
                        namespaceParameter: podNamespace,
                        cancellationToken: cancellationToken).TryMap(
                        stream =>
                        {
                            var buffer = new byte[4096];
                            using var ms = new MemoryStream();
                            while (stream.Read(buffer, 0, buffer.Length) > 0)
                            {
                                ms.Write(buffer);
                            }

                            return ByteString.FromBytes(ms.ToArray());
                        },
                        exception =>
                        {
                            this.logger.LogWarning(exception, "Failed to read log stream from {pod}", podName);
                            return ByteString.Empty;
                        }));
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<bool> DeletePod(string podName, string podNamespace)
        {
            return this.KubeApi.CoreV1.DeleteNamespacedPodAsync(name: podName, namespaceParameter: podNamespace)
                .TryMap(deletedPod => true, err =>
                {
                    this.logger.LogError(err, "Failed to delete pod {podName} in {podNamespace}", podName,
                        podNamespace);
                    return false;
                });
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public Task<string> ReadSecretValue(string secretName, string secretNamespace, string secretKey)
        {
            return this.KubeApi.CoreV1.ReadNamespacedSecretAsync(secretName, secretNamespace)
                .TryMap(secret =>
                {
                    if (secret.StringData.TryGetValue(secretKey, out var secretValue))
                    {
                        return secretValue;
                    }

                    return null;
                }, err =>
                {
                    this.logger.LogError(err,
                        "Failed to read a secret value from a secret {secretName}/{secretKey} in {secretNamespace}",
                        secretName,
                        secretNamespace, secretKey);
                    return null;
                });
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public string GetCurrentNamespace()
        {
            return File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/namespace");
        }

        /// <inheritdoc />
        public string ClusterName { get; }

        /// <inheritdoc />
        public string AccessToken { get; }

        /// <inheritdoc />
        public X509Certificate2Collection SslCaCerts { get; }
    }
}
