namespace PlanMate.Api.Services;

public record PredictDurationRequest(string Title);

public record AddTaskRequest(
    string Title,
    string Time,
    bool IsImportant = false,
    bool IsUrgent = false,
    int? DurationMinutes = null,
    string? Deadline = null,
    bool ConfirmConflict = false);

public record UserTask(
    Guid Id,
    string Title,
    string Time,
    int SortMinutes,
    int EndSortMinutes,
    int DurationMinutes,
    string Advice,
    bool IsImportant,
    bool IsUrgent,
    int PriorityScore,
    string Category,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime? Deadline,
    int SortOrder);

public record TaskConflictInfo(
    Guid TaskId,
    string Title,
    string Time,
    int DurationMinutes,
    string OverlapRange);

public record TaskProgress(int Total, int Completed, int Percent);

public record DailyStat(string DayLabel, int Study, int Exercise, int Rest, int General);

public record WeekStatistics(
    int StudyMinutes,
    int ExerciseMinutes,
    int RestMinutes,
    int GeneralMinutes,
    IReadOnlyList<DailyStat> Daily);

public record OptimizeResult(string Summary, IReadOnlyList<UserTask> Tasks, bool UsedGemini);

public record StudyPlanRequest(string Topic, int? Days = null);

public record StudyPlanDay(int Day, string Title, string Focus, string Duration);

public record StudyPlanResult(string Topic, string Summary, IReadOnlyList<StudyPlanDay> Days, bool UsedGemini);

public record DeadlineAlert(string TaskId, string Title, string Message, int DaysLeft, string DeadlineLabel);

public record WeatherRecommendation(
    string Condition,
    string Temperature,
    string Recommendation,
    string Activity,
    string Icon);

public interface ITaskStore
{
    IReadOnlyList<UserTask> GetAllSorted();
    UserTask? GetById(Guid id);
    UserTask Add(UserTask task);
    bool Delete(Guid id);
    UserTask? ToggleComplete(Guid id);
    void ReplaceOrder(IReadOnlyList<Guid> orderedIds);
    void UpdateTasks(IReadOnlyList<UserTask> tasks);
}

public sealed class InMemoryTaskStore : ITaskStore
{
    private readonly List<UserTask> _tasks = [];

    public IReadOnlyList<UserTask> GetAllSorted() =>
        _tasks
            .OrderByDescending(t => t.PriorityScore)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.SortMinutes)
            .ToList();

    public UserTask? GetById(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

    public UserTask Add(UserTask task)
    {
        _tasks.Add(task);
        return task;
    }

    public bool Delete(Guid id)
    {
        var index = _tasks.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            return false;
        }

        _tasks.RemoveAt(index);
        return true;
    }

    public UserTask? ToggleComplete(Guid id)
    {
        var index = _tasks.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            return null;
        }

        var current = _tasks[index];
        var updated = current with
        {
            IsCompleted = !current.IsCompleted,
            CompletedAt = !current.IsCompleted ? DateTime.Now : null
        };
        _tasks[index] = updated;
        return updated;
    }

    public void ReplaceOrder(IReadOnlyList<Guid> orderedIds)
    {
        var orderMap = orderedIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        for (var i = 0; i < _tasks.Count; i++)
        {
            var task = _tasks[i];
            if (orderMap.TryGetValue(task.Id, out var order))
            {
                _tasks[i] = task with { SortOrder = order };
            }
        }
    }

    public void UpdateTasks(IReadOnlyList<UserTask> tasks)
    {
        _tasks.Clear();
        _tasks.AddRange(tasks);
    }
}
