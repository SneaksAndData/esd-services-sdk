## Actors related to metrics reporting

### Usage

This module contains the `MetricsPublisherActor` base class that can be used to emit a certain type of metrics
periodically using Akka.net.

To use this class user should inherit from it and implement the `EmitMetric` method.
This method will be called periodically by the actor.

You can see an example of how to use this class in the following code snippet:
```csharp
using System;
using System.Collections.Generic;
using SnD.Sdk.Metrics.Actors;
using Snd.Sdk.Metrics.Base;

namespace Arcane.Operator.Services.Metrics.Actors;

/// <summary>
/// The actor that emits a single value metric for each stream class that is online.
/// </summary>
public class StreamClassOnlineMetricsPublisherActor : MetricsPublisherActor
{
    public StreamClassOnlineMetricsPublisherActor(TimeSpan initialDelay, TimeSpan emitInterval, MetricsService metricsService)
        : base(initialDelay, emitInterval, metricsService)
    {
    }

    /// <inheritdoc cref="MetricsPublisherActor.EmitMetric"/>
    protected override void EmitMetric(MetricsService metricsService, string name, int value, SortedDictionary<string, string> tags)
    {
        metricsService.Count(name, value, tags);
    }
}
```

#### Using with .NET Core Dependency Injection

Since all actors in Akka.NET are represented using actor reverences with the `IActorRef` interface, you can not
inject them directly into your services. The recommended approach is wrapping the actor reference in a service:

```csharp
using Arcane.Operator.Models.Commands;

namespace Arcane.Operator.Services.Base.Metrics;

/// <summary>
/// Interface for reporting metrics.
/// </summary>
public interface IMetricsReporter
{
    /// <summary>
    /// Report status metrics for a StreamClass object
    /// </summary>
    /// <param name="command">StreamClassOperatorResponse object with StreamClass status information</param>
    /// <returns>The same object for processing in the next stages of operator state machine.</returns>
    SetStreamClassStatusCommand ReportStatusMetrics(SetStreamClassStatusCommand command);
}
```

```csharp
using Arcane.Operator.Models.Commands;

namespace Arcane.Operator.Services.Base.Metrics;

/// <summary>
/// Interface for reporting metrics.
/// </summary>
public interface IMetricsReporter
{
    /// <summary>
    /// Report status metrics for a StreamClass object
    /// </summary>
    /// <param name="command">StreamClassOperatorResponse object with StreamClass status information</param>
    /// <returns>The same object for processing in the next stages of operator state machine.</returns>
    SetStreamClassStatusCommand ReportStatusMetrics(SetStreamClassStatusCommand command);
}
```

The implementation of the `IMetricsReporter` interface that owns the actor can be as follows:

```csharp
/// <summary>
/// The IMetricsReporter implementation.
/// </summary>
public class MetricsReporter : IMetricsReporter
{
    private readonly MetricsService metricsService;
    private readonly IActorRef statusActor;

    public MetricsReporter(MetricsService metricsService, ActorSystem actorSystem,
        IOptions<MetricsReporterConfiguration> metricsReporterConfiguration)
    {
        this.metricsService = metricsService;
        this.statusActor = actorSystem.StartMetricsPublisher(() =>
            new StreamClassOnlineMetricsPublisherActor(
                metricsReporterConfiguration.Value.MetricsPublisherActorConfiguration.InitialDelay,
                metricsReporterConfiguration.Value.MetricsPublisherActorConfiguration.UpdateInterval,
                this.metricsService));
    }

    /// <inheritdoc cref="IMetricsReporter.ReportStatusMetrics"/>
    public SetStreamClassStatusCommand ReportStatusMetrics(SetStreamClassStatusCommand command)
    {
        if (command.phase.IsFinal())
        {
            this.statusActor.Tell(new RemoveMetricMessage(command.streamClass.KindRef));
        }
        else
        {
            var msg = new AddMetricMessage(command.streamClass.KindRef, "stream_class",
                command.GetMetricsTags());
            this.statusActor.Tell(msg);
        }

        return command;
    }
}

```

With this approach, you can inject the `IMetricsReporter` interface into your services and use it to report metrics:
    
```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the metrics providers
        services.AddDatadogMetrics(DatadogConfiguration.UnixDomainSocket(AppDomain.CurrentDomain.FriendlyName));
        services.AddSingleton<IMetricsReporter, MetricsReporter>();
    }
```
