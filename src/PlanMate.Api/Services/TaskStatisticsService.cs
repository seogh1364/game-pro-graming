namespace PlanMate.Api.Services;

public interface ITaskStatisticsService
{
    TaskProgress GetProgress(IReadOnlyList<UserTask> tasks);
    WeekStatistics GetWeekStatistics(IReadOnlyList<UserTask> tasks);
    IReadOnlyList<DeadlineAlert> GetDeadlineAlerts(IReadOnlyList<UserTask> tasks);
}

public sealed class TaskStatisticsService : ITaskStatisticsService
{
    public TaskProgress GetProgress(IReadOnlyList<UserTask> tasks)
    {
        if (tasks.Count == 0)
        {
            return new TaskProgress(0, 0, 0);
        }

        var completed = tasks.Count(t => t.IsCompleted);
        var percent = (int)Math.Round(completed * 100.0 / tasks.Count);
        return new TaskProgress(tasks.Count, completed, percent);
    }

    public WeekStatistics GetWeekStatistics(IReadOnlyList<UserTask> tasks)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daily = new List<DailyStat>();

        for (var offset = 6; offset >= 0; offset--)
        {
            var day = today.AddDays(-offset);
            var dayTasks = tasks.Where(t =>
                t.IsCompleted &&
                t.CompletedAt.HasValue &&
                DateOnly.FromDateTime(t.CompletedAt.Value) == day);

            daily.Add(new DailyStat(
                day.ToString("ddd", new System.Globalization.CultureInfo("ko-KR")),
                dayTasks.Where(t => t.Category == "study").Sum(t => t.DurationMinutes),
                dayTasks.Where(t => t.Category == "exercise").Sum(t => t.DurationMinutes),
                dayTasks.Where(t => t.Category == "rest").Sum(t => t.DurationMinutes),
                dayTasks.Where(t => t.Category == "general").Sum(t => t.DurationMinutes)));
        }

        return new WeekStatistics(
            daily.Sum(d => d.Study),
            daily.Sum(d => d.Exercise),
            daily.Sum(d => d.Rest),
            daily.Sum(d => d.General),
            daily);
    }

    public IReadOnlyList<DeadlineAlert> GetDeadlineAlerts(IReadOnlyList<UserTask> tasks)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var alerts = new List<DeadlineAlert>();

        foreach (var task in tasks.Where(t => !t.IsCompleted && t.Deadline.HasValue))
        {
            var deadline = DateOnly.FromDateTime(task.Deadline!.Value);
            var daysLeft = deadline.DayNumber - today.DayNumber;
            if (daysLeft < 0 || daysLeft > 3)
            {
                continue;
            }

            var message = daysLeft switch
            {
                0 => "오늘이 마감일이에요!",
                1 => "마감이 내일입니다.",
                _ => $"마감까지 {daysLeft}일 남았어요."
            };

            alerts.Add(new DeadlineAlert(
                task.Id.ToString(),
                task.Title,
                message,
                daysLeft,
                deadline.ToString("yyyy-MM-dd")));
        }

        return alerts.OrderBy(a => a.DaysLeft).ToList();
    }
}
