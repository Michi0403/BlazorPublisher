namespace PublisherStudio.BusinessObjects;

/// <summary>Supplies a disposable no-op scope for logger implementations that do not persist scope state separately.</summary>
internal sealed class LoggerNullScope : IDisposable
{
    /// <summary>Releases the inert logger scope; no resources are owned by this scope.</summary>
    public void Dispose()
    {
    }
}
