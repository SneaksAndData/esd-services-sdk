using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Akka.Util.Internal;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Snd.Sdk.Helpers;
using Snd.Sdk.Tasks;
using Policy = Polly.Policy;

namespace Snd.Sdk.Kubernetes
{
    /// <summary>
    /// Extension method for modifying k8s object models or method behaviours.
    /// </summary>
    /// 
    public static class KubernetesApiExtensions
    {
        private const string BILLING_ID_ANNOTATION_NAME = "wagyu/billing-id";

        /// <summary>
        /// Creates a simple kubernetes job with a single container pod ready to be fired.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="jobName"></param>
        /// <param name="jobResources"></param>
        /// <returns></returns>
        public static V1Job CreateJob(string imageName, string jobName, Dictionary<string, string> jobResources)
        {
            var jobMeta = new V1ObjectMeta(name: jobName, labels: new Dictionary<string, string>
            {
                {
                    "api.sneaksanddata.io/app-version",
                    Environment.GetEnvironmentVariable("APPLICATION_VERSION") ?? "0.0.0"
                }
            });

            var resourceQuantities = jobResources.ToDictionary(jr => jr.Key, jr => new ResourceQuantity(jr.Value));

            var podTemplate = new V1PodTemplateSpec(
                metadata: new V1ObjectMeta(name: jobName),
                spec: new V1PodSpec(
                    restartPolicy: "Never",
                    serviceAccountName: "default",
                    containers: new List<V1Container>
                    {
                        new(
                            name: jobName,
                            image: imageName,
                            resources: new V1ResourceRequirements(limits: resourceQuantities,
                                requests: resourceQuantities),
                            imagePullPolicy: "IfNotPresent"
                        )
                    }
                )
            );


            var spec = new V1JobSpec(
                template: podTemplate
            );

            return new V1Job(
                apiVersion: "batch/v1",
                kind: "Job",
                metadata: jobMeta,
                spec: spec
            );
        }

        /// <summary>
        /// Attach additional labels to both V1Job and its pod.
        /// </summary>
        /// <param name="labels">Additional labels to assign.</param>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="replace">Whether to replace existing labels or merge new ones in.</param>
        /// <returns></returns>
        public static V1Job WithLabels(this V1Job job, Dictionary<string, string> labels, bool replace = false)
        {
            var newLabels = replace switch
            {
                true => labels,
                false => (job.Metadata.Labels ?? new Dictionary<string, string>()).Concat(labels)
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
            };

            job.Metadata.Labels = newLabels;
            job.Spec.Template.Metadata.Labels = newLabels;

            return job;
        }

        /// <summary>
        /// Assigns a service account name to be used for this V1Job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="serviceAccountName">A service account name.</param>
        /// <returns></returns>
        public static V1Job WithServiceAccount(this V1Job job, string serviceAccountName)
        {
            job.Spec.Template.Spec.ServiceAccountName = serviceAccountName;
            return job;
        }

        /// <summary>
        /// Assigns a custom entrypoint by overriding commands and arguments.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="commands">Command(s) to execute.</param>
        /// <param name="args">Command arguments.</param>
        /// <returns></returns>
        public static V1Job WithCustomEntrypoint(this V1Job job, List<string> commands, List<string> args)
        {
            job.Spec.Template.Spec.Containers[0].Command = commands;
            job.Spec.Template.Spec.Containers[0].Args = args;
            return job;
        }

        /// <summary>
        /// Assigns custom environment variables for this job from a static value map.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="envVars">Dictionary of environment variables and their corresponding values to assign. If a value has format fieldRef:123, then a fieldRef.fieldPath env is created.</param>
        /// <returns></returns>
        public static V1Job WithCustomEnvironment(this V1Job job, Dictionary<string, string> envVars)
        {
            var additionalVars = envVars.Select(kv =>
            {
                return kv.Value switch
                {
                    var s when s.StartsWith("fieldRef:") => new V1EnvVar(name: kv.Key,
                        valueFrom: new V1EnvVarSource(
                            fieldRef: new V1ObjectFieldSelector(
                                fieldPath: kv.Value.Replace("fieldRef:", string.Empty)))),
                    _ => new V1EnvVar(name: kv.Key, value: kv.Value)
                };
            });

            job.Spec.Template.Spec.Containers[0].Env =
                (job.Spec.Template.Spec.Containers[0].Env ?? new List<V1EnvVar>())
                .Concat(additionalVars).ToList();

            return job;
        }

