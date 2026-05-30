using PlanMate.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPlannerService, PlannerService>();
builder.Services.AddSingleton<IConflictService, ConflictService>();
builder.Services.AddSingleton<IAiAnalyzer, MockAiAnalyzer>();
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();
builder.Services.AddSingleton<ITaskConflictService, TaskConflictService>();
builder.Services.AddSingleton<ITaskStatisticsService, TaskStatisticsService>();
builder.Services.AddSingleton<IScheduleOptimizerService, ScheduleOptimizerService>();
builder.Services.AddSingleton<IStudyPlannerService, StudyPlannerService>();
builder.Services.AddSingleton<TaskAdvisorService>();
builder.Services.AddSingleton<GeminiTaskAdvisorService>();
builder.Services.AddSingleton<GeminiPlanMateChatService>();
builder.Services.AddSingleton<ITaskAdvisor, PlanMateTaskAdvisorService>();
builder.Services.AddSingleton<IWeekendRoutineService, WeekendRoutineService>();
builder.Services.AddHttpClient<GeminiCompletionService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddSingleton<MockPlanMateChatService>();
builder.Services.AddSingleton<IPlanMateChatService, PlanMateChatService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var schedules = new List<ScheduleItem>();

app.MapPost("/api/schedules/analyze", async (AnalyzeScheduleRequest request, IAiAnalyzer analyzer) =>
{
    var analyzed = await analyzer.AnalyzeAsync(request.RawText);
    return Results.Ok(analyzed);
});

app.MapPost("/api/schedules", (CreateScheduleRequest request, IConflictService conflictService, IPlannerService plannerService) =>
{
    var newItem = new ScheduleItem(
        Guid.NewGuid(),
        request.Title,
        request.StartAt,
        request.EndAt,
        request.Priority,
        request.UserId);

    var conflict = conflictService.FindConflict(newItem, schedules);
    if (conflict is not null)
    {
        var alternatives = plannerService.SuggestAlternatives(newItem, schedules);
        return Results.Conflict(new
        {
            Message = "Schedule conflict detected.",
            ConflictWith = conflict,
            Alternatives = alternatives
        });
    }

    schedules.Add(newItem);
    return Results.Created($"/api/schedules/{newItem.Id}", newItem);
});

app.MapGet("/api/schedules/{userId:guid}", (Guid userId) =>
{
    var userSchedules = schedules.Where(x => x.UserId == userId).OrderBy(x => x.StartAt);
    return Results.Ok(userSchedules);
});

app.MapGet("/api/recommendations/{userId:guid}", (Guid userId, IPlannerService plannerService) =>
{
    var userSchedules = schedules.Where(x => x.UserId == userId).ToList();
    var recommendations = plannerService.BuildDailyRecommendations(userSchedules);
    return Results.Ok(recommendations);
});

app.MapGet("/api/recommendations/weekend-workout", (IWeekendRoutineService routineService) =>
    Results.Ok(routineService.GetRandomPlan()));

app.MapGet("/api/weather/recommendation", async (IWeatherService weatherService, CancellationToken ct) =>
    Results.Ok(await weatherService.GetRecommendationAsync(ct)));

