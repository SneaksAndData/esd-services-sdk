using Akka;
using Akka.Streams;
using Akka.Streams.Azure.Utils;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using StackExchange.Redis;
using Decider = Akka.Streams.Supervision.Decider;
using Directive = Akka.Streams.Supervision.Directive;

namespace Snd.Sdk.Storage.Cache.Streaming;

/// <summary>
///   A <see cref="Source{TOut,TMat}"/> for Redis Stream reader.
///   This class is responsible for reading data from a Redis stream.
/// </summary>
public class RedisStreamConsumerSource : GraphStage<SourceShape<StreamEntry>>
{
    /// <summary>
    /// Create Redis Stream Consumer source
    /// </summary>
    /// <returns></returns>
    public static Source<StreamEntry, NotUsed> Create(string streamName,
        IConnectionMultiplexer redis, string initialId = "0-0", int count = 1,CommandFlags flags = CommandFlags.None, TimeSpan? pollInterval = null)
    {
        return Source.FromGraph(new RedisStreamConsumerSource(streamName, redis, initialId, count, flags, pollInterval));
    }

    private readonly string streamName;
    private readonly IConnectionMultiplexer redis;
    private readonly string initialId;
    private readonly int count;
    private readonly CommandFlags flags;
    private readonly TimeSpan pollInterval;

    public RedisStreamConsumerSource(string streamName, IConnectionMultiplexer redis, string initialId, int? count,  CommandFlags? flags, TimeSpan? pollInterval = null)
    {
        this.streamName = streamName;
        this.redis = redis;
        this.initialId = initialId;
        this.count = count ?? 1;
        this.flags = flags ?? CommandFlags.None;
        this.pollInterval = pollInterval ?? TimeSpan.FromSeconds(10);

        this.Shape = new SourceShape<StreamEntry>(Out);
    }

    /// <inheritdoc/>
    protected override Attributes InitialAttributes { get; } = Attributes.CreateName(nameof(RedisStreamConsumerSource));

    public Outlet<StreamEntry> Out { get; } = new("RedisStreamConsumerSource.Out");

    /// <inheritdoc/>
    public override SourceShape<StreamEntry> Shape { get; }

    /// <inheritdoc/>
    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) =>
        new StreamConsumerLogic(this, inheritedAttributes);

    // Inner class for the logic of the stream consumer
    public class StreamConsumerLogic : TimerGraphStageLogic
    {
        private const string TimerKey = "PollTimer";

        private readonly Decider _decider;
        private readonly RedisStreamConsumerSource _source;
        private Action<Task<StreamEntry[]>> _messagesReceived;

        public StreamConsumerLogic(RedisStreamConsumerSource source, Attributes attributes) : base(source.Shape)
        {
            this._source = source;
            this._decider = attributes.GetDeciderOrDefault();

            SetHandler(this._source.Out, PullStream);
        }

        // Method to pull data from the stream
        public void PullStream() => _source.redis.GetDatabase().StreamReadAsync(_source.streamName, _source.initialId, _source.count,  _source.flags)
            .ContinueWith(_messagesReceived);


        // Method called when a timer event occurs
        protected override void OnTimer(object timerKey) => PullStream();

        // Method called when the stage is started
        public override void PreStart()
        {
            _messagesReceived = GetAsyncCallback<Task<StreamEntry[]>>(OnMessageReceived);
            PullStream();
        }

        // Method called when a message is received from the stream
        public void OnMessageReceived(Task<StreamEntry[]> task)
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                if (_decider(task.Exception) == Directive.Stop)
                    FailStage(task.Exception);
                else
                    ScheduleOnce(TimerKey, _source.pollInterval);
                return;
            }

            // Try again if the stream in empty
            if (task.Result == null || !task.Result.Any())
                ScheduleOnce(TimerKey, _source.pollInterval);
            else
                EmitMultiple(_source.Out, task.Result);
        }
    }
}
