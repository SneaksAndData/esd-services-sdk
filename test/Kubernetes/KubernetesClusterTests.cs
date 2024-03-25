using Akka.Streams.Dsl;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Moq;
using Snd.Sdk.Kubernetes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Snd.Sdk.Tests.Comparers;
using Xunit;

namespace Snd.Sdk.Tests.Kubernetes
{
    public class KubernetesClusterTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;
        private readonly Mock<IKubernetes> mockApi;
        private readonly KubernetesCluster mockMember;

        private delegate void CacheGet(object key, out object result);

        public KubernetesClusterTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
            this.mockApi = new Mock<IKubernetes>();
            this.mockMember = KubernetesCluster.CreateFromApi(nameof(KubernetesClusterTests), this.mockApi.Object,
                this.loggerFixture.Factory);
        }


        [Fact]
        public async Task SendCommandToPod()
        {
            var mockDemuxed = new Mock<IStreamDemuxer>();
            var stdoutStream = new MemoryStream(Encoding.UTF8.GetBytes("command result"));
            var stderrStream = new MemoryStream(Encoding.UTF8.GetBytes("something here"));
            var errStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new V1Status
            { Code = 200, Status = "Success", Message = "All good" })));
            var dummyCommandParts = new List<string>();

            mockDemuxed.Setup(md => md.GetStream(ChannelIndex.StdOut, null)).Returns(stdoutStream);
            mockDemuxed.Setup(md => md.GetStream(ChannelIndex.StdErr, null)).Returns(stderrStream);
            mockDemuxed.Setup(md => md.GetStream(ChannelIndex.Error, null)).Returns(errStream);
            this.mockApi.Setup(k => k.MuxedStreamNamespacedPodExecAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(mockDemuxed.Object));

            var result = await Source.Single(dummyCommandParts.AsEnumerable())
                .Via(this.mockMember.SendCommandToPod("", ""))
                .RunWith(Sink.Seq<(string, string, int)>(), this.akkaFixture.Materializer);

            if (result.Count != 1)
            {
                throw new Exception($"Incorrect number of lines returned from {nameof(SendCommandToPod)}");
            }


            Assert.Equal("command result", result.FirstOrDefault().Item1);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(0)]
        public async Task ListPods(int expectedPodCount)
        {
            var mockPods = Enumerable.Range(0, expectedPodCount)
                .Select(ix => new V1Pod { Metadata = new V1ObjectMeta { Name = $"pod-{ix}" } }).ToList();

            this.mockApi.Setup(k => k.CoreV1.ListNamespacedPodWithHttpMessagesAsync(It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string,
                        IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1PodList>()
                { Body = new V1PodList { Items = mockPods } }));

            var result = await this.mockMember.ListPods("some-ns")
                .RunWith(Sink.Seq<V1Pod>(), this.akkaFixture.Materializer);

            Assert.Equal(expectedPodCount, result.Count);
        }

        [Theory]
        [InlineData(1)]
        public async Task ScaleOutStatefulSet(int replicas)
        {
            var inputList = new V1StatefulSetList(new List<V1StatefulSet>
            {
                new V1StatefulSet
                {
                    Metadata = new V1ObjectMeta { Name = nameof(ScaleOutStatefulSet) }
                }
            });

            var notReadySet = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta { Name = nameof(ScaleOutStatefulSet) },
                Status = new V1StatefulSetStatus
                {
                    ReadyReplicas = null,
                    Replicas = replicas
                }
            };

            var readySet = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta { Name = nameof(ScaleOutStatefulSet) },
                Status = new V1StatefulSetStatus
                {
                    ReadyReplicas = replicas,
                    Replicas = replicas
                }
            };

            this.mockApi.Setup(k => k.AppsV1.PatchNamespacedStatefulSetScaleWithHttpMessagesAsync(It.IsAny<V1Patch>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string,
                        IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1Scale>() { Body = new V1Scale() }));

            this.mockApi.Setup(k => k.AppsV1.ListNamespacedStatefulSetWithHttpMessagesAsync(It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string, IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1StatefulSetList> { Body = inputList }));

            this.mockApi.SetupSequence(k => k.AppsV1.ReadNamespacedStatefulSetStatusWithHttpMessagesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string,
                        IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1StatefulSet> { Body = notReadySet }))
                .Returns(Task.FromResult(new HttpOperationResponse<V1StatefulSet> { Body = readySet }));

            var result = await this.mockMember
                .ScaleOutStatefulSet(nameof(ScaleOutStatefulSet), "test", 1, TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5), 0.1, 3, TimeSpan.FromSeconds(15))
                .RunWith(Sink.Seq<V1StatefulSet>(), this.akkaFixture.Materializer);

            Assert.Equal(replicas, result.FirstOrDefault().Status.ReadyReplicas);
        }

        [Theory]
        [InlineData("label/sublabel", "somevalue", "pod", "ns")]
        public async Task LabelPod(string labelKey, string labelValue, string podName, string podNamespace)
        {
            this.mockApi.Setup(k => k.CoreV1.PatchNamespacedPodWithHttpMessagesAsync(It.IsAny<V1Patch>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string,
                        IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1Pod> { Body = new V1Pod() }));

            var r = await this.mockMember.LabelPod(labelKey, labelValue, podName, podNamespace);

            this.mockApi.Verify(k => k.CoreV1.PatchNamespacedPodWithHttpMessagesAsync(
                It.Is<V1Patch>(v =>
                    (v.Content as string).Contains($"/metadata/labels/{labelKey.Replace("/", "~1")}") &&
                    (v.Content as string).Contains(labelValue)),
                It.Is<string>(v => v == podName),
                It.Is<string>(v => v == podNamespace),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<Dictionary<string,
                    IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()));
        }

        [Theory]
        [InlineData("label/sublabel", "pod", "ns")]
        public async Task RemoveLabelFromPod(string labelKey, string podName, string podNamespace)
        {
            this.mockApi.Setup(k => k.CoreV1.PatchNamespacedPodWithHttpMessagesAsync(It.IsAny<V1Patch>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string,
                        IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1Pod> { Body = new V1Pod() }));

            var r = await this.mockMember.RemoveLabelFromPod(labelKey, podName, podNamespace);

            this.mockApi.Verify(k => k.CoreV1.PatchNamespacedPodWithHttpMessagesAsync(
                It.Is<V1Patch>(v =>
                    (v.Content as string).Contains($"/metadata/labels/{labelKey.Replace("/", "~1")}") &&
                    (v.Content as string).Contains("remove")),
                It.Is<string>(v => v == podName),
                It.Is<string>(v => v == podNamespace),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<Dictionary<string,
                    IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ListStatefulSets()
        {
            var cs = new V1Container();

            var mockNs = "testNs";

            var mockSets = Enumerable.Range(0, 5).Select(ix =>
            {
                return new V1StatefulSet
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"test{ix}",
                        NamespaceProperty = mockNs
                    },
                    Spec = new V1StatefulSetSpec
                    {
                        Template = new V1PodTemplateSpec
                        {
                            Spec = new V1PodSpec
                            {
                                Containers = new List<V1Container>
                                {
                                    cs
                                }
                            }
                        }
                    },
                    Status = new V1StatefulSetStatus
                    {
                        Replicas = 1
                    }
                };
            });

            this.mockApi.Setup(k => k.AppsV1.ListNamespacedStatefulSetWithHttpMessagesAsync(It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string, IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1StatefulSetList>
                { Body = new V1StatefulSetList(mockSets.ToList()) }));

            var result = await this.mockMember.ListStatefulSets(mockNs);

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetJobs()
        {
            var firstBatch = new V1JobList
            {
                Items = Enumerable.Range(0, 5).Select(_ => new V1Job()).ToList(),
                Metadata = new V1ListMeta { ContinueProperty = "nextpage1" }
            };
            var secondBatch = new V1JobList
            {
                Items = Enumerable.Range(0, 5).Select(_ => new V1Job()).ToList(),
                Metadata = new V1ListMeta { ContinueProperty = null }
            };

            this.mockApi.SetupSequence(k8s => k8s.BatchV1.ListNamespacedJobWithHttpMessagesAsync("test",
                    It.IsAny<bool?>(), null, null, null, 5, null, null, null, null, null, null, null,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1JobList> { Body = firstBatch }));

            this.mockApi.Setup(k8s => k8s.BatchV1.ListNamespacedJobWithHttpMessagesAsync("test", It.IsAny<bool?>(),
                    "nextpage1", null, null, 5, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpOperationResponse<V1JobList> { Body = secondBatch }));

            var jobList = await this.mockMember.GetJobs("test", 5)
                .RunWith(Sink.Seq<V1Job>(), this.akkaFixture.Materializer);

            Assert.Equal(10, jobList.Count);
        }

        public static List<object[]> CreateJobTestCases = new()
        {
            new object[]
            {
                "myrepo.azurecr.io/unittest:0.0.0",
                "0117c233-4614-455f-8fe8-4e55166ffc60",
                new Dictionary<string, string>
                {
                    { "ENV1", "value1" },
                    { "ENV2", "value2" }
                },
                new List<string>
                {
                    "crystal-omnichannel"
                },
                new List<string>
                {
                    "/bin/sh",
                    "-c"
                },
                new List<string>
                {
                    "echo 1"
                },
                new Dictionary<string, string>
                {
                    { "cpu", "800m" },
                    { "memory", "6000Mi" }
                },
                900,
                10,
                new Dictionary<string, string>
                {
                    { "app.kubernetes.io/component", "unittest" },
                    { "app.kubernetes.io/instance", "algorithm" },
                },
                "unittest",
                "kubernetes.unittest.com/nodetype",
                "job1",
                "serviceAccount"
            }
        };

        [Theory]
        [MemberData(nameof(CreateJobTestCases))]
        public void CreateJob(string imageName,
            string jobName,
            Dictionary<string, string> envVars,
            List<string> secretEnvVarFrom,
            List<string> commands,
            List<string> jobArgs,
            Dictionary<string, string> jobResources,
            int deadlineSeconds,
            int retries,
            Dictionary<string, string> labels,
            string customTaint,
            string customTaintKey,
            string expectedJsonPath,
            string serviceAccountName)
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            opts.Converters.Add(new ResourceQuantityConverter());

            var expectedJob = JsonSerializer.Deserialize<V1Job>(
                File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", expectedJsonPath)),
                opts
            );

            var job = KubernetesApiExtensions.CreateJob(
                    imageName: imageName,
                    jobName: jobName,
                    jobResources: jobResources)
                .WithDeadline(deadlineSeconds)
                .WithTtlAfterFinished(900)
                .WithServiceAccount(serviceAccountName)
                .WithRetries(retries)
                .WithSpecificNodes(customTaintKey, customTaint)
                .WithLabels(labels)
                .WithCustomEnvironment(envVars)
                .WithCustomEnvironment(secretEnvVarFrom)
                .WithCustomEntrypoint(commands, jobArgs);

            Assert.Equal(expectedJob, job, new V1JobEqualityComparer());
        }

        [Theory]
        [InlineData(5, "test-pod")]
        [InlineData(1, null)]
        public async Task GetPods(int timeoutSeconds, string expectedPodName)
        {
            this.mockApi.SetupSequence(k8s => k8s.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
                    It.Is<string>(ns => ns == "test"),
                    It.IsAny<bool?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<string>(ls => ls == "test-label-a=test-label-a-value,test-label-b=test-label-b-value"),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpOperationException { Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(1)) } }, content: string.Empty) })
                .ReturnsAsync(new HttpOperationResponse<V1PodList>
                {
                    Body = new V1PodList(items: new List<V1Pod> { new(metadata: new V1ObjectMeta(name: "test-pod")) }),
                    Response = new HttpResponseMessage(HttpStatusCode.OK)
                });

            Task<List<V1Pod>> ResultFunc()
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                Thread.Sleep(2000);
                return this.mockMember.GetPods(new Dictionary<string, string> { { "test-label-a", "test-label-a-value" }, { "test-label-b", "test-label-b-value" }, }, "test", cts.Token);
            }

            var result = await ResultFunc();

            Assert.Equal(expectedPodName, result.Count > 0 ? result[0].Name() : null);
        }

        [Theory]
        [InlineData(1, null)]
        [InlineData(3, "CANCELLED")]
        public async Task DeleteJob(int timeoutSeconds, string expectedJobStatus)
        {

            this.mockApi.SetupSequence(k8s => k8s.BatchV1.DeleteNamespacedJobWithHttpMessagesAsync(
                    It.IsAny<string>(),
                    It.Is<string>(ns => ns == "test"),
                    It.IsAny<V1DeleteOptions>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string, IReadOnlyList<string>>>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(new HttpOperationResponse<V1Status>
                {
                    Body = new V1Status("1.2", null, null, null, null, null, null, expectedJobStatus),
                    Response = new HttpResponseMessage(HttpStatusCode.OK)
                }
                );


            Task<V1Status> ResultFunc()
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                Thread.Sleep(2000);
                return this.mockMember.DeleteJob("123", "test", cts.Token);

            }

            try
            {
                var result = await ResultFunc();
                Assert.Equal(expectedJobStatus, result is not null ? result.Status : null);
            }
            catch (OperationCanceledException)
            {
                Assert.True(true);
            }
        }
    }


    public class ResourceQuantityConverter : JsonConverter<ResourceQuantity>
    {
        public override ResourceQuantity Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new ResourceQuantity(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, ResourceQuantity value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
