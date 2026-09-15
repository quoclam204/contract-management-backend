using System.Text;
using ContractManagement.Application.AI.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace ContractManagement.Infrastructure.AI;

/// <summary>
/// Extracts plain text from supported contract document formats.
/// </summary>
public class DocumentTextExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx"
    };

    public Task<string> ExtractTextAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (content == null || content.Length == 0)
            throw new ArgumentException("Document content cannot be null or empty.", nameof(content));

        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(fileName);
        if (!SupportedExtensions.Contains(extension))
            throw new NotSupportedException($"Unsupported document format '{extension}'. Supported formats are: .pdf, .docx.");

        var extractedText = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? ExtractPdfText(content, cancellationToken)
            : ExtractDocxText(content, cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedText))
            throw new InvalidOperationException("The document does not contain extractable text.");

        return Task.FromResult(extractedText.Trim());
    }

    private static string ExtractPdfText(byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    builder.AppendLine(page.Text);
                }
            }

            return builder.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to extract text from PDF document.", ex);
        }
    }

    private static string ExtractDocxText(byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var document = WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;

            if (body is null)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    builder.AppendLine(text);
                }
            }

            return builder.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to extract text from DOCX document.", ex);
        }
    }
}
