using System.Text.RegularExpressions;

namespace PlanMate.Api.Services;

internal static class AdviceTextHelper
{
    private static readonly Regex[] LeadingGreetingPatterns =
    [
        new(@"^안녕하세요[!.]?\s*", RegexOptions.IgnoreCase),
        new(@"^안녕[!.]?\s*", RegexOptions.IgnoreCase),
        new(@"^(?:저는\s+)?(?:플랜|Plan)\s*메이트(?:AI)?(?:입니다|예요)?[!.]?\s*", RegexOptions.IgnoreCase),
        new(@"^도우미입니다[!.]?\s*", RegexOptions.IgnoreCase),
    ];

    public static string NormalizeForCard(string raw, string taskTitle)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultAdvice(taskTitle);
        }

        var text = raw.Trim()
            .Replace("\r\n", " ")
            .Replace('\n', ' ');

        text = Regex.Replace(text, @"\s{2,}", " ").Trim();

        foreach (var pattern in LeadingGreetingPatterns)
        {
            text = pattern.Replace(text, "").Trim();
        }

        if (text.Length > 220)
        {
            text = TrimToLastCompleteSentence(text[..220]);
        }

        if (string.IsNullOrWhiteSpace(text) || text.Length < 6)
        {
            return DefaultAdvice(taskTitle);
        }

        return text;
    }

    private static string DefaultAdvice(string taskTitle) =>
        $"「{taskTitle}」에 맞게 준비물을 챙기고, 작은 목표 하나부터 차근차근 시작해 보세요.";

    private static string TrimToLastCompleteSentence(string text)
    {
        var lastStop = Math.Max(
            text.LastIndexOf('.'),
            Math.Max(text.LastIndexOf('!'), text.LastIndexOf('?')));

        if (lastStop > 30)
        {
            return text[..(lastStop + 1)].Trim();
        }

        var lastSpace = text.LastIndexOf(' ');
        return lastSpace > 20 ? text[..lastSpace].Trim() + "…" : text.Trim() + "…";
    }
}
