namespace PlanMate.Api.Services;

public record AnalyzeScheduleRequest(string RawText);

public record AnalyzeScheduleResult(
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int Priority,
    string Reason);

public record CreateScheduleRequest(
    Guid UserId,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int Priority);

public record ScheduleItem(
    Guid Id,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int Priority,
    Guid UserId);

public record RecommendationItem(
    string Message,
    DateTime SuggestedStartAt,
    DateTime SuggestedEndAt,
    int SuggestedPriority);
