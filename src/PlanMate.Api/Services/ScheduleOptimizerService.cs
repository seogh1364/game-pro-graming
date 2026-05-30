using System.Text.Json;

namespace PlanMate.Api.Services;

public interface IScheduleOptimizerService
{
    Task<OptimizeResult> OptimizeAsync(IReadOnlyList<UserTask> tasks, CancellationToken cancellationToken = default);
}

public sealed class ScheduleOptimizerService(
    GeminiCompletionService gemini,
    ITaskStore taskStore) : IScheduleOptimizerService
{
    public async Task<OptimizeResult> OptimizeAsync(IReadOnlyList<UserTask> tasks, CancellationToken cancellationToken = default)
    {
        if (tasks.Count == 0)
        {
            return new OptimizeResult("정렬할 일정이 없어요.", tasks, UsedGemini: false);
        }

        if (gemini.IsConfigured)
        {
            try
            {
                return await OptimizeWithGeminiAsync(tasks, cancellationToken);
            }
            catch
            {
                // fall through to rule-based optimizer
            }
        }

        return OptimizeWithRules(tasks);
    }

    private async Task<OptimizeResult> OptimizeWithGeminiAsync(
        IReadOnlyList<UserTask> tasks,
        CancellationToken cancellationToken)
    {
        var taskLines = string.Join("\n", tasks.Select(t =>
            $"- id:{t.Id} | {t.Title} | {t.Time} | {t.DurationMinutes}분 | priority:{t.PriorityScore} | important:{t.IsImportant} | urgent:{t.IsUrgent}"));

        var systemInstruction =
            "You are Plan mate schedule optimizer. Return ONLY valid JSON, no markdown.\n" +
            "Reorder tasks by priority and time efficiency. Insert short breaks where needed.\n" +
            "JSON schema: {\"summary\":\"...\",\"orderedIds\":[\"guid\",...],\"breaks\":[{\"afterId\":\"guid\",\"label\":\"5분 휴식\",\"minutes\":5}]}";

        var reply = await gemini.CompleteAsync(
            systemInstruction,
            [new GeminiMessage("user", $"Optimize these tasks:\n{taskLines}")],
            temperature: 0.3,
            maxOutputTokens: 2048,
            cancellationToken: cancellationToken);

        using var document = JsonDocument.Parse(ExtractJson(reply));
        var root = document.RootElement;
        var summary = root.GetProperty("summary").GetString() ?? "AI가 일정 순서를 재배치했어요.";

        var orderedIds = root.GetProperty("orderedIds")
            .EnumerateArray()
            .Select(x => Guid.Parse(x.GetString()!))
            .ToList();

        taskStore.ReplaceOrder(orderedIds);

        var breaks = root.TryGetProperty("breaks", out var breaksElement)
            ? breaksElement.EnumerateArray()
                .Select(b => $"{b.GetProperty("label").GetString()} ({b.GetProperty("minutes").GetInt32()}분)")
                .ToList()
            : [];

        if (breaks.Count > 0)
        {
            summary += " 휴식: " + string.Join(", ", breaks) + ".";
        }

        return new OptimizeResult(summary, taskStore.GetAllSorted(), UsedGemini: true);
    }

    private OptimizeResult OptimizeWithRules(IReadOnlyList<UserTask> tasks)
    {
        var ordered = tasks
            .OrderByDescending(t => t.PriorityScore)
            .ThenBy(t => t.SortMinutes)
            .Select((t, index) => t with { SortOrder = index })
            .ToList();

        taskStore.UpdateTasks(ordered);

        return new OptimizeResult(
            "중요·긴급도와 시작 시간을 기준으로 일정을 재배치했어요. 50분마다 5분 휴식을 넣어보세요.",
            taskStore.GetAllSorted(),
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

        throw new InvalidOperationException("Optimizer returned invalid JSON.");
    }
}
