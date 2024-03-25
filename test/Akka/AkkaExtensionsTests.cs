using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams.Dsl;
using Snd.Sdk.ActorProviders;
using Xunit;

namespace Snd.Sdk.Tests.Akka;

public class AkkaExtensionsTests : IClassFixture<AkkaFixture>
{
    private readonly AkkaFixture akkaFixture;

    public AkkaExtensionsTests(AkkaFixture akkaFixture)
    {
        this.akkaFixture = akkaFixture;
    }

    [Theory]
    [InlineData("hello world,\ni am a working test", 1024, "hello world\ni am a working test")]
    [InlineData("hello world,\ni am a working test", 1, "")]
    [InlineData("hello world,\ni am a working test", 25, "hello wor")]
    [InlineData("hello world,\ni am a working test", 32, "hello world")]
    public async Task Compress(string input, long sizeLimit, string expectation)
    {
        var byteStrings = input.Split(",").Select(ByteString.FromString).ToList();
        var resultTask = Source.From(byteStrings).Via(ActorProviderExtensions.Compress(sizeLimit))
            .RunWith(Sink.First<byte[]>(), this.akkaFixture.Materializer);

        if (sizeLimit < 17)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await resultTask);
        }
        else
        {
            Assert.Equal(expectation, (await resultTask).Decompress().ToString());
        }
    }
}
