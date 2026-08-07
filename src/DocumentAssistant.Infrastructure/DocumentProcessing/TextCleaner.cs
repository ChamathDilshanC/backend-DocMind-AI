using System.Text.RegularExpressions;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public static partial class TextCleaner
{
    public static string Clean(string text)
    {
        // PDFs with broken font encodings (and OCR output) can contain NUL bytes, other
        // control characters, or lone surrogates. Postgres rejects those on insert with
        // 22021 "invalid byte sequence for encoding UTF8", so strip them up front.
        var sanitized = InvalidChars().Replace(text, string.Empty);
        var normalized = WhitespaceRun().Replace(sanitized, " ");
        normalized = MultipleBlankLines().Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    // Control characters except \t \n \r, plus lone surrogates and Unicode non-characters.
    [GeneratedRegex(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F-\u009F\uD800-\uDFFF\uFFFE\uFFFF]")]
    private static partial Regex InvalidChars();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex MultipleBlankLines();
}
