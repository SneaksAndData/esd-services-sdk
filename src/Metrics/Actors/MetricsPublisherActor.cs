using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Snd.Sdk.Metrics.Base;

namespace SnD.Sdk.Metrics.Actors;

/// <summary>
/// Add metric to the periodic metrics collection
/// </summary>
/// <param name="Key">
/// Key associated with the metric. If the second message with the same key sent, it will
/// overwrite exiting metric.
/// </param>
/// <param name="MetricName">Name of the metric to report</param>
/// <param name="MetricTags">Tags of the metric to report</param>
/// <param name="MetricValue">Value of the metric to report</param>
public record AddMetricMessage(string Key, string MetricName, SortedDictionary<string, string> MetricTags, int MetricValue = 1);


/// <summary>
/// Remove stream class metrics message. Once received, the metrics will be removed from the 
/// metrics collection in the <see cref="Key"/> actor.
/// </summary>
/// <param name="Key">Name of the stream kind referenced by the stream class</param>
public record RemoveMetricMessage(string Key);


/// <summary>
/// Emit metrics message. Once received, the metrics will be emitted to the metrics service.
/// This message is emitted periodically by the <see cref="MetricsPublisherActor"/> actor.
/// </summary>
public record EmitMetricsMessage;

/// <summary>
/// The metrics registration internal model.
/// This class is not intended to be used outside of the metrics actor.
/// </summary>
/// <param name="MetricName">Name of the metric to report</param>
/// <param name="MetricTags">Name of the metric to report</param>
/// <param name="MetricValue">Value of the metric to report</param>
internal record MetricRegistration(string MetricName, SortedDictionary<string, string> MetricTags, int MetricValue);


/// <summary>
/// Stream class service actor. This actor is responsible for collecting metrics for stream classes
/// that should be emitted periodically.
/// </summary>
public abstract class MetricsPublisherActor : ReceiveActor, IWithTimers
{
    /// <inheritdoc cref="IWithTimers.Timers"/>
    public ITimerScheduler Timers { get; set; }

    private readonly Dictionary<string, MetricRegistration> metrics = new();
    private readonly ILoggingAdapter Log = Context.GetLogger();
    private readonly TimeSpan initialDelay;
    private readonly TimeSpan emitInterval;
    private readonly MetricsService metricsService;

    /// <summary>
    /// Creates new instance of the <see cref="MetricsPublisherActor"/>
    /// </summary>
    /// <param name="initialDelay">Initial delay before begin to emit the metrics.</param>
    /// <param name="emitInterval">Interval to emit the metrics.</param>
    /// <param name="metricsService">The metrics service instance used to emit the metrics (<see cref="MetricsService"/>.</param>
    protected MetricsPublisherActor(TimeSpan initialDelay, TimeSpan emitInterval, MetricsService metricsService)
    {
        this.initialDelay = initialDelay;
        this.emitInterval = emitInterval;
        this.metricsService = metricsService;

        this.Receive<AddMetricMessage>(this.HandleAddMetricMessage);
        this.Receive<RemoveMetricMessage>(this.HandleRemoveMetricMessage);
        this.Receive<EmitMetricsMessage>(_ => this.HandleEmitMetricsMessage());
    }

    /// <summary>
    /// Inheritor should implement this method to emit the metrics with the provided <see cref="MetricsService"/>.
    /// <param name="name">Name of the metric to report</param>
    /// <param name="value">Value of the metric to report</param>
    /// <param name="tags">Name of the metric to report</param>
    /// <param name="metricsService">Metrics service used to emit the metrics.</param>
    /// </summary>
    protected abstract void EmitMetric(MetricsService metricsService, string name, int value,
        SortedDictionary<string, string> tags);

    private void HandleAddMetricMessage(AddMetricMessage m)
    {
        this.Log.Debug("Adding stream class metrics for {streamKindRef}", m.Key);
        if (m.MetricTags == null || m.MetricName == null || m.Key == null)
        {
            this.Log.Warning("Skip malformed {messageName} for {key} with value: {@message}",
                nameof(AddMetricMessage),
                m.Key,
                m);
            return;
        }
        this.metrics[m.Key] = new MetricRegistration(m.MetricName, m.MetricTags, m.MetricValue);
    }

    private void HandleRemoveMetricMessage(RemoveMetricMessage m)
    {
        if (!this.metrics.Remove(m.Key))
        {
            this.Log.Warning("Stream class {streamKindRef} not found in metrics collection", m.Key);
        }
    }

    private void HandleEmitMetricsMessage()
    {
        this.Log.Debug("Start emitting stream class metrics");
        foreach (var (_, metric) in this.metrics)
        {
            try
            {
                this.EmitMetric(this.metricsService, metric.MetricName, metric.MetricValue, metric.MetricTags);
            }
            catch (Exception exception)
            {
                this.Log.Error(exception, "Failed to publish metrics for {streamKindRef}",
                    metric.MetricName);
            }
        }
    }

    /// <summary>
    /// Starts the timer before the actor starts processing messages.
    /// </summary>
    protected override void PreStart()
    {
        base.PreStart();
        this.Timers.StartPeriodicTimer(nameof(EmitMetricsMessage),
            new EmitMetricsMessage(),
            this.initialDelay,
            this.emitInterval);
    }
}
