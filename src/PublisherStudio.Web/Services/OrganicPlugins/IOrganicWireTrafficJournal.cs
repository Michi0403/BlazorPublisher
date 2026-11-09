using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Journals every 1-Wire packet PublisherStudio sends or receives: one Information log line with the packet metadata, the
/// decrypted payload at Debug, the raw wire frame at Trace, and a bounded in-memory journal for the live traffic view.
/// Journaling never interrupts a transport; its own failures are only logged.
/// </summary>
public interface IOrganicWireTrafficJournal
{
    /// <summary>Raised after a packet was journaled or the journal was cleared.</summary>
    event Action? Changed;

    /// <summary>Gets the traffic options currently in effect.</summary>
    /// <value>The live <c>OrganicPlugins:Traffic</c> options.</value>
    OrganicWireTrafficOptions CurrentOptions { get; }

    /// <summary>Captures the plaintext view of an outgoing envelope before it is protected, when payload capture is on.</summary>
    /// <param name="envelope">The envelope about to be protected and sent.</param>
    /// <returns>The plaintext JSON, or empty when payloads are not captured.</returns>
    string CapturePlaintext(OrganicWireEnvelope envelope);

    /// <summary>Journals one packet: logs it and adds it to the live view.</summary>
    /// <param name="record">The packet as the transport saw it.</param>
    void Record(OrganicWireTrafficRecord record);

    /// <summary>Returns the journaled packets matching a filter, newest first.</summary>
    /// <param name="query">The filter.</param>
    /// <returns>The matching packets.</returns>
    IReadOnlyList<OrganicWireTrafficEntry> Query(OrganicWireTrafficQuery query);

    /// <summary>Returns the total number of packets journaled since PublisherStudio started.</summary>
    /// <returns>The total count, including packets no longer in the bounded view.</returns>
    long TotalRecorded();

    /// <summary>Removes every packet from the live view; log files are not affected.</summary>
    void Clear();
}
