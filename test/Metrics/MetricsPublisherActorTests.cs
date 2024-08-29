using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Moq;
using SnD.Sdk.Metrics.Actors;
using Snd.Sdk.Metrics.Base;
using Xunit;

namespace SnD.Sdk.Tests.Metrics;

public class MetricsActorTests : TestKit
{
    // Akka service and test helpers
    private readonly TaskCompletionSource tcs = new();
    private readonly CancellationTokenSource cts = new();


    // Mocks
    private readonly Mock<MetricsService> metricsServiceMock = new();

    public MetricsActorTests()
    {
        this.cts.CancelAfter(TimeSpan.FromSeconds(5));
        this.cts.Token.Register(this.tcs.SetResult);

    }

    [Fact]
    public async Task TestMetricsAdded()
    {
        // Arrange
        this.metricsServiceMock.Setup(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>())).Callback(() => this.tcs.TrySetResult());
        var subject = this.Sys.StartMetricsPublisher(() => new TestMetricsPublisherActor(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10), this.metricsServiceMock.Object, null));

        // Act
        subject.Tell(new AddMetricMessage("test", "test", new()));
        subject.Tell(new AddMetricMessage("test2", "test", new()));
        subject.Tell(new EmitMetricsMessage());
        await this.tcs.Task;

        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TestBrokenMetricsService()
    {
        // Arrange
        this.metricsServiceMock.Setup(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>())).Callback(() => this.tcs.TrySetResult());
        this.metricsServiceMock.Setup(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>())).Throws(new Exception("Test exception"));

        var subject = this.Sys.StartMetricsPublisher(() => new TestMetricsPublisherActor(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10), this.metricsServiceMock.Object, null));

        // Act
        subject.Tell(new AddMetricMessage("test", "test", new()));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(new EmitMetricsMessage());
        await this.tcs.Task;

        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task TestRemoveMetricsMessage()
    {
        // Arrange
        var subject = this.Sys.StartMetricsPublisher(() => new TestMetricsPublisherActor(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10), this.metricsServiceMock.Object, this.tcs));

        // Act
        subject.Tell(new AddMetricMessage("test", "test", new()));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(new RemoveMetricMessage("test"));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(PoisonPill.Instance);
        await this.tcs.Task;

        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.Exactly(1));
    }

    [Fact]
    public async Task TestRemoveNonExisingMetric()
    {
        // Arrange
        var subject = this.Sys.StartMetricsPublisher(() => new TestMetricsPublisherActor(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10), this.metricsServiceMock.Object, this.tcs));

        // Act
        subject.Tell(new AddMetricMessage("test", "test", new()));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(new RemoveMetricMessage("not-exists"));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(PoisonPill.Instance);
        await this.tcs.Task;

        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TestBrokenMessage()
    {
        // Arrange
        var subject = this.Sys.StartMetricsPublisher(() => new TestMetricsPublisherActor(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10), this.metricsServiceMock.Object, this.tcs));

        // Act
        subject.Tell(new AddMetricMessage("test", "test", new()));
        subject.Tell(new AddMetricMessage(null, null, null));
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(PoisonPill.Instance);
        await this.tcs.Task;

        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.Exactly(1));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this.cts?.Dispose();
    }

    private class TestMetricsPublisherActor : MetricsPublisherActor
    {
        private readonly TaskCompletionSource tcs;
        public TestMetricsPublisherActor(TimeSpan initialDelay, TimeSpan emitInterval, MetricsService metricsService,
            TaskCompletionSource tcs)
            : base(initialDelay, emitInterval, metricsService)
        {
            this.tcs = tcs;
        }

        protected override void EmitMetric(MetricsService metricsService, string name, int value, SortedDictionary<string, string> tags)
        {
            metricsService.Count(name, value, tags);
        }

        protected override void PostStop()
        {
            this.tcs?.TrySetResult();
        }
    }
}
