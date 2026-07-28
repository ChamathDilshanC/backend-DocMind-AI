using System.Text;
using DocumentAssistant.Application.Common.Interfaces;
using SharpToken;

namespace DocumentAssistant.SemanticKernel;

/// <summary>Renders the spec's exact RAG prompt template, packing as many top-ranked source chunks as fit a token budget.</summary>
public class PromptBuilder : IPromptBuilder
{
    private const int ContextTokenBudget = 6000;

    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    private const string TemplatePrefix =
        "You are an AI assistant.\n" +
        "Use only the provided context.\n" +
        "If the answer is unavailable, say:\n" +
        "\"I couldn't find that information in the uploaded documents.\"\n\n" +
        "Context\n";

    public string BuildSystemPrompt(IReadOnlyList<SourceChunk> sources)
    {
        var builder = new StringBuilder(TemplatePrefix);
        var tokensUsed = Encoding.Encode(TemplatePrefix).Count;

        foreach (var source in sources)
        {
            var block = $"[Source: {source.Filename}, page {source.Page}]\n{source.Text}\n\n";
            var blockTokens = Encoding.Encode(block).Count;

            if (tokensUsed + blockTokens > ContextTokenBudget) break;

            builder.Append(block);
            tokensUsed += blockTokens;
        }

        return builder.ToString();
    }
}
