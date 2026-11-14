namespace PublisherStudio.BusinessObjects;

/// <summary>Verified bytes and file information for one generated publication asset.</summary>
public sealed record PublisherGeneratedMedia(string FileName, string ContentType, byte[] Bytes);

/// <summary>Authenticated reply containing one chunk of a generated LocalGPT asset.</summary>
internal sealed record PublisherMediaReadResult(string Status, string ArtifactId, string FileName,
    string ContentType, long Length, long Offset, byte[]? Data);