        /// <summary>
        /// Assigns custom environment variables for this job using mapped secrets.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="secretEnvVarFrom">Secrets to map on job containers.</param>
        /// <returns></returns>
        public static V1Job WithCustomEnvironment(this V1Job job, IEnumerable<string> secretEnvVarFrom)
        {
            job.Spec.Template.Spec.Containers[0].EnvFrom =
                (job.Spec.Template.Spec.Containers[0].EnvFrom ?? new List<V1EnvFromSource>())
                .Concat(secretEnvVarFrom.Select(v => new V1EnvFromSource(secretRef: new V1SecretEnvSource(name: v)))
                    .ToList()).ToList();

            return job;
        }

        /// <summary>
        /// Assigns custom environment variables for this job using secrets or config maps.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="envVarFrom">Secrets or config map objects to map on job containers.</param>
        /// <returns></returns>
        public static V1Job WithCustomEnvironment(this V1Job job, IEnumerable<V1EnvFromSource> envVarFrom)
        {
            job.Spec.Template.Spec.Containers[0].EnvFrom =
                (job.Spec.Template.Spec.Containers[0].EnvFrom ?? new List<V1EnvFromSource>())
                .Concat(envVarFrom).ToList();

            return job;
        }

        /// <summary>
        /// Assigns custom environment variables for this job using mapped env vars.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="envVars">V1EnvVar collection to map on job containers.</param>
        /// <returns></returns>
        public static V1Job WithCustomEnvironment(this V1Job job, IEnumerable<V1EnvVar> envVars)
        {
            job.Spec.Template.Spec.Containers[0].Env =
                (job.Spec.Template.Spec.Containers[0].Env ?? new List<V1EnvVar>())
                .Concat(envVars).ToList();

            return job;
        }

        /// <summary>
        /// Assigns this job to a specified node group.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="nodeClass">Node affinity/taint key, for example "kubernetes.mycloud.io"</param>
        /// <param name="nodeClassValue">Node affinity/taint value, for example "my-gpu-node"</param>
        /// <returns></returns>
        public static V1Job WithSpecificNodes(this V1Job job, string nodeClass, string nodeClassValue)
        {
            job.Spec.Template.Spec.Affinity = new V1Affinity(nodeAffinity: new V1NodeAffinity(
                requiredDuringSchedulingIgnoredDuringExecution: new V1NodeSelector(
                    nodeSelectorTerms: new List<V1NodeSelectorTerm>
                    {
                        new V1NodeSelectorTerm(
                            new List<V1NodeSelectorRequirement>
                            {
                                new V1NodeSelectorRequirement(
                                    key: nodeClass,
                                    operatorProperty: "In",
                                    values: new List<string>
                                    {
                                        nodeClassValue
                                    })
                            })
                    })));

            job.Spec.Template.Spec.Tolerations = new List<V1Toleration>
            {
                new V1Toleration(key: nodeClass, operatorProperty: "Equal", value: nodeClassValue,
                    effect: "NoSchedule")
            };

            return job;
        }

        /// <summary>
        /// Assigns the active deadline to this job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="deadline">Active deadline in seconds.</param>
        /// <returns></returns>
        public static V1Job WithDeadline(this V1Job job, int deadline)
        {
            job.Spec.ActiveDeadlineSeconds = deadline;

            return job;
        }

        /// <summary>
        /// Assigns a maximum number of retries to this job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="retries">Maximum number of retries.</param>
        /// <returns></returns>
        public static V1Job WithRetries(this V1Job job, int retries)
        {
            job.Spec.BackoffLimit = retries;

            return job;
        }

