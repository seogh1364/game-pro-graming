namespace PlanMate.Api.Services;

public record ChatMessage(string Role, string Content);

public record ChatRequest(string Message, List<ChatMessage>? History);

public record ChatResponse(string Reply, bool UsedGemini);
