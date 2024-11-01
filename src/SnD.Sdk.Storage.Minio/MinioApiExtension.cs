using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace SnD.Sdk.Storage.Minio;

/// <summary>
/// Extension methods for Minio API.
/// </summary>
public static class MinioApiExtension
{
    /// <summary>
    /// Applies a timeout retry policy to the specified asynchronous method.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the asynchronous method.</typeparam>
    /// <typeparam name="TCaller">The type of the caller, used for logging purposes.</typeparam>
    /// <param name="asyncMethod">The asynchronous method to which the retry policy will be applied.</param>
    /// <param name="retryPolicy">The retry policy to apply.</param>
    /// <param name="retryLogger">The logger used to log retry attempts.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, which produces the result of the wrapped asynchronous method.</returns>
    public static async Task<TResult> WithTimeoutRetryPolicy<TResult, TCaller>(this Func<CancellationToken, Task<TResult>> asyncMethod,
        ILogger<TCaller> retryLogger,
        CancellationToken cancellationToken = default)
    {
        var policy = Policy.Handle<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: int.Parse(
                    Environment.GetEnvironmentVariable("PROTEUS__MINIO_TIMEOUT_RETRY_COUNT") ?? "3"),
                sleepDurationProvider: (_, ex) =>
                {
                    var defaultRetry = Environment.GetEnvironmentVariable("PROTEUS__MINIO_TIMEOUT_RETRY_INTERVAL") ??
                                       "3";

                    return TimeSpan.FromSeconds(int.Parse(defaultRetry));
                },
                onRetryAsync: (exception, retryCount, context) =>
                {
                    retryLogger.LogWarning(exception,
                        "API Server responded with Timeout. Will retry in {retryInSeconds} seconds",
                        retryCount.TotalSeconds);
                    return Task.CompletedTask;
                });
        return await policy.ExecuteAsync(asyncMethod, cancellationToken);
    }
}
