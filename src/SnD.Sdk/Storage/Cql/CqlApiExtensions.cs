using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimit;
using Polly.Retry;
using Snd.Sdk.Tasks;

namespace Snd.Sdk.Storage.Cql;

/// <summary>
/// Provides extension methods for CQL API.
/// </summary>
public static class CqlApiExtensions
{
    /// <summary>
    /// Executes a CQL API call with retry and rate limit policies.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the CQL API call.</typeparam>
    /// <typeparam name="TCaller">The type of the caller for logging purposes.</typeparam>
    /// <param name="cqlApiCall">The CQL API call to be executed.</param>
    /// <param name="logger">The logger to log retry and rate limit information.</param>
    /// <param name="rateLimit">The rate limit (number of requests) per specified period.</param>
    /// <param name="rateLimitPeriod">The time period for the rate limit.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, which produces the result of the CQL API call.</returns>
    public static Task<TResult> ExecuteWithRetryAndRateLimit<TResult, TCaller>(
        this Func<CancellationToken, Task<TResult>> cqlApiCall,
        ILogger<TCaller> logger,
        int rateLimit, TimeSpan rateLimitPeriod,
        CancellationToken cancellationToken = default
    )
    {
        var wrapPolicy = CreateRetryPolicy(logger).WrapAsync(Policy.RateLimitAsync(rateLimit, rateLimitPeriod));
        var wrappedTask = cqlApiCall.WithWrapPolicy(wrapPolicy, cancellationToken);

        return wrappedTask;
    }

    private static AsyncRetryPolicy CreateRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<RateLimitRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: (retryAttempt, exception, context) =>
                {
                    // Respect the retry after time provided by the rate limiter
                    if (exception is RateLimitRejectedException rateLimitException)
                    {
                        logger.LogWarning("Rate limit hit. Retrying after {RetryAfter} milliseconds",
                            rateLimitException.RetryAfter.TotalMilliseconds);
                        return rateLimitException.RetryAfter;
                    }

                    // Exponential backoff for other exceptions
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetryAsync: (exception, timeSpan, retryCount, _) =>
                {
                    logger.LogWarning(exception,
                        "Retrying batch after {SleepDuration}. Retry attempt {RetryCount}",
                        timeSpan, retryCount);
                    return Task.CompletedTask;
                });
    }
}
