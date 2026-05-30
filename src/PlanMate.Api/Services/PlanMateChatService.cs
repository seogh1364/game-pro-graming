namespace PlanMate.Api.Services;

/// <summary>
/// Google API key가 있으면 Gemini, 없으면 내장 Mock AI로 응답합니다.
/// </summary>
public sealed class PlanMateChatService(
    GeminiCompletionService gemini,
    MockPlanMateChatService mockChat,
    GeminiPlanMateChatService geminiChat) : IPlanMateChatService
{
    public async Task<ChatResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (gemini.IsConfigured)
        {
            try
            {
                return await geminiChat.ReplyAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                var fallback = await mockChat.ReplyAsync(request, cancellationToken);
                return new ChatResponse(
                    fallback.Reply + $"\n\n⚠️ {ex.Message}",
                    UsedGemini: false);
            }
        }

        return await mockChat.ReplyAsync(request, cancellationToken);
    }
}
