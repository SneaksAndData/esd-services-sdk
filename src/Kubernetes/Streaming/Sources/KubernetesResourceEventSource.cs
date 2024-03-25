using System;
using System.IO;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Snd.Sdk.Kubernetes.Streaming.Sources
{
    /// <summary>
    /// Akka Source for Kubernetes resource events.
    /// </summary>
    public class KubernetesResourceEventSource<T> : GraphStage<SourceShape<(WatchEventType, T)>> where T : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Watcher factory
        /// <param name="onMessage">Invoked when a message received</param>
        /// <param name="onError">Invoked when an exception thrown</param>
        /// <param name="onClose">Invoked when an exception thrown</param>
        /// </summary>
        public delegate Watcher<T> WatcherFactory(Action<WatchEventType, T> onMessage,
            Action<Exception> onError,
            Action onClose);

        /// <inheritdoc/>
        public override SourceShape<(WatchEventType, T)> Shape { get; }

        /// <summary>
        /// Create Job source
        /// </summary>
        /// <param name="watcherFactory">Watcher factory</param>
        /// <param name="maxBufferCapacity">Maximum capacity of the buffer</param>
        /// <param name="overflowStrategy">Overflow strategy</param>
        /// <param name="reconnectDelay">Interval for check if watcher is alive and recreate
        /// if watcher stopped watching for events. Defaults to 1 minute.</param>
        /// <param name="logger">Logger for logging events from the source.</param>
        /// <returns></returns>
        public static Source<(WatchEventType, T), NotUsed> Create(WatcherFactory watcherFactory,
            int maxBufferCapacity, OverflowStrategy overflowStrategy, TimeSpan? reconnectDelay = null, ILogger logger = null)
        {
            if (overflowStrategy == OverflowStrategy.Backpressure)
            {
                throw new NotSupportedException($"{nameof(OverflowStrategy)}.{overflowStrategy} is not supported");
            }
            return Source
                .FromGraph(new KubernetesResourceEventSource<T>(watcherFactory, reconnectDelay, logger))
                .Buffer(maxBufferCapacity, overflowStrategy);
        }

        /// <inheritdoc/>
        protected override Attributes InitialAttributes { get; } = Attributes.CreateName(nameof(KubernetesResourceEventSource<T>));

        /// <inheritdoc/>
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new JobSourceLogic(this);

        private readonly WatcherFactory watcherFactory;
        private readonly TimeSpan reconnectInterval;
        private readonly ILogger logger;

        private Outlet<(WatchEventType, T)> Out { get; } = new($"{nameof(KubernetesResourceEventSource<T>)}.Out");

        private KubernetesResourceEventSource(WatcherFactory watcherFactory, TimeSpan? reconnectInterval, ILogger logger)
        {
            this.Shape = new SourceShape<(WatchEventType, T)>(Out);
            this.watcherFactory = watcherFactory;
            this.reconnectInterval = reconnectInterval ?? TimeSpan.FromSeconds(5);
            this.logger = logger;
        }

        private class JobSourceLogic : TimerGraphStageLogic
        {
            private readonly LocalOnlyDecider decider;
            private readonly WatcherFactory watcherFactory;
            private readonly KubernetesResourceEventSource<T> kubernetesResourceEventSource;

            private readonly Action<(WatchEventType, T)> onWatcherEvent;
            private readonly Action<Exception> onWatcherFail;
            private readonly Action onWatcherClose;

            private Watcher<T> watcher;

            public JobSourceLogic(KubernetesResourceEventSource<T> kubernetesResourceEventSource) : base(kubernetesResourceEventSource.Shape)
            {
                this.watcherFactory = kubernetesResourceEventSource.watcherFactory;
                this.kubernetesResourceEventSource = kubernetesResourceEventSource;

                this.decider = Decider.From(
                    exception =>
                    {
                        return exception switch
                        {
                            // Kubernetes watcher throws EndOfStreamException when the connection is closed.
                            // This behavior will not be fixed in the near future.
                            // See: https://github.com/kubernetes-client/csharp/issues/893 for details.
                            { InnerException: EndOfStreamException _ } => Directive.Restart,
                            _ => Directive.Stop
                        };
                    });

                this.onWatcherEvent = GetAsyncCallback<(WatchEventType, T)>(OnWatcherEvent);
                this.onWatcherFail = GetAsyncCallback<Exception>(OnWatcherFail);
                this.onWatcherClose = GetAsyncCallback(CompleteStage);

                SetHandler(this.kubernetesResourceEventSource.Out, DoNothing, Finish);
            }

            public override void PreStart()
            {
                this.watcher = StartWatcher();
                ScheduleOnce(TIMER_KEY, this.kubernetesResourceEventSource.reconnectInterval);
            }

            private void OnWatcherEvent((WatchEventType, T) tuple)
            {
                Emit(kubernetesResourceEventSource.Out, tuple);
            }

            private void OnWatcherFail(Exception exception)
            {
                this.watcher?.Dispose();
                switch (decider.Decide(exception))
                {
                    case Directive.Stop:
                        Finish(exception);
                        break;
                    case Directive.Restart:
                        this.watcher = StartWatcher();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void Finish(Exception ex)
            {
                this.watcher?.Dispose();

                if (ex != null && ex is not SubscriptionWithCancelException.NonFailureCancellation)
                {
                    FailStage(ex);
                }
            }

            private Watcher<T> StartWatcher()
            {
                return this.watcherFactory(
                   (et, resource) => this.onWatcherEvent((et, resource)),
                   onWatcherFail,
                   onWatcherClose);
            }

            // Sometimes watcher stops watching for events. In case of this we need to recreate watcher.
            // This method checks if watcher is alive and recreate if watcher stopped watching for events.
            // See https://github.com/kubernetes-client/csharp/issues/533 for details.
            protected override void OnTimer(object timerKey)
            {
                if (this.watcher is { Watching: false })
                {
                    this.kubernetesResourceEventSource.logger?.LogWarning("Watcher is not watching for events. Recreate watcher");
                    this.watcher?.Dispose();
                    this.watcher = StartWatcher();
                }
                ScheduleOnce(TIMER_KEY, this.kubernetesResourceEventSource.reconnectInterval);
            }

            private const string TIMER_KEY = nameof(KubernetesResourceEventSource<T>);
        }
    }
}
