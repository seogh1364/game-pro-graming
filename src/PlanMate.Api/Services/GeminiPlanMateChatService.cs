namespace PlanMate.Api.Services;

public sealed class GeminiPlanMateChatService(
    GeminiCompletionService gemini,
    ITaskStore taskStore) : IPlanMateChatService
{
    public async Task<ChatResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var scheduleContext = BuildScheduleContext();
        var systemInstruction =
            "You are Plan mate, a friendly Korean AI schedule assistant. " +
            "Answer in Korean. Be practical, warm, and clear. " +
            "Always finish your sentences completely; never stop mid-sentence. " +
            "Use 2-5 short paragraphs when helpful. " +
            "Help with planning, priorities, exercise, study, and work habits. " +
            $"User schedule context:\n{scheduleContext}";

        var contents = new List<GeminiMessage>();

        if (request.History is not null)
        {
            foreach (var item in request.History.TakeLast(8))
            {
                contents.Add(new GeminiMessage(item.Role, item.Content));
            }
        }

        contents.Add(new GeminiMessage("user", request.Message));

        var reply = await gemini.CompleteAsync(
            systemInstruction,
            contents,
            temperature: 0.7,
            maxOutputTokens: 8192,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(reply))
        {
            return new ChatResponse("답변을 생성하지 못했어요.", UsedGemini: true);
        }

        return new ChatResponse(reply, UsedGemini: true);
    }

    private string BuildScheduleContext()
    {
        var tasks = taskStore.GetAllSorted();
        return tasks.Count == 0
            ? "등록된 일정 없음"
            : string.Join("; ", tasks.Select(t => $"{t.Time} {t.Title}"));
    }
}
