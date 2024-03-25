using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Akka.Streams;
using Akka.Streams.Dsl;
using k8s;
using k8s.Models;
using Snd.Sdk.Kubernetes.Streaming.Sources;
using Xunit;

namespace Snd.Sdk.Tests.Kubernetes;

public class JobSourceTests : IClassFixture<AkkaFixture>
{
    private readonly AkkaFixture akkaFixture;
    private readonly int maxBufferCapacity;
    private readonly int eventsCount;
    private readonly OverflowStrategy overflowStrategy;

    public JobSourceTests(AkkaFixture akkaFixture)
    {
        this.akkaFixture = akkaFixture;
        this.maxBufferCapacity = 10;
        this.eventsCount = 100;
        this.overflowStrategy = OverflowStrategy.Fail;
    }

    [Fact]
    public async Task ShouldStreamUpdatesSequence()
    {
        var jobSource = KubernetesResourceEventSource<V1Job>.Create(
            (onEvent, onError, onClose) =>
                new Watcher<V1Job>(CreateEvents, onEvent, onError, onClose), this.maxBufferCapacity, this.overflowStrategy);
        var count = await jobSource
            .Take(this.eventsCount)
            .RunAggregate(0, (agg, _) => agg + 1, this.akkaFixture.Materializer);
        Assert.Equal(this.eventsCount, count);
    }

    [Fact]
    public async Task ShouldDisposeWatcherOnStop()
    {
        Watcher<V1Job> watcher = null;
        var jobSource = KubernetesResourceEventSource<V1Job>.Create((onEvent, onError, onClose) =>
        {
            watcher = new Watcher<V1Job>(CreateEvents, onEvent, onError, onClose);
            return watcher;
        }, this.maxBufferCapacity, this.overflowStrategy);
        await jobSource.Take(this.eventsCount).RunForeach(_ => { }, this.akkaFixture.Materializer);
        Assert.False(watcher.Watching);
    }

    [Fact]
    public async Task ShouldFailStreamOnException()
    {
        await Assert.ThrowsAsync<JsonException>(async ()
            => await KubernetesResourceEventSource<V1Job>.Create((onEvent, onError, onClose) =>
                    new Watcher<V1Job>(GenerateError, onEvent, onError, onClose), this.maxBufferCapacity, this.overflowStrategy)
                .RunForeach(_ => { }, this.akkaFixture.Materializer));
    }

    [Fact]
    public async Task ShouldThrowOnIncorrectBufferSize()
    {
        await Assert.ThrowsAsync<ArgumentException>(async ()
            => await KubernetesResourceEventSource<V1Job>.Create((onEvent, onError, onClose) =>
                    new Watcher<V1Job>(GenerateError, onEvent, onError, onClose), -1,
                    this.overflowStrategy)
                .RunForeach(_ => { }, this.akkaFixture.Materializer));
    }

    [Fact]
    public async Task ShouldFailOnException()
    {
        await Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await KubernetesResourceEventSource<V1Job>.Create((_, onError, onClose) =>
                    new Watcher<V1Job>(CreateEvents, (_, _) => throw new NullReferenceException(), onError, onClose),
                    this.maxBufferCapacity, this.overflowStrategy)
                .Throttle(1, TimeSpan.FromSeconds(10), 1, ThrottleMode.Shaping)
                .RunWith(Sink.Last<(WatchEventType, V1Job)>(), this.akkaFixture.Materializer);
        });
    }

    [Fact]
    public async Task ShouldNotSupportBackpressure()
    {
        var ex = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await KubernetesResourceEventSource<V1Job>.Create((onEvent, onError, onClose)
                        => new Watcher<V1Job>(CreateEvents, onEvent, onError, onClose),
                    this.maxBufferCapacity,
                    OverflowStrategy.Backpressure)
                .Throttle(1, TimeSpan.FromSeconds(10), 1, ThrottleMode.Shaping)
                .RunWith(Sink.Last<(WatchEventType, V1Job)>(), this.akkaFixture.Materializer);
        });
        Assert.Equal("OverflowStrategy.Backpressure is not supported", ex.Message);
    }

    private static Task<StreamReader> GenerateError()
    {
        return Task.FromResult(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("not_a_json"))));
    }

    private Task<StreamReader> CreateEvents()
    {
        var jobsArray = Enumerable
            .Range(0, this.eventsCount)
            .Select(index => JsonSerializer.Serialize(CreateIndexedEvent(index)));
        var mockHttpResponse = string.Join("\n", jobsArray.ToArray());
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(mockHttpResponse));
        return Task.FromResult(new StreamReader(stream));
    }

    private Watcher<V1Job>.WatchEvent CreateIndexedEvent(int index)
    {
        return new Watcher<V1Job>.WatchEvent
        {
            Type = WatchEventType.Added,
            Object = new V1Job(metadata: new V1ObjectMeta(name: $"job_{index}"))
        };
    }
}
