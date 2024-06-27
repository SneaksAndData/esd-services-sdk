using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using Akka.Util;
using Snd.Sdk.Tasks;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Snd.Sdk.Hosting;
using Snd.Sdk.Storage.Streaming.Models;
using AvroSchema = Avro.Schema;

namespace Snd.Sdk.Storage.Streaming.Sources;

/// <summary>
/// Akka.Streams source for receiving messages from Apache Pulsar and Astra Streaming Topics.
/// Usage example:
/// var src = PulsarSource{JsonElement,JsonElement}.Create(
///   "pulsar+ssl://pulsar-....streaming.datastax.com:6651",
///   "streaming-tenant",
///   "streaming-namespace",
///   "streaming-topic",
///   TimeSpan.FromSeconds(5),
///   "subscription-name",
///   "https://pulsar-....api.streaming.datastax.com/admin/v2/schemas/streaming-tenant/streaming-namespace/streaming-topic/schema",
///    true);
/// </summary>
/// <typeparam name="TData">Type to deserialize data element to</typeparam>
/// <typeparam name="TKey">Type to deserialize key element to</typeparam>
public class PulsarSource<TKey, TData> : GraphStage<SourceShape<PulsarEvent<TKey, TData>>>
{
    private readonly Uri pulsarServiceUrl;
    private readonly string pulsarToken;
    private readonly string topic;
    private readonly TimeSpan changeCaptureInterval;
    private readonly string subscriptionName;
    private readonly string schemaUrl;
    private readonly bool autoAck;
    private readonly string consumerName;

    private PulsarSource(
        string pulsarServiceUrl,
        string pulsarToken,
        string tenantName,
        string pulsarNamespace,
        string topicName,
        string subscriptionName,
        string schemaUrl,
        bool autoAck,
        TimeSpan changeCaptureInterval,
        string consumerName = null
        )
    {
        this.pulsarServiceUrl = new Uri(pulsarServiceUrl);
        this.pulsarToken = pulsarToken;
        this.topic = $"persistent://{tenantName}/{pulsarNamespace}/{topicName}";
        this.changeCaptureInterval = changeCaptureInterval;
        this.subscriptionName = subscriptionName;
        this.schemaUrl = schemaUrl;
        this.autoAck = autoAck;
        this.consumerName = consumerName ?? $"{AppDomain.CurrentDomain.FriendlyName.ToLower()}-{Guid.NewGuid()}";

        Shape = new SourceShape<PulsarEvent<TKey, TData>>(Out);
    }

    /// <summary>  
    /// Creates an Akka Source from a complete PulsarSource graph, which reads messages from a Pulsar topic.  
    /// </summary>  
    /// <param name="pulsarServiceUrl">The URL of the Pulsar service.</param>  
    /// <param name="tenantName">The name of the tenant that owns the topic.</param>  
    /// <param name="pulsarNamespace">The namespace of the topic.</param>  
    /// <param name="topicName">The name of the topic to read from.</param>  
    /// <param name="changeCaptureInterval">The interval at which to capture changes.</param>  
    /// <param name="subscriptionName">The name of the subscription to use.</param>  
    /// <param name="schemaUrl">The URL of the Avro schema to use.</param>  
    /// <param name="autoAck">Whether to automatically acknowledge messages when they are read.</param>
    /// <param name="consumerName">Identifying name for the created/resumed consumer</param>
    /// <returns>  
    /// An Akka Source that emits the messages read from the Pulsar topic. Each message is represented as an  
    /// instance of type T, which is deserialized from the Avro-encoded payload of the message using the  
    /// specified schema.  
    /// </returns>  
    public static Source<PulsarEvent<TKey, TData>, NotUsed> Create(string pulsarServiceUrl,
        string tenantName,
        string pulsarNamespace,
        string topicName,
        TimeSpan changeCaptureInterval,
        string subscriptionName,
        string schemaUrl,
        bool autoAck = false,
        string consumerName = null)
    {
        return Source.FromGraph(new PulsarSource<TKey, TData>(pulsarServiceUrl,
            EnvironmentExtensions.GetDomainEnvironmentVariable("PULSAR_TOKEN"),
            tenantName, pulsarNamespace, topicName, subscriptionName, schemaUrl, autoAck,
            changeCaptureInterval, consumerName));
    }

    /// <inheritdoc/>
    public override SourceShape<PulsarEvent<TKey, TData>> Shape { get; }

    /// <inheritdoc/>
    protected override Attributes InitialAttributes { get; } = Attributes.CreateName(nameof(PulsarSource<TKey, TData>));

    /// <inheritdoc/>
    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new PulsarSourceLogic(this);

    private Outlet<PulsarEvent<TKey, TData>> Out { get; } = new($"{nameof(PulsarSource<TKey, TData>)}.Out");

    private class PulsarSourceLogic : TimerGraphStageLogic
    {
        private const string TimerKey = nameof(PulsarSource<TKey, TData>);
        private readonly PulsarSource<TKey, TData> pulsarSource;
        private readonly PulsarClient pulsarClient;
        private IConsumer<byte[]> consumer;
        private readonly LocalOnlyDecider decider;
        private Action<Task<Option<PulsarEvent<TKey, TData>>>> eventReceived;
        private (AvroSchema keySchema, AvroSchema valueSchema) schema;

