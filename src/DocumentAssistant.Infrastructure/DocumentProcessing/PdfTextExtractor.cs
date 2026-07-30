using System.Diagnostics;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public class PdfTextExtractor(ILogger<PdfTextExtractor> logger) : IDocumentTextExtractor
{
    public DocumentFileType SupportedFileType => DocumentFileType.Pdf;

    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(fileStream);

        var pages = new List<ExtractedPage>();
        foreach (var page in document.GetPages())
        {
            var text = ExtractPageText(page);

            // Scanned pages have no text layer at all — every glyph is a pixel, not a
            // character — so word-based extraction finds nothing even though the page
            // clearly has content. Fall back to OCR-ing the page's embedded image(s).
            if (string.IsNullOrWhiteSpace(text))
            {
                text = await OcrPageAsync(page, cancellationToken);
            }

            var cleaned = TextCleaner.Clean(text);
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                pages.Add(new ExtractedPage(page.Number, cleaned));
            }
        }

        return pages;
    }

    // Page.Text concatenates glyph runs in content-stream order with no reliable word
    // spacing, so labels from diagrams, tables, and bullet lists (positioned via layout
    // rather than a literal space glyph) get glued into unreadable walls of run-on text.
    // GetWords() segments glyphs into words using gap-width heuristics, so joining those
    // with single spaces reconstructs real word boundaries instead.
    private static string ExtractPageText(Page page) => string.Join(' ', page.GetWords().Select(w => w.Text));

    private async Task<string> OcrPageAsync(Page page, CancellationToken cancellationToken)
    {
        var texts = new List<string>();

        foreach (var image in page.GetImages())
        {
            var bytes = GetImageBytes(image);
            if (bytes is null) continue;

            var text = await RunTesseractAsync(bytes, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                texts.Add(text);
            }
        }

        return string.Join('\n', texts);
    }

    // TryGetPng() re-encodes whatever the PDF's internal filter chain produced, but it
    // doesn't handle every encoding this "custom" PdfPig build encounters (observed on a
    // real scanned document: it returned false for a plain DCTDecode/JPEG image). RawBytes
    // for a DCTDecode-filtered image *is* already a complete, standalone JPEG file, so fall
    // back to using it directly when it's recognizable as one.
    private static byte[]? GetImageBytes(IPdfImage image)
    {
        if (image.TryGetPng(out var png)) return png;

        var raw = image.RawBytes.ToArray();
        return LooksLikeImage(raw) ? raw : null;
    }

    private static bool LooksLikeImage(byte[] bytes) =>
        bytes.Length > 4 &&
        ((bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) // JPEG
         || (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)); // PNG

    // Shells out to the tesseract CLI rather than a Tesseract .NET wrapper: the
    // charlesw/Tesseract package's native loader expects libtesseract50/libleptonica
    // filenames that don't match what `apt install tesseract-ocr` actually provides on
    // Debian, and separately P/Invokes libdl, which no longer exists as its own shared
    // object on modern glibc (folded into libc). The CLI has none of that baggage —
    // verified end-to-end against a real scanned document in the exact deployment image.
    private async Task<string> RunTesseractAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        var tempBase = Path.Combine(Path.GetTempPath(), "ocr-" + Guid.NewGuid().ToString("N"));
        var imagePath = tempBase + ".img";
        var outputBase = tempBase + "-out";
        var outputPath = outputBase + ".txt";

        try
        {
            await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                ArgumentList = { imagePath, outputBase, "-l", "eng" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            if (process is null) return string.Empty;

            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                logger.LogWarning("Tesseract OCR failed (exit {ExitCode}): {Error}", process.ExitCode, await stdErrTask);
                return string.Empty;
            }

            await stdOutTask;
            return await File.ReadAllTextAsync(outputPath, cancellationToken);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            logger.LogWarning(ex, "Tesseract OCR is unavailable; scanned page will have no extracted text.");
            return string.Empty;
        }
        finally
        {
            if (File.Exists(imagePath)) File.Delete(imagePath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
