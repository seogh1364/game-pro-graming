namespace PlanMate.Api.Services;

public interface ITaskConflictService
{
    IReadOnlyList<TaskConflictInfo> FindConflicts(UserTask incoming, IEnumerable<UserTask> existing);
}

public sealed class TaskConflictService : ITaskConflictService
{
    public IReadOnlyList<TaskConflictInfo> FindConflicts(UserTask incoming, IEnumerable<UserTask> existing)
    {
        return existing
            .Where(item => item.Id != incoming.Id)
            .Where(item => incoming.SortMinutes < item.EndSortMinutes && incoming.EndSortMinutes > item.SortMinutes)
            .Select(item => new TaskConflictInfo(
                item.Id,
                item.Title,
                item.Time,
                item.DurationMinutes,
                TaskHelpers.FormatMinutesRange(
                    Math.Max(incoming.SortMinutes, item.SortMinutes),
                    Math.Min(incoming.EndSortMinutes, item.EndSortMinutes))))
            .ToList();
    }
}