        /// <summary>
        /// Assigns a time-to-live for this job, in seconds, after it has been finished.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="ttlSecondsAfterFinished">Maximum number of retries.</param>
        /// <returns></returns>
        public static V1Job WithTtlAfterFinished(this V1Job job, int ttlSecondsAfterFinished)
        {
            job.Spec.TtlSecondsAfterFinished = ttlSecondsAfterFinished;

            return job;
        }

        /// <summary>
        /// Assigns supplied annotations for a job and dependent object templates.
        /// NB. This method doesn't rule-check value. Make sure you follow https://kubernetes.io/docs/concepts/overview/working-with-objects/annotations/#syntax-and-character-set
        /// when creating annotations.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="annotations">Annotations to assign to this job.</param>
        /// <param name="replace">Whether to replace existing annotations or merge new ones in.</param>
        /// <returns></returns>
        public static V1Job WithAnnotations(this V1Job job, Dictionary<string, string> annotations,
            bool replace = false)
        {
            if (!annotations.Any())
            {
                return job;
            }

            var newAnnotations = replace switch
            {
                true => annotations,
                false => (job.Metadata.Annotations ?? new Dictionary<string, string>()).DeepClone()
                    .MergeDifference(annotations)
            };

            job.Metadata.Annotations = newAnnotations;
            job.Spec.Template.Metadata.Annotations = newAnnotations;

            return job;
        }

        /// <summary>
        /// Overrides resources and limits on the first container in a job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="resources">New resource map to assign for this job.</param>
        /// <returns></returns>
        public static V1Job WithCustomResources(this V1Job job, Dictionary<string, string> resources)
        {
            if (!resources.Any())
            {
                return job;
            }

            var resourceMap = resources
                .Select(r => new KeyValuePair<string, ResourceQuantity>(r.Key, new ResourceQuantity(r.Value)))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var newResources = new V1ResourceRequirements(limits: resourceMap, requests: resourceMap);

            job.Spec.Template.Spec.Containers[0].Resources = newResources;

            return job;
        }

        /// <summary>
        /// Updates name of a first container in this job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="containerName">A new name to assign.</param>
        /// <returns></returns>
        public static V1Job WithContainerName(this V1Job job, string containerName)
        {
            job.Spec.Template.Spec.Containers[0].Name = containerName;

            return job;
        }

        /// <summary>
        /// Assigns a new name for this job.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="jobName">A new name to assign.</param>
        /// <returns></returns>
        public static V1Job WithName(this V1Job job, string jobName)
        {
            job.Metadata.Name = jobName;

            return job;
        }

        /// <summary>
        /// Adds specified tolerations to the existing list of pod tolerations. Creates an new list of no tolerations are present yet.
        /// </summary>
        /// <param name="job">V1Job to modify.</param>
        /// <param name="tolerations">A list of k8s tolerations to use when scheduling a pod for this job.</param>
        /// <returns></returns>
        public static V1Job WithTolerations(
            this V1Job job,
            IEnumerable<(string tolerationKey, string tolerationOperator, string tolerationEffect, string
                tolerationValue)> tolerations)
        {
            var newTolerations = tolerations.Select(tl => new V1Toleration(
                key: tl.tolerationKey,
                effect: tl.tolerationEffect,
                operatorProperty: tl.tolerationOperator,
                value: tl.tolerationValue)).ToList();

            job.Spec.Template.Spec.Tolerations = (job.Spec.Template.Spec.Tolerations ?? new List<V1Toleration>())
                .Concat(newTolerations).ToList();

            return job;
        }