app.MapPost("/api/ai/chat", async (ChatRequest request, IPlanMateChatService chatService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { Message = "질문을 입력해 주세요." });
    }

    try
    {
        var response = await chatService.ReplyAsync(request, ct);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/ai/study-plan", async (StudyPlanRequest request, IStudyPlannerService studyPlanner, CancellationToken ct) =>
{
    try
    {
        var plan = await studyPlanner.CreatePlanAsync(request, ct);
        return Results.Ok(plan);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapGet("/api/tasks", (ITaskStore taskStore) => Results.Ok(taskStore.GetAllSorted()));

app.MapGet("/api/tasks/progress", (ITaskStore taskStore, ITaskStatisticsService stats) =>
{
    var tasks = taskStore.GetAllSorted();
    return Results.Ok(stats.GetProgress(tasks));
});

app.MapGet("/api/tasks/deadline-alerts", (ITaskStore taskStore, ITaskStatisticsService stats) =>
    Results.Ok(stats.GetDeadlineAlerts(taskStore.GetAllSorted())));

app.MapGet("/api/statistics/week", (ITaskStore taskStore, ITaskStatisticsService stats) =>
    Results.Ok(stats.GetWeekStatistics(taskStore.GetAllSorted())));

app.MapPost("/api/tasks/predict-duration", (PredictDurationRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { Message = "할 일을 입력해 주세요." });
    }

    var category = TaskHelpers.DetectCategory(request.Title);
    var minutes = TaskHelpers.PredictDurationMinutes(request.Title, category);
    return Results.Ok(new
    {
        DurationMinutes = minutes,
        Category = category,
        Label = FormatDurationLabel(minutes)
    });
});

app.MapPost("/api/tasks/optimize", async (ITaskStore taskStore, IScheduleOptimizerService optimizer, CancellationToken ct) =>
{
    var tasks = taskStore.GetAllSorted();
    if (tasks.Count == 0)
    {
        return Results.BadRequest(new { Message = "재배치할 일정이 없습니다." });
    }

    var result = await optimizer.OptimizeAsync(tasks, ct);
    return Results.Ok(result);
});

app.MapPost("/api/tasks", async (
    AddTaskRequest request,
    ITaskStore taskStore,
    ITaskAdvisor taskAdvisor,
    ITaskConflictService conflictService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { Message = "할 일을 입력해 주세요." });
    }

    if (string.IsNullOrWhiteSpace(request.Time))
    {
        return Results.BadRequest(new { Message = "시간을 입력해 주세요." });
    }

    try
    {
        var adviceResult = await taskAdvisor.GetAdviceAsync(request.Title, request.Time, ct);
        var sortMinutes = TaskHelpers.ParseTimeToMinutes(request.Time);
        var category = TaskHelpers.DetectCategory(request.Title);
        var duration = request.DurationMinutes ?? TaskHelpers.PredictDurationMinutes(request.Title, category);
        var priority = TaskHelpers.ComputePriorityScore(request.IsImportant, request.IsUrgent, request.Title);
        var deadline = TaskHelpers.ParseDeadline(request.Deadline);

        var draft = new UserTask(
            Guid.NewGuid(),
            request.Title.Trim(),
            request.Time,
            sortMinutes,
            sortMinutes + duration,
            duration,
            adviceResult.Advice,
            request.IsImportant,
            request.IsUrgent,
            priority,
            category,
            false,
            null,
            deadline,
            sortMinutes);

        var conflicts = conflictService.FindConflicts(draft, taskStore.GetAllSorted());
        if (conflicts.Count > 0 && !request.ConfirmConflict)
        {
            return Results.Conflict(new
            {
                Message = "겹치는 일정이 있습니다.",
                Conflicts = conflicts
            });
        }

        taskStore.Add(draft);
        return Results.Ok(new
        {
            Tasks = taskStore.GetAllSorted(),
            Created = draft,
            UsedGemini = adviceResult.UsedGemini,
            PredictedDurationMinutes = duration,
            DurationLabel = FormatDurationLabel(duration)
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Message = ex.Message });
    }
});

app.MapPatch("/api/tasks/{id:guid}/complete", (Guid id, ITaskStore taskStore) =>
{
    var updated = taskStore.ToggleComplete(id);
    if (updated is null)
    {
        return Results.NotFound(new { Message = "일정을 찾을 수 없습니다." });
    }

    return Results.Ok(new
    {
        Task = updated,
        Tasks = taskStore.GetAllSorted()
    });
});

app.MapDelete("/api/tasks/{id:guid}", (Guid id, ITaskStore taskStore) =>
{
    if (!taskStore.Delete(id))
    {
        return Results.NotFound(new { Message = "일정을 찾을 수 없습니다." });
    }

    return Results.Ok(taskStore.GetAllSorted());
});

app.Run();

static string FormatDurationLabel(int minutes)
{
    if (minutes < 60)
    {
        return $"{minutes}분";
    }

    var hours = minutes / 60;
    var remainder = minutes % 60;
    return remainder == 0 ? $"{hours}시간" : $"{hours}시간 {remainder}분";
}
