namespace PlanMate.Api.Services;

public interface IPlannerService
{
    IReadOnlyList<RecommendationItem> SuggestAlternatives(ScheduleItem target, IReadOnlyList<ScheduleItem> schedules);
    IReadOnlyList<RecommendationItem> BuildDailyRecommendations(IReadOnlyList<ScheduleItem> schedules);
}

public sealed class PlannerService : IPlannerService
{
    public IReadOnlyList<RecommendationItem> SuggestAlternatives(ScheduleItem target, IReadOnlyList<ScheduleItem> schedules)
    {
        var userSchedules = schedules
            .Where(s => s.UserId == target.UserId)
            .OrderBy(s => s.StartAt)
            .ToList();

        var suggestions = new List<RecommendationItem>();
        var duration = target.EndAt - target.StartAt;
        var candidateStart = target.StartAt.AddHours(1);

        for (var i = 0; i < 3; i++)
        {
            var candidateEnd = candidateStart + duration;
            var overlaps = userSchedules.Any(x => candidateStart < x.EndAt && candidateEnd > x.StartAt);
            if (!overlaps)
            {
                suggestions.Add(new RecommendationItem(
                    "Available time slot found.",
                    candidateStart,
                    candidateEnd,
                    target.Priority));
            }

            candidateStart = candidateStart.AddHours(1);
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new RecommendationItem(
                "No immediate slot found. Consider reducing task duration.",
                target.StartAt.AddHours(4),
                target.EndAt.AddHours(4),
                target.Priority - 1));
        }

        return suggestions;
    }

    public IReadOnlyList<RecommendationItem> BuildDailyRecommendations(IReadOnlyList<ScheduleItem> schedules)
    {
        if (schedules.Count == 0)
        {
            return new[]
            {
                new RecommendationItem(
                    "No schedules found. Add your first plan.",
                    DateTime.Today.AddHours(9),
                    DateTime.Today.AddHours(10),
                    3)
            };
        }

        var ordered = schedules.OrderByDescending(x => x.Priority).ThenBy(x => x.StartAt).ToList();
        return ordered.Select(x => new RecommendationItem(
            $"Prioritize: {x.Title}",
            x.StartAt,
            x.EndAt,
            x.Priority)).ToList();
    }
}
