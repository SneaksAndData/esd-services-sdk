using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Snd.Sdk.Tasks;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Snd.Sdk.Tests.Tasks
{
    public class TaskExtensionsTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public TaskExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Map()
        {
            Task<int> t = new(() => 1);
            t.Start();

            var mappedTaskResult = await t.Map(r => r + 1);

            Assert.Equal(2, mappedTaskResult);
        }

        [Fact]
        public async Task MapUntyped()
        {
            Task t = new(() => { testOutputHelper.WriteLine("Hello from void task!"); });
            t.Start();

            var mappedTaskResult = await t.Map(_ => 1);

            Assert.Equal(1, mappedTaskResult);
        }

        [Fact]
        public async Task FlatMapNested()
        {
            Task<Task<int>> t = new(() => Task.FromResult(1));
            t.Start();

            var mappedTaskResult = await t.FlatMap(r => r + 1);

            Assert.Equal(2, mappedTaskResult);
        }

        [Fact]
        public async Task FlatMap()
        {
            Task<int> t = new(() => 1);
            t.Start();

            var mappedTaskResult = await t.FlatMap(r => Task.FromResult(r + 1));

            Assert.Equal(2, mappedTaskResult);
        }

        [Fact]
        public async Task Flatten()
        {
            Task<Task<int>> t = new(() => Task.FromResult(1));
            t.Start();

            var mappedTaskResult = await t.Flatten();

            Assert.Equal(1, mappedTaskResult);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("test")]
        public async Task TryMap(string exceptionMessage)
        {
            var testTask = new Func<string, string>(_ => throw new Exception(exceptionMessage));
            var testRun = Task.Run(() => testTask("hello"));
            var result = !string.IsNullOrEmpty(exceptionMessage)
                ? await testRun.TryMap(res => "success", ex => ex.Message)
                : await testRun.TryMap(res => "success");
            Assert.Equal(exceptionMessage, result);
        }

        [Theory]
        [InlineData(1, "A task was canceled.")]
        [InlineData(5, "test")]
        public async Task TryMapWithCts(int timeoutSeconds, string message)
        {
            var testTask = new Func<string, string>(_ => throw new Exception("test"));

            Task<string> ResultTask()
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                Thread.Sleep(2000);
                return Task.Run(() => testTask("hello"), cts.Token).TryMap(res => "success", ex => ex.Message);
            }

            var result = await ResultTask();
            Assert.Equal(message, result);
        }
    }
}