        /// <summary>
        /// Adds or updates volumes and volume mounts to the V1Job object's template specification that target hostPath paths.
        /// </summary>
        /// <param name="job">The V1Job object to modify.</param>
        /// <param name="volumeMap">A dictionary of volume name to hostPath path (ends with /).</param>
        /// <returns>The modified V1Job object.</returns>
        public static V1Job WithHostPathVolumes(this V1Job job, Dictionary<string, string> volumeMap)
        {
            job.Spec.Template.Spec.Volumes ??= new List<V1Volume>();
            job.Spec.Template.Spec.Containers[0].VolumeMounts ??= new List<V1VolumeMount>();

            foreach (var (volumeName, hostPath) in volumeMap)
            {
                var existingVolume = job.Spec.Template.Spec.Volumes.FirstOrDefault(vol => vol.Name == volumeName);
                if (existingVolume != null)
                {
                    job.Spec.Template.Spec.Volumes[job.Spec.Template.Spec.Volumes.IndexOf(existingVolume)].HostPath =
                        new V1HostPathVolumeSource
                        {
                            Path = hostPath
                        };
                }
                else
                {
                    job.Spec.Template.Spec.Volumes.Add(new V1Volume
                    {
                        Name = volumeName,
                        HostPath = new V1HostPathVolumeSource { Path = hostPath }
                    });
                }

                var existingMount = job.Spec.Template.Spec.Containers[0].VolumeMounts
                    .FirstOrDefault(vol => vol.Name == volumeName);

                if (existingMount != null)
                {
                    job.Spec.Template.Spec.Containers[0]
                        .VolumeMounts[job.Spec.Template.Spec.Containers[0].VolumeMounts.IndexOf(existingMount)]
                        .MountPath = hostPath.TrimEnd('/');
                }
                else
                {
                    job.Spec.Template.Spec.Containers[0].VolumeMounts.Add(new V1VolumeMount(
                        mountPath: hostPath.TrimEnd('/'),
                        name: volumeName
                    ));
                }
            }

            return job;
        }

        /// <summary>
        /// Adds or updates ConfigMap volumes in the V1Job object's template specification based on the given volume map.
        /// </summary>
        /// <param name="job">The V1Job object to modify.</param>
        /// <param name="volumeMap">A dictionary of volume name to ConfigMap name mappings to apply.</param>
        /// <returns>The modified V1Job object.</returns>
        public static V1Job WithConfigMapVolumes(this V1Job job, Dictionary<string, string> volumeMap)
        {
            job.Spec.Template.Spec.Volumes ??= new List<V1Volume>();

            foreach (var (volumeName, volumeConfigMapName) in volumeMap)
            {
                var existingVolume = job.Spec.Template.Spec.Volumes.FirstOrDefault(vol => vol.Name == volumeName);
                if (existingVolume != null)
                {
                    job.Spec.Template.Spec.Volumes[job.Spec.Template.Spec.Volumes.IndexOf(existingVolume)].ConfigMap =
                        new V1ConfigMapVolumeSource
                        {
                            Name = volumeConfigMapName
                        };
                }
                else
                {
                    job.Spec.Template.Spec.Volumes.Add(new V1Volume
                    {
                        Name = volumeConfigMapName
                    });
                }
            }

            return job;
        }

        /// <summary>
        /// Adds an owner reference to the job.
        /// </summary>
        /// <param name="job">The Kubernetes Job object to add the owner reference to.</param>
        /// <param name="apiVersion">The API version of the owner object.</param>
        /// <param name="kind">The kind of the owner object.</param>
        /// <param name="metadata">The metadata of the owner object.</param>
        /// <returns>The Kubernetes Job object with the added owner reference.</returns>
        private static V1Job WithOwnerReference(this V1Job job, string apiVersion, string kind, V1ObjectMeta metadata)
        {
            job.Metadata.OwnerReferences ??= new List<V1OwnerReference>();
            job.Metadata.OwnerReferences.Add(new V1OwnerReference(apiVersion, kind, metadata.Name,
                metadata.Uid));
            return job;
        }


        /// <summary>
        /// Adds an Job owner object reference to a Job.
        /// </summary>
        /// <param name="job">The Kubernetes Job object to add the owner reference to.</param>
        /// <param name="metadata">The metadata of the Job owner object.</param>
        /// <returns>The Kubernetes Job object with the added owner reference.</returns>
        public static V1Job WithJobOwnerReference(this V1Job job, V1ObjectMeta metadata)
        {
            return WithOwnerReference(job, "batch/v1", "Job", metadata);
        }


