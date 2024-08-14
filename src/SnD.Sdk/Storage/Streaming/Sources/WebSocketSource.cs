using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using Akka.Util;
using SnD.Sdk.Extensions.Environment.Hosting;
using Snd.Sdk.Tasks;

namespace Snd.Sdk.Storage.Streaming.Sources;

/// <summary>
/// Akka.Streams source for receiving messages from Websocket connections.
/// Usage example for Pulsar:
/// var ws = WebSocketSource{JsonElement, PulsarWebsocketMessage}.Create("wss://my-websocket-url", TimeSpan.FromSeconds(15), bytes => JsonSerializer.Deserialize{PulsarWebsocketMessage}(Encoding.UTF8.GetString(bytes)), wsm => JsonSerializer.Deserialize{JsonElement}(AvroExtensions.AvroToJson(Convert.FromBase64String(wsm.Payload), schemaJson, true)));
/// </summary>
/// <typeparam name="TElement"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class WebSocketSource<TElement, TMessage> : GraphStage<SourceShape<TElement>>
{
    private readonly Uri wssUrl;
    private readonly string authToken;
    private readonly string authHeader;
    private readonly TimeSpan changeCaptureInterval;
    private readonly Func<byte[], TMessage> messageDecoder;
    private readonly Func<TMessage, TElement> messageConverter;

    private WebSocketSource(
        string wssUrl,
        string authToken,
        string authHeader,
        TimeSpan changeCaptureInterval,
        Func<byte[], TMessage> messageDecoder,
        Func<TMessage, TElement> messageConverter)
    {
        this.wssUrl = new Uri(wssUrl);
        this.authToken = authToken;
        this.authHeader = authHeader;
        this.changeCaptureInterval = changeCaptureInterval;
        this.messageDecoder = messageDecoder;
        this.messageConverter = messageConverter;

        Shape = new SourceShape<TElement>(Out);
    }


    /// <summary>
    /// Creates an Akka Source from a complete WebSocketSource graph.
    /// </summary>
    /// <param name="wssUrl">The WebSocket URL.</param>
    /// <param name="changeCaptureInterval">The interval at which to capture changes.</param>
    /// <param name="messageDecoder">A function that decodes a byte array into a TMessage.</param>
    /// <param name="messageConverter">A function that converts a TMessage into a TElement.</param>
    /// <returns>The Akka Source.</returns>
    public static Source<TElement, NotUsed> Create(
        string wssUrl,
        TimeSpan changeCaptureInterval,
        Func<byte[], TMessage> messageDecoder,
        Func<TMessage, TElement> messageConverter)
    {
        return Source.FromGraph(new WebSocketSource<TElement, TMessage>(wssUrl,
            EnvironmentExtensions.GetDomainEnvironmentVariable("WSS_TOKEN"),
            EnvironmentExtensions.GetDomainEnvironmentVariable("WSS_HEADER"),
            changeCaptureInterval,
            messageDecoder,
            messageConverter));
    }

    /// <inheritdoc/>
    public override SourceShape<TElement> Shape { get; }

    /// <inheritdoc/>
    protected override Attributes InitialAttributes { get; } = Attributes.CreateName(nameof(WebSocketSource<TElement, TMessage>));

    /// <inheritdoc/>
    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new WebSocketSourceLogic(this);

    private Outlet<TElement> Out { get; } = new($"{nameof(WebSocketSource<TElement, TMessage>)}.Out");

    private class WebSocketSourceLogic : TimerGraphStageLogic
    {
        private const string TimerKey = nameof(WebSocketSource<TElement, TMessage>);
        private readonly WebSocketSource<TElement, TMessage> webSocketSource;
        private ClientWebSocket webSocket;
        private List<byte> extendableBuffer;
        private ArraySegment<byte> messageBuffer;
        private readonly LocalOnlyDecider decider;
        private Action<Task<Option<TMessage>>> eventReceived;

        public WebSocketSourceLogic(WebSocketSource<TElement, TMessage> webSocketSource) : base(webSocketSource.Shape)
        {
            this.webSocketSource = webSocketSource;
            this.messageBuffer = new ArraySegment<byte>(new byte[4096]);
            this.extendableBuffer = new List<byte>();
            this.decider = Decider.From(
                exception =>
                {
                    return exception switch
                    {
                        WebSocketException => Directive.Restart,
                        _ => Directive.Stop
                    };
                });

            SetHandler(this.webSocketSource.Out, PullChanges, Finish);
        }

        private void Finish(Exception ex)
        {
            this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Terminated subscription", CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            if (ex != null && ex is not SubscriptionWithCancelException.NonFailureCancellation)
            {
                FailStage(ex);
            }
        }

        public override void PreStart()
        {
            base.PreStart();

            this.eventReceived = GetAsyncCallback<Task<Option<TMessage>>>(OnEventReceived);
            this.webSocket = new ClientWebSocket();
            this.webSocket.Options.SetRequestHeader("Authorization", this.webSocketSource.authToken);

            this.webSocket.ConnectAsync(this.webSocketSource.wssUrl, CancellationToken.None).ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }

        private void OnEventReceived(Task<Option<TMessage>> eventTask)
        {
            if (eventTask.IsFaulted || eventTask.IsCanceled)
            {
                switch (this.decider.Decide(eventTask.Exception))
                {
                    case Directive.Stop:
                        Finish(eventTask.Exception);
                        break;
                    default:
                        ScheduleOnce(TimerKey, TimeSpan.FromSeconds(1));
                        break;
                }

                return;
            }

            // Current batch has ended, start a new one
            if (eventTask.Result.IsEmpty)
            {
                ScheduleOnce(TimerKey, this.webSocketSource.changeCaptureInterval);
            }
            else
            {
                this.extendableBuffer = new List<byte>();
                Emit(this.webSocketSource.Out, this.webSocketSource.messageConverter(eventTask.Result.Value));
            }
        }

        private void PullChanges() => this.webSocket.ReceiveAsync(this.messageBuffer, CancellationToken.None)
            .TryMap(msg =>
            {
                this.extendableBuffer.AddRange(this.messageBuffer.ToArray());
                this.messageBuffer = new ArraySegment<byte>(new byte[4096]);

                if (msg.Count > 0)
                {
                    return this.webSocketSource.messageDecoder(this.extendableBuffer.ToArray()[0..msg.Count]);
                }

                return Option<TMessage>.None;
            }, exception =>
            {
                this.Log.Error(exception, "Failed to receive a message from the configured topic");
                return Option<TMessage>.None;
            })
            .ContinueWith(eventReceived);


        protected override void OnTimer(object timerKey) => PullChanges();
    }
}
