using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Streaming.Models;
using Snd.Sdk.Storage.Streaming.Sources;
using Snd.Sdk.Storage.Streaming.MessageProtocolExtensions;
using SnD.Sdk.Extensions.Environment.Hosting;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class StreamingTests : IClassFixture<AkkaFixture>
{
    private readonly AkkaFixture akkaFixture;

    public StreamingTests(AkkaFixture akkaFixture)
    {
        this.akkaFixture = akkaFixture;
    }

    [Theory(Skip = "End-to-end test, use when developing locally")]
    [InlineData("wss://...")]
    public async Task WebSocketSource(string wssUrl)
    {
        // remember to set domain-bound env vars
        // PROTEUSTESTS__WSS_TOKEN=...
        // PROTEUSTESTS__WSS_HEADER=Bearer
        // PROTEUSTESTS__WSS_SCHEMA_URL=...
        var http = new HttpClient();
        var hrm = new HttpRequestMessage(HttpMethod.Get, EnvironmentExtensions.GetDomainEnvironmentVariable("WSS_SCHEMA_URL"));
        hrm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", EnvironmentExtensions.GetDomainEnvironmentVariable("WSS_TOKEN"));
        hrm.Headers.Add("Accept", "application/json");
        var resp = JsonSerializer.Deserialize<JsonElement>(
            await (await http.SendAsync(hrm)).Content.ReadAsStringAsync());
        var schemaStr = JsonSerializer.Deserialize<JsonElement>(resp.GetProperty("data").GetString()).GetProperty("value").GetRawText();
        var schemaJson = Avro.Schema.Parse(schemaStr);

        var src = WebSocketSource<JsonElement, PulsarWebsocketMessage>.Create(
            wssUrl,
            TimeSpan.FromSeconds(15),
            bytes => JsonSerializer.Deserialize<PulsarWebsocketMessage>(Encoding.UTF8.GetString(bytes)),
            wsm => JsonSerializer.Deserialize<JsonElement>(AvroExtensions.AvroToJson(Convert.FromBase64String(wsm.Payload), schemaJson, true)));

        var result = await src.Take(10).RunWith(Sink.Seq<JsonElement>(), this.akkaFixture.Materializer);

        Assert.True(result.Count > 0);
    }

    [Theory(Skip = "End-to-end test, use when developing locally")]
    [InlineData("pulsar+ssl://pulsar...streaming.datastax.com:6651", "sometenant", "somenamespace", "some-topic", "some-subscription", "https://...")]
    public async Task PulsarSource(string brokerUrl, string tenantName, string pulsarNamespace, string topic, string subscription, string schemaUrl)
    {
        var src = PulsarSource<JsonElement, JsonElement>.Create(
            brokerUrl,
            tenantName,
            pulsarNamespace,
            topic,
            TimeSpan.FromSeconds(5),
            subscription,
            schemaUrl,
            true);

        var result = await src.Take(3).RunWith(Sink.Seq<PulsarEvent<JsonElement, JsonElement>>(), this.akkaFixture.Materializer);

        Assert.True(result.Count > 0);
    }
}
