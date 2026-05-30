namespace PlanMate.Api.Services;

public interface IConflictService
{
    ScheduleItem? FindConflict(ScheduleItem incoming, IEnumerable<ScheduleItem> existing);
}

public sealed class ConflictService : IConflictService
{
    public ScheduleItem? FindConflict(ScheduleItem incoming, IEnumerable<ScheduleItem> existing)
    {
        return existing.FirstOrDefault(item =>
            item.UserId == incoming.UserId &&
            incoming.StartAt < item.EndAt &&
            incoming.EndAt > item.StartAt);
    }
}
