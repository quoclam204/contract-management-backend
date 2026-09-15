using System.Text;
using ContractManagement.Infrastructure.AI;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.UnitTests.AI;

public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractTextAsync_WithPdfDocument_ReturnsExtractedText()
    {
        // Arrange
        const string expectedText = "Contract summary text";
        var pdfBytes = CreatePdfBytes(expectedText);

        // Act
        var result = await _extractor.ExtractTextAsync("contract.pdf", pdfBytes);

        // Assert
        Assert.Contains(expectedText, result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithDocxDocument_ReturnsExtractedText()
    {
        // Arrange
        const string expectedText = "Service contract value is 1000 USD";
        var docxBytes = CreateDocxBytes(expectedText);

        // Act
        var result = await _extractor.ExtractTextAsync("contract.docx", docxBytes);

        // Assert
        Assert.Contains(expectedText, result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithUnsupportedExtension_ThrowsNotSupportedException()
    {
        // Arrange
        var content = "plain text"u8.ToArray();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => _extractor.ExtractTextAsync("contract.txt", content));
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _extractor.ExtractTextAsync("contract.pdf", Array.Empty<byte>()));
    }

    [Fact]
    public async Task ExtractTextAsync_WithInvalidPdf_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidPdf = "not a real pdf"u8.ToArray();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _extractor.ExtractTextAsync("contract.pdf", invalidPdf));
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyDocx_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyDocx = CreateDocxBytes(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _extractor.ExtractTextAsync("contract.docx", emptyDocx));
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();

            if (!string.IsNullOrEmpty(text))
            {
                body.AppendChild(new Paragraph(new Run(new Text(text))));
            }

            mainPart.Document = new Document(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePdfBytes(string text)
    {
        var streamContent = $"BT\n/F1 24 Tf\n100 700 Td\n({EscapePdfText(text)}) Tj\nET\n";
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}endstream\nendobj\n"
        };

        var builder = new StringBuilder();
        var offsets = new List<int>();

        builder.Append("%PDF-1.4\n");
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 6\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            builder.Append(offset.ToString("D10"));
            builder.Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefOffset);
        builder.Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string EscapePdfText(string value)
    {
        return value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
