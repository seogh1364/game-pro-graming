using System.Text.Json;

namespace PlanMate.Api.Services;

public interface IStudyPlannerService
{
    Task<StudyPlanResult> CreatePlanAsync(StudyPlanRequest request, CancellationToken cancellationToken = default);
}

public sealed class StudyPlannerService(GeminiCompletionService gemini) : IStudyPlannerService
{
    public async Task<StudyPlanResult> CreatePlanAsync(StudyPlanRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("학습 주제를 입력해 주세요.");
        }

        var days = request.Days is > 0 and <= 14 ? request.Days.Value : 5;

        if (gemini.IsConfigured)
        {
            try
            {
                return await CreateWithGeminiAsync(request.Topic, days, cancellationToken);
            }
            catch
            {
                // fall through
            }
        }

        return CreateWithRules(request.Topic, days);
    }

    private async Task<StudyPlanResult> CreateWithGeminiAsync(
        string topic,
        int days,
        CancellationToken cancellationToken)
    {
        var systemInstruction =
            "You are Plan mate study planner. Answer in Korean. Return ONLY valid JSON, no markdown.\n" +
            $"Create a {days}-day study plan.\n" +
            "JSON schema: {\"summary\":\"...\",\"days\":[{\"day\":1,\"title\":\"...\",\"focus\":\"...\",\"duration\":\"90분\"}]}";

        var reply = await gemini.CompleteAsync(
            systemInstruction,
            [new GeminiMessage("user", $"학습 주제: {topic}\n하루 분량까지 나눠 학습 계획을 만들어 주세요.")],
            temperature: 0.5,
            maxOutputTokens: 4096,
            cancellationToken: cancellationToken);

        using var document = JsonDocument.Parse(ExtractJson(reply));
        var root = document.RootElement;
        var summary = root.GetProperty("summary").GetString() ?? $"{topic} 학습 계획을 만들었어요.";

        var planDays = root.GetProperty("days")
            .EnumerateArray()
            .Select(d => new StudyPlanDay(
                d.GetProperty("day").GetInt32(),
                d.GetProperty("title").GetString() ?? "",
                d.GetProperty("focus").GetString() ?? "",
                d.GetProperty("duration").GetString() ?? "60분"))
            .ToList();

        return new StudyPlanResult(topic, summary, planDays, UsedGemini: true);
    }

    private static StudyPlanResult CreateWithRules(string topic, int days)
    {
        var templates = new[]
        {
            ("개념 정리", "핵심 개념과 용어를 정리해요.", "60분"),
            ("예제 풀이", "기본 예제를 직접 풀어봐요.", "90분"),
            ("심화 학습", "어려운 파트를 집중 공략해요.", "90분"),
            ("복습 & 오답", "틀린 문제와 헷갈리는 부분을 복습해요.", "60분"),
            ("실전 문제", "시험/과제 형태로 마무리해요.", "120분"),
            ("총정리", "전체 내용을 한 번에 정리해요.", "90분"),
            ("모의 테스트", "시간 제한 두고 풀어봐요.", "120분")
        };

        var planDays = Enumerable.Range(1, days)
            .Select(day =>
            {
                var template = templates[(day - 1) % templates.Length];
                return new StudyPlanDay(day, $"Day {day}: {template.Item1}", $"{topic} — {template.Item2}", template.Item3);
            })
            .ToList();

        return new StudyPlanResult(
            topic,
            $"{topic} {days}일 학습 계획입니다. 하루 한 단계씩 차근차근 진행해 보세요.",
            planDays,
            UsedGemini: false);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        throw new InvalidOperationException("Study planner returned invalid JSON.");
    }
}