        /// <summary>
        /// Adds the billing id annotation to the job.
        /// </summary>
        /// <param name="job">The job object to modify</param>
        /// <param name="billingId">Billing Id value</param>
        /// <returns></returns>
        public static V1Job WithBillingId(this V1Job job, string billingId)
        {
            return job.WithAnnotations(new Dictionary<string, string>
            {
                { BILLING_ID_ANNOTATION_NAME, billingId }
            });
        }

        /// <summary>
        /// Adds a policy failure action and exit codes to the job.
        /// </summary>
        /// <param name="job">The job object to modify.</param>
        /// <param name="actions">A list with all action names and corresponding exit code number.</param>
        /// <returns>The Kubernetes Job object with added pod failure policy rules.</returns>>
        public static V1Job WithPodPolicyFailureExitCodes(this V1Job job, Dictionary<string, List<int>> actions)
        {
            job.Spec ??= new V1JobSpec();
            job.Spec.PodFailurePolicy ??= new V1PodFailurePolicy();

            var auxActions = new Dictionary<string, List<int>>(actions);
            var updatedRules = new List<V1PodFailurePolicyRule>();

            if (job.Spec.PodFailurePolicy.Rules != null)
            {
                foreach (var rule in job.Spec.PodFailurePolicy.Rules)
                {
                    if (auxActions.ContainsKey(rule.Action))
                    {
                        rule.OnExitCodes.Values = rule.OnExitCodes.Values
                            .Concat(auxActions[rule.Action])
                            .Distinct()
                            .ToList();

                        auxActions.Remove(rule.Action);
                    }

                    updatedRules.Add(rule);
                }
            }

            foreach (var action in auxActions)
            {
                updatedRules.Add(new V1PodFailurePolicyRule
                {
                    Action = action.Key,
                    OnExitCodes = new V1PodFailurePolicyOnExitCodesRequirement
                    {
                        Values = action.Value.Distinct().ToList()
                    }
                });
            }

            job.Spec.PodFailurePolicy.Rules = updatedRules;

            return job;
        }

        /// <summary>
        /// Clones a job object.
        /// </summary>
        /// <param name="job">V1Job to clone.</param>
        /// <returns>New V1Job object.</returns>
        public static V1Job Clone(this V1Job job) =>
            JsonSerializer.Deserialize<V1Job>(JsonSerializer.Serialize(job));

        /// <summary>
        /// Checks if the job is completed.
        /// </summary>
        /// <param name="job">V1Job object to test</param>
        /// <returns>True if the job in completed state</returns>
        public static bool IsCompleted(this V1Job job)
            => job.Status?.Conditions != null && job.Status.Conditions.Any(IsCompleteCondition);

        /// <summary>
        /// Checks if the job is failed.
        /// </summary>
        /// <param name="job">V1Job object to test</param>
        /// <returns>True if the job in failed state</returns>
        public static bool IsFailed(this V1Job job)
            => job.Status?.Conditions != null && job.Status.Conditions.Any(IsFailedCondition);

        /// <summary>
        /// Checks if the job is running
        /// </summary>
        /// <param name="job">V1Job object to test</param>
        /// <returns>True if the job in running state</returns>
        public static bool IsRunning(this V1Job job)
            => job.Status?.Conditions == null;

        /// <summary>
        /// Checks if pod has BillingId annotation
        /// </summary>
        /// <param name="pod">V1Job object to test</param>
        /// <returns>True pod contains BillingId</returns>
        public static bool HasBillingId(this V1Pod pod)
            => pod?.Metadata?.Annotations != null &&
               pod.Metadata.Annotations.ContainsKey(BILLING_ID_ANNOTATION_NAME);

        /// <summary>
        /// Checks if pod has BillingId annotation
        /// </summary>
        /// <param name="pod">V1Job object to test</param>
        /// <returns>True pod contains BillingId</returns>
        public static string GetBillingId(this V1Pod pod)
            => pod.HasBillingId() ? pod.Metadata.Annotations[BILLING_ID_ANNOTATION_NAME] : default;

        private static bool IsCompleteCondition(this V1JobCondition c) =>
            c != null
            && c.Type.Equals("Complete", StringComparison.OrdinalIgnoreCase)
            && c.Status.Equals("True", StringComparison.OrdinalIgnoreCase);

