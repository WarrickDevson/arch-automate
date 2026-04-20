using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ArchAutomate.AI.Services;

/// <summary>
/// Extracts plain text from PDF council rejection letters using PdfPig.
/// </summary>
public class PdfOcrService
{
    /// <summary>
    /// Extracts all text from the provided PDF bytes, preserving page order.
    /// </summary>
    public string ExtractText(byte[] pdfBytes)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        var sb = new System.Text.StringBuilder();

        foreach (Page page in doc.GetPages())
        {
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Returns per-page text content.
    /// </summary>
    public IReadOnlyList<string> ExtractPageTexts(byte[] pdfBytes)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        return doc.GetPages()
                  .Select(p => p.Text)
                  .ToList()
                  .AsReadOnly();
    }
}
