using System;
using System.IO;
using System.IO.Compression;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.IO;
using Akka.Logger.Serilog;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using Microsoft.Extensions.DependencyInjection;
using Snd.Sdk.Hosting;

namespace Snd.Sdk.ActorProviders
{
    /// <summary>
    /// Extension methods from Akka.Streams.
    /// </summary>
    public static class ActorProviderExtensions
    {
        /// <summary>
        /// Filters out None values from the flow.
        /// </summary>
        /// <param name="flow">Source flow.</param>
        /// <typeparam name="TOut">Element type that <see cref="Option{T}"/> wraps.</typeparam>
        /// <typeparam name="TMat">Materialisation type for this flow.</typeparam>
        /// <returns></returns>
        public static Source<TOut, TMat> CollectOption<TOut, TMat>(this Source<Option<TOut>, TMat> flow)
        {
            return flow.Collect(v => v.HasValue, v => v.Value);
        }

        /// <summary>
        /// Filters out None values from the flow.
        /// </summary>
        /// <param name="flow">Source flow.</param>
        /// <typeparam name="TIn">Flow input type.</typeparam> 
        /// <typeparam name="TOut">Element type that <see cref="Option{T}"/> wraps.</typeparam>
        /// <typeparam name="TMat">Materialisation type for this flow.</typeparam>
        /// <returns></returns>        
        public static Flow<TIn, TOut, TMat> CollectOption<TIn, TOut, TMat>(this Flow<TIn, Option<TOut>, TMat> flow)
        {
            return flow.Collect(v => v.HasValue, v => v.Value);
        }

        /// <summary>
        /// Compresses a ByteString using gzip algorithm.
        /// </summary>
        /// <param name="byteString">ByteString to compress.</param>
        /// <returns></returns>
        public static byte[] Compress(this ByteString byteString)
        {
            var rawBytes = byteString.Count;
            using var stream = new MemoryStream();
            using var compressor = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: false);
            compressor.Write(byteString.ToArray(), 0, rawBytes);
            compressor.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// Decompresses a byte stream using gzip into a ByteString.
        /// </summary>
        /// <param name="bytes">Byte array to decompress.</param>
        /// <returns></returns>
        public static ByteString Decompress(this byte[] bytes)
        {
            using var compressedStream = new MemoryStream(bytes);
            using var decompressedStream = new MemoryStream();
            using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
            gzip.CopyTo(decompressedStream);
            return ByteString.FromBytes(decompressedStream.ToArray());
        }

        /// <summary>
        /// Compresses all incoming ByteStrings until the compressed output size reaches a certain limit.
        /// </summary>
        /// <param name="limitSizeBytes">Desired limit in bytes.</param>
        /// <returns></returns>
        public static Flow<ByteString, byte[], NotUsed> Compress(long limitSizeBytes)
        {
            return Flow.Create<ByteString>()
                .Aggregate((aggregated: ByteString.Empty, canContinue: true), (agg, e) =>
                {
                    if (limitSizeBytes <= 17)
                    {
                        throw new ArgumentOutOfRangeException("Size limit cannot be lower than the size of a compressed array representing a single character", innerException: null);
                    }

                    if (!agg.canContinue)
                    {
                        return agg;
                    }

                    // handle case when nothing has been aggregated yet and input already exceeds compression size limit
                    if (agg.aggregated.IsEmpty && e.Compress().Length > limitSizeBytes)
                    {
                        while (e.Count > 0 && e.Compress().Length > limitSizeBytes)
                        {
                            e = e[..^1];
                        }

                        return (e, false);
                    }

                    var newString = agg.aggregated.Concat(e);
                    return newString.Compress().Length < limitSizeBytes ? (newString, true) : (agg.aggregated, false);
                })
                .Select(raw => raw.aggregated.Compress());
        }

        /// <summary>
        /// Configures the application to the local ActorSystem singleton.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="configureAction">Optional configuration Action for ActorSystem</param>
        /// <returns></returns>
        public static IServiceCollection AddLocalActorSystem(this IServiceCollection services, Action<AkkaConfigurationBuilder> configureAction = null)
         => services.AddAkka(nameof(AppDomain.CurrentDomain.FriendlyName), builder =>
         {
             ConfigureActorLogging(builder);
             configureAction?.Invoke(builder);
         }).AddSingleton<IMaterializer>(provider => provider.GetRequiredService<ActorSystem>().Materializer());

        private static void ConfigureActorLogging(AkkaConfigurationBuilder builder) =>
            builder.ConfigureLoggers(loggerBuilder =>
            {
                loggerBuilder.LogLevel = EnvironmentExtensions.GetDomainEnvironmentVariable("DEFAULT_LOG_LEVEL") switch
                {
                    "INFO" => LogLevel.InfoLevel,
                    "WARN" => LogLevel.WarningLevel,
                    "ERROR" => LogLevel.ErrorLevel,
                    "DEBUG" => LogLevel.DebugLevel,
                    _ => LogLevel.InfoLevel
                };
                loggerBuilder.LogConfigOnStart = true;
                loggerBuilder.AddLogger<SerilogLogger>();
                loggerBuilder.DebugOptions = new DebugOptions
                {
                    Unhandled = true,
                    EventStream = true,
                    LifeCycle = true,
                    AutoReceive = true,
                    Receive = true
                };
                loggerBuilder.LogMessageFormatter = typeof(SerilogLogMessageFormatter);
            });
    }
}
