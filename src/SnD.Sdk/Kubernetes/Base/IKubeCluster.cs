using Akka;
using Akka.Streams.Dsl;
using k8s;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.IO;
using Snd.Sdk.Storage.Base;

namespace Snd.Sdk.Kubernetes.Base
{
    /// <summary>
    /// CoreV1 API objects.
    /// </summary>
    public enum V1Object
    {
        /// <summary>
        /// V1Pod.
        /// </summary>
        POD,

        /// <summary>
        /// V1ConfigMap.
        /// </summary>
        CONFIG_MAP,

        /// <summary>
        /// V1Secret.
        /// </summary>
        SECRET,

        /// <summary>
        /// V1StatefulSet.
        /// </summary>
        STATEFUL_SET,

        /// <summary>
        /// V1Deployment.
        /// </summary>
        DEPLOYMENT
    }

    /// <summary>
    /// Propagation policy types.
    /// </summary>
    public enum PropagationPolicy
    {
        /// <summary>
        /// The child resources of the deleted resource will be orphaned.
        /// </summary>
        Orphan,
        /// <summary>
        /// The deletion of the resource will be processed in the background.
        /// </summary>
        Background,
        /// <summary>
        /// The deletion of the resource will be processed synchronously in the foreground.
        /// </summary>
        Foreground
    }