        private static bool IsFailedCondition(this V1JobCondition c) =>
            c != null
            && c.Type.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            && c.Status.Equals("True", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Applies a retry policy for HTTP 429 errors when making a Kubernetes API call.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the Kubernetes API call.</typeparam>
        /// <typeparam name="TCaller">The type of the caller that is making the Kubernetes API call.</typeparam>
        /// <param name="k8SApiCall">A function that produces a new task for the Kubernetes API call.</param>
        /// <param name="retryLogger">An instance of an ILogger that logs retry attempts.</param>
        /// <param name="cancellationToken">An optional CancellationToken to proxy to the task and cancel retry attempts.</param>
        /// <returns>A task that will make the Kubernetes API call with the retry policy applied.</returns>
        /// <exception cref="HttpOperationException">Thrown if an HTTP error occurs when making the Kubernetes API call.</exception>
        public static Task<TResult> RetryHttp429<TResult, TCaller>(
            this Func<CancellationToken, Task<TResult>> k8SApiCall,
            ILogger<TCaller> retryLogger,
            CancellationToken cancellationToken = default
        )
        {
            var policy = Policy
                .Handle<HttpOperationException>(ex => ex.Response.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: int.Parse(
                        Environment.GetEnvironmentVariable("PROTEUS__K8S_HTTP_429_RETRY_COUNT") ?? "3"),
                    sleepDurationProvider: (_, ex, _) =>
                    {
                        var defaultRetry =
                            Environment.GetEnvironmentVariable("PROTEUS__K8S_HTTP_429_RETRY_INTERVAL") ??
                            "3";
                        var requestedRetry = (ex as HttpOperationException)?.Response.Headers
                            .GetOrElse("Retry-After", new List<string>()).FirstOrDefault();
                        return string.IsNullOrEmpty(requestedRetry)
                            ? TimeSpan.FromSeconds(int.Parse(defaultRetry))
                            : TimeSpan.FromSeconds(int.Parse(requestedRetry));
                    },
                    onRetryAsync: (exception, span, _, _) =>
                    {
                        retryLogger.LogWarning(exception,
                            "API Server responded with HTTP 429. Will retry in {retryInSeconds} seconds",
                            span.TotalSeconds);
                        return Task.CompletedTask;
                    });

            return k8SApiCall.WithRetryPolicy(policy, cancellationToken);
        }

        /// <summary>
        /// Applies a retry policy for HTTP exceptions caused by underlying transport level-errors when making a Kubernetes API call.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the Kubernetes API call.</typeparam>
        /// <typeparam name="TCaller">The type of the caller that is making the Kubernetes API call.</typeparam>
        /// <param name="k8SApiCall">A function that produces a new task for the Kubernetes API call.</param>
        /// <param name="retryLogger">An instance of an ILogger that logs retry attempts.</param>
        /// <param name="cancellationToken">An optional CancellationToken to proxy to the task and cancel retry attempts.</param>
        /// <returns>A task that will make the Kubernetes API call with the retry policy applied.</returns>
        /// <exception cref="HttpOperationException">Thrown if an HTTP error occurs when making the Kubernetes API call.</exception>
        public static Task<TResult> RetryConnectionError<TResult, TCaller>(
            this Func<CancellationToken, Task<TResult>> k8SApiCall,
            ILogger<TCaller> retryLogger,
            CancellationToken cancellationToken = default
        )
        {
            var policy = Policy
                .Handle<HttpRequestException>(ex => ex.InnerException is IOException)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: (_, _, _) => TimeSpan.FromSeconds(0.5d),
                    onRetryAsync: (exception, span, _, _) =>
                    {
                        retryLogger.LogWarning(exception,
                            "Transport level error occured when connecting to the API Server. Will retry in {retryInSeconds} seconds",
                            span.TotalSeconds);
                        return Task.CompletedTask;
                    });

            return k8SApiCall.WithRetryPolicy(policy, cancellationToken);
        }
    }
}