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

public class MetricsActorTests: TestKit
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

        this.metricsServiceMock.Setup(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>())).Callback(() => this.tcs.TrySetResult());
    }


    [Fact]
    public async Task TestMetricsAdded()
    {
        // Arrange
        var subject = this.Sys.ActorOf(Props.Create(() => new TestMetricsPublisherActor(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10), this.metricsServiceMock.Object)),
            nameof(MetricsPublisherActor));
        var message = new AddMetricMessage("test", "test", new());

        // Act
        subject.Tell(message);
        subject.Tell(new EmitMetricsMessage());
        await this.tcs.Task;
        
        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.AtLeast(1));
    }
    
    [Fact]
    public async Task TestBrokenMetricsService()
    {
        // Arrange
        this.metricsServiceMock.Setup(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>())).Throws(new Exception("Test exception"));
        
        var subject = this.Sys.ActorOf(Props.Create(() => new TestMetricsPublisherActor(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10), this.metricsServiceMock.Object)),
            nameof(MetricsPublisherActor));
        var message = new AddMetricMessage("test", "test", new());
        
        // Act
        subject.Tell(message);
        subject.Tell(new EmitMetricsMessage());
        subject.Tell(new EmitMetricsMessage());
        await this.tcs.Task;
        
        // Assert
        this.metricsServiceMock.Verify(ms => ms.Count(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<SortedDictionary<string, string>>()), Times.AtLeast(2));
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this.cts?.Dispose();
    }
    
    private class TestMetricsPublisherActor : MetricsPublisherActor
    {
        public TestMetricsPublisherActor(TimeSpan initialDelay, TimeSpan emitInterval, MetricsService metricsService)
            : base(initialDelay, emitInterval, metricsService)
        {
        }

        protected override void EmitMetric(MetricsService metricsService,
            string name,
            int value,
            SortedDictionary<string, string> tags)
        {
            metricsService.Count(name, value, tags);
        }
    }

}
