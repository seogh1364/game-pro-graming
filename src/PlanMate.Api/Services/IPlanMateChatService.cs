namespace PlanMate.Api.Services;

public interface IPlanMateChatService
{
    Task<ChatResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
