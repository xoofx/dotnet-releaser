using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetReleaser.Runners;

/// <summary>
/// Runs work that starts one child process per item, with a limit on how many run at once.
/// </summary>
/// <remarks>
/// One `dotnet msbuild` per project of a solution, all started together, takes far more processors than a
/// small CI machine has. It also starves the thread pool: reading the redirected output of a child process
/// can block two pool threads for as long as that process runs, while the pool only starts with one thread
/// per processor and grows slowly on a busy machine.
/// </remarks>
internal static class ChildProcessConcurrency
{
    /// <summary>
    /// The pool threads that reading the standard output and the standard error of one child process can block.
    /// </summary>
    private const int BlockedThreadsPerProcess = 2;

    /// <summary>
    /// Gets how many child processes run at once.
    /// </summary>
    public static int MaxParallelProcesses => Environment.ProcessorCount;

    /// <summary>
    /// Runs <paramref name="action"/> for every item, for at most <see cref="MaxParallelProcesses"/> items at once.
    /// </summary>
    public static Task ForEachAsync<T>(IEnumerable<T> items, Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(action);

        EnsureThreadPoolCapacity(MaxParallelProcesses);
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelProcesses };
        return Parallel.ForEachAsync(items, options, async (item, _) => await action(item).ConfigureAwait(false));
    }

    /// <summary>
    /// Makes sure the thread pool keeps threads free while <paramref name="processes"/> child processes run.
    /// </summary>
    public static void EnsureThreadPoolCapacity(int processes)
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
        var required = Environment.ProcessorCount + processes * BlockedThreadsPerProcess;
        if (workerThreads < required)
        {
            ThreadPool.SetMinThreads(required, completionPortThreads);
        }
    }
}
