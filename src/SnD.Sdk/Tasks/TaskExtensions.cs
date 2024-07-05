using System;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Polly.Retry;

namespace Snd.Sdk.Tasks
{
    /// <summary>
    /// Extension methods emulating monad behaviours for System.Threading.Tasks.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Extension to map/derive the result of a task.
        /// </summary>
        /// <typeparam name="TSource">Return type of a source task.</typeparam>
        /// <typeparam name="TResult">Return type of a projection function.</typeparam>
        /// <param name="task">Task to map.</param>
        /// <param name="selector">Projection function.</param>
        /// <returns></returns>
        public static async Task<TResult> Map<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> selector) => selector(await task);

        private static async Task<TResult> AsTyped<TResult>(this Task task, Func<TResult> defaultValueProvider)
        {
            await task;
            return defaultValueProvider();
        }

        /// <summary>
        /// Extension to map the result of a task without return type. This should be used to allow Map/TryMap/FlatMap etc monad semantics on untyped tasks.
        /// </summary>
        /// <typeparam name="TResult">Return type of a projection function.</typeparam>
        /// <param name="task">Task to map.</param>
        /// <param name="selector">Projection function.</param>
        /// <returns></returns>
        public static async Task<TResult> Map<TResult>(this Task task, Func<NotUsed, TResult> selector) => selector(await task.AsTyped(() => NotUsed.Instance));

        /// <summary>
        /// Extension to map/derive the result of a task with error handling.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="selector"></param>
        /// <param name="errorHandler"></param>
        /// <returns></returns>
        public static async Task<TResult> TryMap<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> selector, Func<Exception, TResult> errorHandler = null)
        {
            try
            {
                return selector(await task);
            }
            catch (Exception ex)
            {
                return errorHandler != null ? errorHandler(ex) : default;
            }
        }

        /// <summary>
        /// Extension to map/derive the result of a task with error handling.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="selector"></param>
        /// <param name="errorHandler"></param>
        /// <returns></returns>
        public static async Task<TResult> TryMap<TResult>(this Task task, Func<TResult> selector, Func<Exception, TResult> errorHandler = null)
        {
            try
            {
                await task;
                return selector();
            }
            catch (Exception ex)
            {
                return errorHandler != null ? errorHandler(ex) : default;
            }
        }

        /// <summary>
        /// Executes nested tasks in a sequences and maps the result.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="taskChain"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static async Task<TResult> FlatMap<TSource, TResult>(this Task<Task<TSource>> taskChain, Func<TSource, TResult> selector) => selector(await await taskChain);

        /// <summary>
        /// FlatMap that can be called on a Task, given the select that returns a task. Allows simplification of
        /// taskA.Map(_ => taskB).Flatten() to taskA.FlatMap(_ => taskB) 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static async Task<TResult> FlatMap<TSource, TResult>(this Task<TSource> task, Func<TSource, Task<TResult>> selector) => await selector(await task);

        /// <summary>
        /// Flattens task chain without changing the return type.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="taskChain"></param>
        /// <returns></returns>
        public static async Task<TResult> Flatten<TResult>(this Task<Task<TResult>> taskChain) => await await taskChain;

        /// <summary>
        /// Applies the specified retry policy to the specified task and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result produced by the task.</typeparam>
        /// <param name="wrapped">The task to which to apply the retry policy.</param>
        /// <param name="policy">The retry policy to apply.</param>
        /// <param name="cancellationToken">Optional cancellation token for this policy wrapper.</param>
        /// <returns>A task that represents the asynchronous operation, which produces the result of the wrapped task.</returns>
        public static Task<TResult> WithRetryPolicy<TResult>(this Func<CancellationToken, Task<TResult>> wrapped, AsyncRetryPolicy policy, CancellationToken cancellationToken = default)
        {
            return policy.ExecuteAsync(wrapped, cancellationToken);
        }
    }
}
