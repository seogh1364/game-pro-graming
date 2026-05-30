namespace PlanMate.Api.Services;

public sealed class GeminiTaskAdvisorService(
    GeminiCompletionService gemini,
    ITaskStore taskStore)
{
    public async Task<string> GetAdviceAsync(string title, string time, CancellationToken cancellationToken = default)
    {
        var scheduleContext = BuildScheduleContext();
        var timeLabel = FormatTimeLabel(time);

        var systemInstruction =
            "당신은 Plan mate 일정 코치입니다. 반드시 한국어로만 답하세요.\n" +
            "규칙:\n" +
            "1) 인사말·자기소개·'플랜메이트입니다' 같은 문구는 절대 쓰지 마세요.\n" +
            "2) 사용자 할 일과 예정 시간에 맞는 실행 팁을 2~3문장으로 작성하세요.\n" +
            "3) 150~220자, 존댓말, 따뜻하고 구체적으로. 문장은 반드시 끝까지 완성하세요.\n" +
            "4) 불릿·번호 목록 없이 문장만 작성하세요.\n" +
            $"오늘 다른 일정: {scheduleContext}";

        var contents = new List<GeminiMessage>
        {
            new("user",
                $"할 일: {title}\n시간: {timeLabel}\n" +
                "위 일정을 잘 수행하는 방법만 짧게 알려주세요.")
        };

        var reply = await gemini.CompleteAsync(
            systemInstruction,
            contents,
            temperature: 0.5,
            maxOutputTokens: 2048,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("Gemini returned an empty advice response.");
        }

        return AdviceTextHelper.NormalizeForCard(reply, title);
    }

    private string BuildScheduleContext()
    {
        var tasks = taskStore.GetAllSorted();
        return tasks.Count == 0
            ? "없음"
            : string.Join(", ", tasks.Select(t => $"{t.Time} {t.Title}"));
    }

    private static string FormatTimeLabel(string time)
    {
        if (!TimeOnly.TryParse(time, out var parsed))
        {
            return time;
        }

        var period = parsed.Hour < 12 ? "오전" : "오후";
        var hour12 = parsed.Hour % 12 == 0 ? 12 : parsed.Hour % 12;
        return $"{period} {hour12}:{parsed.Minute:D2}";
    }
}
