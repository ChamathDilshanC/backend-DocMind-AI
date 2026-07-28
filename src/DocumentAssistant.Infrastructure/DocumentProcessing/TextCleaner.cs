using System.Text.RegularExpressions;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public static partial class TextCleaner
{
    public static string Clean(string text)
    {
        var normalized = WhitespaceRun().Replace(text, " ");
        normalized = MultipleBlankLines().Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex MultipleBlankLines();
}
