using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;

namespace ProPilot.Services;

public class DocumentService
{
    /// <summary>
    /// Extract text from a PDF or DOCX file.
    /// </summary>
    public string ExtractText(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdfText(filePath),
            ".docx" => ExtractDocxText(filePath),
            _ => $"Unsupported file type: {ext}"
        };
    }

    private static string ExtractPdfText(string filePath)
    {
        try
        {
            using var document = PdfDocument.Open(filePath);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading PDF: {ex.Message}";
        }
    }

    private static string ExtractDocxText(string filePath)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var para in body.Elements<Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading DOCX: {ex.Message}";
        }
    }

    /// <summary>
    /// Apply text replacements to a DOCX file.
    /// </summary>
    public bool ApplyDocxEdits(string filePath, List<(string original, string replacement)> changes)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return false;

            foreach (var (original, replacement) in changes)
            {
                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText;
                    if (text.Contains(original))
                    {
                        // Clear existing runs and set new text
                        var runs = para.Elements<Run>().ToList();
                        var fullText = string.Concat(runs.Select(r => r.InnerText));
                        var newText = fullText.Replace(original, replacement);

                        // Remove all runs
                        foreach (var run in runs)
                            run.Remove();

                        // Add new run with replaced text
                        var newRun = new Run(new Text(newText) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
                        para.AppendChild(newRun);
                    }
                }
            }

            doc.MainDocumentPart!.Document.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
