using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser.Tests;

public class ChildProcessConcurrencyTests
{
    [Test]
    public async Task ForEachAsyncRunsEveryItemWithoutExceedingTheLimit()
    {
        var itemCount = ChildProcessConcurrency.MaxParallelProcesses * 4;
        var running = 0;
        var maxRunning = 0;
        var done = 0;

        await ChildProcessConcurrency.ForEachAsync(Enumerable.Range(0, itemCount), async _ =>
        {
            var current = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, current);
            await Task.Delay(20);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref done);
        });

        Assert.AreEqual(itemCount, done);
        Assert.IsTrue(maxRunning <= ChildProcessConcurrency.MaxParallelProcesses, $"{maxRunning} items ran at once.");
    }

    [Test]
    public void EnsureThreadPoolCapacityKeepsThreadsFreeBesideTheBlockedOnes()
    {
        ThreadPool.GetMinThreads(out var initialWorkerThreads, out var completionPortThreads);
        try
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount, completionPortThreads);

            ChildProcessConcurrency.EnsureThreadPoolCapacity(processes: 5);

            ThreadPool.GetMinThreads(out var workerThreads, out _);
            Assert.AreEqual(Environment.ProcessorCount + 10, workerThreads);

            // A smaller need does not lower it again.
            ChildProcessConcurrency.EnsureThreadPoolCapacity(processes: 1);
            ThreadPool.GetMinThreads(out workerThreads, out _);
            Assert.AreEqual(Environment.ProcessorCount + 10, workerThreads);
        }
        finally
        {
            ThreadPool.SetMinThreads(initialWorkerThreads, completionPortThreads);
        }
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        while ((current = Volatile.Read(ref location)) < value && Interlocked.CompareExchange(ref location, value, current) != current)
        {
        }
    }
}
