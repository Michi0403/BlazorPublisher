using System.Collections.Concurrent;

namespace PublisherStudio.Services;

/// <summary>
/// Observes intentionally concurrent PublisherStudio work so task failures never become unobserved.
/// Every operation retains an explicit component or service owner and operation name.
/// </summary>
/// <param name="logger">Logger used to record supervised task lifecycle and failures.</param>
public sealed class SupervisedTaskRunner(ILogger<SupervisedTaskRunner> logger) : ISupervisedTaskRunner
{
    /// <summary>Tracks every intentionally concurrent operation until its continuation removes it after completion.</summary>
    private readonly ConcurrentDictionary<long, Task> activeTasks = new();

    /// <summary>Monotonically increasing sequence used to correlate supervised operations in diagnostics.</summary>
    private long nextTaskId;

    /// <summary>Reports how many explicitly owned background operations are still being observed.</summary>
    /// <value>The number of supervised tasks that have not completed yet.</value>
    public int ActiveTaskCount => activeTasks.Count;

    /// <summary>Transfers an intentionally concurrent operation into the supervised application lifetime and observes its completion.</summary>
    /// <param name="owner">Component or service that owns the operation.</param>
    /// <param name="operation">Stable operation name used in diagnostics.</param>
    /// <param name="action">Asynchronous operation to observe.</param>
    /// <param name="cancellationToken">Token that ends work with its owning lifetime.</param>
    public void Run(string owner, string operation, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(action);

            var taskId = Interlocked.Increment(ref nextTaskId);
            var task = Task.Run(
                () => ObserveAsync(taskId, owner, operation, action, cancellationToken),
                CancellationToken.None);
            if (!activeTasks.TryAdd(taskId, task))
                throw new InvalidOperationException($"Could not track supervised task {taskId}.");

            task.GetAwaiter().OnCompleted(() =>
            {
                activeTasks.TryRemove(taskId, out var removedTask);
            });
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, $"Supervised task scheduling for {owner}/{operation} was canceled.");
            else
                logger.LogError(exception, $"Supervised task scheduling for {owner}/{operation} failed.");
            throw;
        }
    }

    /// <summary>Observes one scheduled operation until completion, cancellation, or failure.</summary>
    /// <param name="taskId">Internal task identifier used for diagnostics.</param>
    /// <param name="owner">Component or service that owns the operation.</param>
    /// <param name="operation">Stable operation name used in diagnostics.</param>
    /// <param name="action">Asynchronous operation to observe.</param>
    /// <param name="cancellationToken">Token that ends work with its owning lifetime.</param>
    /// <returns>A task that completes after the owned operation has been observed.</returns>
    private async Task ObserveAsync(long taskId, string owner, string operation, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action(cancellationToken).ConfigureAwait(false);
            logger.LogDebug($"Supervised task {taskId} for {owner}/{operation} completed.");
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, $"Supervised task {taskId} for {owner}/{operation} was canceled by its owner.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Supervised task {taskId} for {owner}/{operation} failed.");
        }
    }
}
