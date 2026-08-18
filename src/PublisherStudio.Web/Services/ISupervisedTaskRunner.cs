namespace PublisherStudio.Services;

/// <summary>
/// Starts intentionally non-blocking work while observing completion, cancellation, and failure.
/// Use this instead of discarding a task returned by an asynchronous method.
/// </summary>
public interface ISupervisedTaskRunner
{
    /// <summary>Reports how many explicitly owned background operations are still being observed.</summary>
    /// <value>The number of supervised tasks that have not completed yet.</value>
    int ActiveTaskCount { get; }

    /// <summary>Transfers an intentionally concurrent operation into the supervised application lifetime and observes its completion.</summary>
    /// <param name="owner">Component or service that owns the operation.</param>
    /// <param name="operation">Stable operation name used in diagnostics.</param>
    /// <param name="action">Asynchronous operation to observe.</param>
    /// <param name="cancellationToken">Token that ends work with its owning lifetime.</param>
    void Run(string owner, string operation, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