    /// <summary>
    /// Kubernetes cluster API wrapper.
    /// </summary>
    public interface IKubeCluster
    {
        /// <summary>
        /// Sends a job to the specified namespace and returns the status of the job.
        /// </summary>
        /// <param name="job">The job object to send.</param>
        /// <param name="jobNamespace">The namespace in which to send the job.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task object representing the asynchronous operation that returns the status of the job as a V1JobStatus object.</returns>
        Task<V1JobStatus> SendJob(V1Job job, string jobNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a Kubernetes job to the specified namespace and maps the result to a custom type.
        /// </summary>
        /// <param name="job">The Kubernetes job object to be sent.</param>
        /// <param name="jobNamespace">The namespace in which to send the job.</param>
        /// <param name="resultMapper">A function that maps the job result to a custom type.</param>
        /// <param name="cancellationToken">An optional CancellationToken to interrupt task execution.</param>
        /// <typeparam name="T">The type to which the job result will be mapped.</typeparam>
        /// <returns>A Task object representing the asynchronous operation, which when completed will return the job result mapped to the custom type.</returns>
        Task<T> SendJob<T>(V1Job job, string jobNamespace, Func<V1Job, T> resultMapper,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Reads a job list for this fleet member.
        /// </summary>
        /// <returns></returns>
        Source<V1Job, NotUsed> GetJobs(string jobNamespace, int pageSize = 1000);

        /// <summary>
        /// Akka.NET Source streaming Kubernetes Events for all Jobs in the cluster.
        /// </summary>
        /// <param name="jobNamespace">Namespace to watch for</param>
        /// <param name="maxBufferCapacity">Maximum size of buffer in source</param>
        /// <param name="overflowStrategy"><see cref="OverflowStrategy"/>. Backpressure overflow strategy
        /// is not supported by the source</param>
        /// <param name="reconnectDelay">Interval for check if watcher is alive and recreate
        /// if watcher stopped watching for events. Defaults to 1 minute.</param>
        /// <returns></returns>
        public Source<(WatchEventType, V1Job), NotUsed> StreamJobEvents(string jobNamespace,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy,
            TimeSpan? reconnectDelay = null);

        /// <summary>
        /// Akka.NET Source streaming Kubernetes Events for all Pods in the cluster.
        /// </summary>
        /// <param name="podNamespace">Namespace to watch for</param>
        /// <param name="maxBufferCapacity">Maximum size of buffer in source</param>
        /// <param name="overflowStrategy"><see cref="OverflowStrategy"/>. Backpressure overflow strategy
        /// is not supported by the source</param>
        /// <param name="reconnectDelay">Interval for check if watcher is alive and recreate
        /// if watcher stopped watching for events. Defaults to 1 minute.</param>
        /// <returns></returns>
        public Source<(WatchEventType, V1Pod), NotUsed> StreamPodEvents(string podNamespace,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy,
            TimeSpan? reconnectDelay = null);

        /// <summary>
        /// Akka.NET Source streaming Kubernetes Events for all CRDs under group/version/plural API in the cluster.
        /// </summary>
        /// <typeparam name="T">The type of the custom resource.</typeparam>
        /// <param name="crdNamespace">Namespace to watch for.</param>
        /// <param name="group">The group name of the Kubernetes CRD.</param>
        /// <param name="version">The version of the Kubernetes CRD.</param>
        /// <param name="plural">The plural name of the Kubernetes CRD.</param>
        /// <param name="maxBufferCapacity">Maximum size of buffer in source</param>
        /// <param name="overflowStrategy"><see cref="OverflowStrategy"/>. Backpressure overflow strategy
        /// is not supported by the source</param>
        /// <param name="reconnectDelay">Interval for check if watcher is alive and recreate
        /// if watcher stopped watching for events. Defaults to 1 minute.</param>
        /// <returns></returns>
        public Source<(WatchEventType, T), NotUsed> StreamCustomResourceEvents<T>(
            string crdNamespace,
            string group,
            string version,
            string plural,
            int maxBufferCapacity,
            OverflowStrategy overflowStrategy,
            TimeSpan? reconnectDelay = null) where T : IKubernetesObject<V1ObjectMeta>;

        /// <summary>
        /// Reads a specific job from this cluster.
        /// </summary>
        /// <param name="jobId">Name of the job.</param>
        /// <param name="jobNamespace">Namespace of the job.</param>
        /// <param name="cancellationToken">An optional CancellationToken to interrupt task execution.</param>
        /// <returns></returns>
        Task<V1Job> GetJob(string jobId, string jobNamespace, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a specific job from this cluster and ALL associated resources (spawned pods) before exiting.
        /// </summary>
        /// <param name="jobId">Name of the job.</param>
        /// <param name="jobNamespace">Namespace of the job.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// /// <param name="propagationPolicy">Type of propagation policy <see cref="PropagationPolicy"/>></param>
        /// <returns></returns>
        Task<V1Status> DeleteJob(string jobId, string jobNamespace, CancellationToken cancellationToken = default, PropagationPolicy propagationPolicy = PropagationPolicy.Foreground);

        /// <summary>
        /// Lists pods using Akka stream.
        /// </summary>
        /// <param name="podNamespace"></param>
        /// <returns></returns>
        Source<V1Pod, NotUsed> ListPods(string podNamespace);

        /// <summary>
        /// Sends a command to pod using pod exec.
        /// </summary>
        /// <param name="podName">Name of a pod.</param>
        /// <param name="podNamespace">Namespace of a pod.</param>
        /// <returns></returns>
        Flow<IEnumerable<string>, (string, string, int), NotUsed> SendCommandToPod(string podName, string podNamespace);

        /// <summary>
        /// Scales out a stateful set using Akka restart functionality.
        /// </summary>
        /// <param name="statefulSet"></param>
        /// <param name="ns"></param>
        /// <param name="replicaCount"></param>
        /// <param name="minBackOff"></param>
        /// <param name="maxBackOff"></param>
        /// <param name="randomFactor"></param>
        /// <param name="maxRestarts"></param>
        /// <param name="maxRetryInterval"></param>
        /// <returns></returns>
        Source<V1StatefulSet, NotUsed> ScaleOutStatefulSet(string statefulSet, string ns, int replicaCount, TimeSpan minBackOff, TimeSpan maxBackOff, double randomFactor, int maxRestarts, TimeSpan maxRetryInterval);

        /// <summary>
        /// Scales out a statefulset without waiting for the operation to complete.
        /// </summary>
        /// <param name="statefulSet">Name of a statefulset.</param>
        /// <param name="ns">Namespace.</param>
        /// <param name="replicaCount">Number of replicas to scale to.</param>
        /// <returns></returns>
        Task<V1Scale> ScaleOutStatefulSet(string statefulSet, string ns, int replicaCount);

        /// <summary>
        /// Returns a list of stateful sets present in a namespace, using optional cache.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        Task<List<V1StatefulSet>> ListStatefulSets(string ns);

        /// <summary>
        /// Reads a stateful set status.
        /// </summary>
        /// <param name="setName"></param>
        /// <param name="setNamespace"></param>
        /// <returns></returns>
        Task<V1StatefulSet> GetStatefulSet(string setName, string setNamespace);

        /// <summary>
        /// Reads a stateful set configuration.
        /// </summary>
        /// <param name="setName"></param>
        /// <param name="setNamespace"></param>
        /// <returns></returns>
        Task<V1StatefulSet> GetStatefulSetInfo(string setName, string setNamespace);

        /// <summary>
        /// Lists horizontal pod autoscalers using Akka stream.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        Source<V2HorizontalPodAutoscaler, NotUsed> ListHpa(string ns);

        /// <summary>
        /// Reads a single horizontal pod autoscaler. Returns null on exception.
        /// </summary>
        /// <param name="hpaName">Name of the HPA to read.</param>
        /// <param name="ns">Namespace where HPA is deployed.</param>
        /// <returns></returns>
        Task<V2HorizontalPodAutoscaler> GetHpa(string hpaName, string ns);

        /// <summary>
        /// Patches a pod to have a certain label.
        /// </summary>
        /// <param name="labelKey">Key of a label to add.</param>
        /// <param name="labelValue">Value of a label to add.</param>
        /// <param name="podName">Name of a pod.</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<V1Pod> LabelPod(string labelKey, string labelValue, string podName, string podNamespace);

        /// <summary>
        /// Patches a custom object to have a certain annotation.
        /// </summary>
        /// <param name="annotationKey">Key of an annotation to add.</param>
        /// <param name="annotationValue">Value of an annotation to add.</param>
        /// <param name="namespacedCrd">Resource definition search parameters.</param>
        /// <param name="objName">Name of an object to annotate.</param>
        /// <param name="objNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<object> AnnotateObject(NamespacedCrd namespacedCrd, string annotationKey, string annotationValue,
            string objName, string objNamespace);

        /// <summary>
        /// Patches a job to have a certain annotation.
        /// <param name="name">Job name</param>
        /// <param name="nameSpace">Job namespace</param>
        /// <param name="annotationKey">Key of an annotation to add.</param>
        /// <param name="annotationValue">Value of an annotation to add.</param>
        /// </summary>
        /// <returns></returns>
        public Task<V1Job> AnnotateJob(string name, string nameSpace, string annotationKey, string annotationValue);


        /// <summary>
        /// Remove an annotation from a custom object.
        /// </summary>
        /// <param name="annotationKey">Key of an annotation to add.</param>
        /// <param name="namespacedCrd">Resource definition search parameters.</param>
        /// <param name="objName">Name of an object to annotate.</param>
        /// <param name="objNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<object> RemoveObjectAnnotation(NamespacedCrd namespacedCrd, string annotationKey, string objName, string objNamespace);

        /// <summary>
        /// Patches a pod, removing a certain label.
        /// </summary>
        /// <param name="labelKey">Key of a label to remove.</param>
        /// <param name="podName">Name of a pod.</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<V1Pod> RemoveLabelFromPod(string labelKey, string podName, string podNamespace);

        /// <summary>
        /// Reads a label value from a CoreV1 API object.
        /// </summary>
        /// <param name="labelKey">Key of a label to read value for.</param>
        /// <param name="objectName">Name of an object.</param>
        /// <param name="objectNamespace">Namespace of an object this member operates in.</param>
        /// <param name="v1Object">Type of CoreV1 object to read label from.</param>
        /// <returns></returns>
        Task<string> GetV1ObjectLabelValue(string labelKey, string objectName, string objectNamespace, V1Object v1Object);

        /// <summary>
        /// Reads an annotation value from a CoreV1 API object.
        /// </summary>
        /// <param name="annotationKey">Key of a label to read value for.</param>
        /// <param name="objectName">Name of an object.</param>
        /// <param name="objectNamespace">Namespace of an object this member operates in.</param>
        /// <param name="v1Object">Type of CoreV1 object to read label from.</param>
        /// <returns></returns>
        Task<string> GetV1ObjectAnnotationValue(string annotationKey, string objectName, string objectNamespace, V1Object v1Object);

        /// <summary>
        /// Reads a pod.
        /// </summary>
        /// <param name="podName">Name of a pod.</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<V1Pod> GetPod(string podName, string podNamespace);

        /// <summary>
        /// Retrieves all pods in a namespace with a matching label set. All provided labels must match.
        /// </summary>
        /// <param name="podLabels">A map of labelKey: labelValue pairs</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <param name="cancellationToken">An optional CancellationToken to interrupt task execution.</param>
        /// <returns></returns>
        Task<List<V1Pod>> GetPods(Dictionary<string, string> podLabels, string podNamespace, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the `data` segment of a config map.
        /// </summary>
        /// <param name="configMapName">Name of a config map to read from.</param>
        /// <param name="configMapNamespace">Namespace where this member operates.</param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetConfigMapData(string configMapName, string configMapNamespace);

        /// <summary>
        /// Kubernetes API client for this member.
        /// </summary>
        IKubernetes KubeApi { get; }

        /// <summary>
        /// Optional external interface to a shared filesystem (ReadWriteMany mount) used by this fleet member.
        /// </summary>
        ISharedFileSystemService SharedFileSystem();

        /// <summary>
        /// Streams a log from a pod that belongs to this fleet member.
        /// </summary>
        /// <param name="podName">Name of a pod this member has access to.</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <param name="cancellationToken">Cancellation token for the pod stream operation timeout.</param>
        /// <returns></returns>
        Source<ByteString, NotUsed> StreamPodLog(string podName, string podNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a pod that belongs to this fleet member.
        /// </summary>
        /// <param name="podName">Name of a pod this member has access to.</param>
        /// <param name="podNamespace">Namespace of a pod this member operates in.</param>
        /// <returns></returns>
        Task<bool> DeletePod(string podName, string podNamespace);

        /// <summary>
        ///  Reads an individual V1Secret value using the provided key.
        /// </summary>
        /// <param name="secretName">Name of a secret object.</param>
        /// <param name="secretNamespace">Namespace of a secret object.</param>
        /// <param name="secretKey">Key inside the secret to retrieve a value from.</param>
        /// <returns></returns>
        Task<string> ReadSecretValue(string secretName, string secretNamespace, string secretKey);

        /// <summary>
        /// Kubernetes context name associated with this cluster.
        /// </summary>
        string ClusterName { get; }

        /// <summary>
        /// Access token for accessing Kubernetes API from outside the cluster
        /// </summary>
        string AccessToken { get; }


        /// <summary>
        /// Trusted certificate authorities for cluster API endpoint
        /// </summary>
        X509Certificate2Collection SslCaCerts { get; }

        /// <summary>
        /// Retrieves a list of nodes available in the cluster.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns>An iterable of V1Node.</returns>
        IEnumerable<V1Node> GetNodes(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a node metrics info for each node from metrics server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns></returns>
        IEnumerable<NodeMetrics> GetNodeMetrics(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a node metrics info for each node from metrics server (async).
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns></returns>
        Task<IEnumerable<NodeMetrics>> GetNodeMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a pod metrics info for each pod from metrics server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns></returns>
        IEnumerable<PodMetrics> GetPodMetrics(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a pod metrics info for each pod from metrics server (async).
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns></returns>
        Task<IEnumerable<PodMetrics>> GetPodMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a custom resource of type T from a Kubernetes Custom Resource Definition (CRD) with the specified group, version, and plural name.
        /// </summary>
        /// <typeparam name="T">The type of the custom resource to retrieve.</typeparam>
        /// <param name="group">The group name of the Kubernetes CRD.</param>
        /// <param name="version">The version of the Kubernetes CRD.</param>
        /// <param name="plural">The plural name of the Kubernetes CRD.</param>
        /// <param name="crdNamespace">Namespace where CRD is located.</param>
        /// <param name="name">The name of the custom resource to retrieve.</param>
        /// <param name="converter">Optional converter from JsonElement for this CRD.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, which when completed will return an instance of type T representing the retrieved custom resource.</returns>
        Task<T> GetCustomResource<T>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            string name,
            Func<JsonElement, T> converter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a list of custom resources of type T from a Kubernetes Custom Resource Definition (CRD) with the specified group, version, and plural name.
        /// </summary>
        /// <typeparam name="T">The type of the custom resource to retrieve.</typeparam>
        /// <param name="group">The group name of the Kubernetes CRD.</param>
        /// <param name="version">The version of the Kubernetes CRD.</param>
        /// <param name="plural">The plural name of the Kubernetes CRD.</param>
        /// <param name="crdNamespace">Namespace where CRD is located.</param>
        /// <param name="converter">Optional converter from JsonElement for this CRD.</param> 
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, which when completed will return an instance of type T representing the retrieved custom resource.</returns>
        Task<IEnumerable<T>> ListCustomResources<T>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            Func<JsonElement, List<T>> converter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of a custom resource in Kubernetes by sending a patch request.
        /// </summary>
        /// <param name="group">The API group of the custom resource.</param>
        /// <param name="version">The API version of the custom resource.</param>
        /// <param name="plural">The plural name of the custom resource.</param>
        /// <param name="crdNamespace">The namespace of the custom resource.</param>
        /// <param name="resourceName">The name of the custom resource.</param>
        /// <param name="status">The new status of the custom resource.</param>
        /// <param name="converter">Optional function to convert the response payload to a specified result type.</param>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <typeparam name="TStatus">Status object type</typeparam>
        /// <returns></returns>
        public Task<TResult> UpdateCustomResourceStatus<TResult, TStatus>(
            string group,
            string version,
            string plural,
            string crdNamespace,
            string resourceName,
            TStatus status,
            Func<JsonElement, TResult> converter = null);

        /// <summary>
        /// Retrieves cluster node as a stream. Use this method for large clusters.
        /// </summary>
        /// <param name="pageSize">Number of nodes to retrieves in a single API call.</param>
        /// <param name="cancellationToken">Optional cancellation token to support task cancel logic.</param>
        /// <returns>Akka.Streams Source of V1Node.</returns>
        Source<V1Node, NotUsed> GetNodes(int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current namespace the member is running in.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentNamespace();
    }
}