        public PulsarSourceLogic(PulsarSource<TKey, TData> pulsarSource) : base(pulsarSource.Shape)
        {
            this.pulsarSource = pulsarSource;
            this.pulsarClient = new PulsarClientBuilder()
                .ServiceUrl(this.pulsarSource.pulsarServiceUrl.ToString()[..^1])
                .Authentication(AuthenticationFactory.Token(this.pulsarSource.pulsarToken))
                .BuildAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            this.decider = Decider.From(
                exception =>
                {
                    return PulsarClientException.isRetriableError(exception) switch
                    {
                        true => Directive.Restart,
                        _ => Directive.Stop
                    };
                });

            SetHandler(this.pulsarSource.Out, PullChanges, Finish);
        }

        private void Finish(Exception ex)
        {
            this.consumer.UnsubscribeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            this.consumer.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            this.pulsarClient.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if (ex != null && ex is not SubscriptionWithCancelException.NonFailureCancellation)
            {
                FailStage(ex);
            }
        }

        public override void PreStart()
        {
            base.PreStart();

            this.eventReceived = GetAsyncCallback<Task<Option<PulsarEvent<TKey, TData>>>>(OnEventReceived);

            var maybeSchema = this.GetSchema().ConfigureAwait(false).GetAwaiter().GetResult();

            if (maybeSchema.IsEmpty)
            {
                FailStage(new ApplicationException($"Cannot locate the schema given then url {this.pulsarSource.schemaUrl}"));
            }
            else
            {
                this.schema = maybeSchema.Value;
                this.consumer = this.pulsarClient
                    .NewConsumer()
                    .ConsumerName(this.pulsarSource.consumerName)
                    .SubscriptionName(this.pulsarSource.subscriptionName)
                    .Topic(this.pulsarSource.topic)
                    .SubscriptionInitialPosition(SubscriptionInitialPosition.Earliest)
                    .SubscribeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private void OnEventReceived(Task<Option<PulsarEvent<TKey, TData>>> eventTask)
        {
            if (eventTask.IsFaulted || eventTask.IsCanceled)
            {
                switch (this.decider.Decide(eventTask.Exception))
                {
                    case Directive.Escalate:
                    case Directive.Stop:
                        Finish(eventTask.Exception);
                        break;
                    case Directive.Resume:
                    case Directive.Restart:
                    default:
                        ScheduleOnce(TimerKey, TimeSpan.FromSeconds(1));
                        break;
                }

                return;
            }

            // Current batch has ended, start a new one
            if (eventTask.Result.IsEmpty)
            {
                ScheduleOnce(TimerKey, this.pulsarSource.changeCaptureInterval);
            }
            else
            {
                Emit(this.pulsarSource.Out, eventTask.Result.Value);
            }
        }

        public Task Ack(MessageId messageId)
        {
            if (this.pulsarSource.autoAck)
            {
                throw new InvalidOperationException("Source has automatic acknowledge enabled");
            }

            return this.consumer.AcknowledgeAsync(messageId);
        }

        private void PullChanges() => (this.consumer.HasReachedEndOfTopic ? Task.FromResult(Option<PulsarEvent<TKey, TData>>.None) : this.consumer.ReceiveAsync()
            .TryMap(
                msg => Option<PulsarEvent<TKey, TData>>.Create(PulsarEvent<TKey, TData>.FromPulsarMessage(msg, schema.keySchema, schema.valueSchema)),
                exception =>
            {
                this.Log.Error(exception, "Failed to receive a message from the configured topic");
                return Option<PulsarEvent<TKey, TData>>.None;
            })
            .TryMap(processed =>
            {
                if (processed.HasValue && processed.Value.MessageId != null && this.pulsarSource.autoAck)
                {
                    return this.consumer.AcknowledgeAsync(processed.Value.MessageId).Map(_ => processed);
                }
                return Task.FromResult(processed);
            }, exception =>
            {
                this.Log.Error(exception, "Failed to parse the message from the configured topic");
                return Task.FromResult(Option<PulsarEvent<TKey, TData>>.None);
            })
            .Flatten())
            .ContinueWith(eventReceived);


        protected override void OnTimer(object timerKey) => PullChanges();

        /// <summary>  
        /// Asynchronously retrieves the Avro schemas for the key and value of this object.  
        /// </summary>  
        /// <returns>  
        /// A task that represents the asynchronous operation. The task result is an option that may contain  
        /// a tuple with the Avro schema for the key and the Avro schema for the value, or None if the schemas  
        /// could not be retrieved.  
        /// </returns>
        private Task<Option<(AvroSchema keySchema, AvroSchema valueSchema)>> GetSchema()
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var hrm = new HttpRequestMessage(HttpMethod.Get, this.pulsarSource.schemaUrl);
            hrm.Headers.Add("Authorization", $"Bearer {this.pulsarSource.pulsarToken}");

            return httpClient.SendAsync(hrm, CancellationToken.None)
                .Map(response =>
                {
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync();
                })
                .FlatMap(content =>
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(content).GetProperty("data").GetString() ?? throw new Exception("Could not find a data property in json response");

                    // The contents of the data element in the response is a string, so we deserialize it as an object
                    var dataElement = JsonSerializer.Deserialize<JsonElement>(data);
                    var keySchema = dataElement.GetProperty("key");
                    var valueSchema = dataElement.GetProperty("value");
                    return (AvroSchema.Parse(keySchema.GetRawText()), AvroSchema.Parse(valueSchema.GetRawText()));
                }).TryMap(result => result, exception =>
                {
                    this.Log.Error(exception, "Failed to decode the schema");
                    return Option<(AvroSchema keySchema, AvroSchema valueSchema)>.None;
                });
        }
    }
}
