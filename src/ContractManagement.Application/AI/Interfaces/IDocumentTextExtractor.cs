namespace ContractManagement.Application.AI.Interfaces;

/// <summary>
/// Extracts readable text from supported contract document formats.
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>
    /// Extracts text from the provided document bytes using the supplied file name or extension.
    /// </summary>
    Task<string> ExtractTextAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);
}
