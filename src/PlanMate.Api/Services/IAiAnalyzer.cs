namespace PlanMate.Api.Services;

public interface IAiAnalyzer
{
    Task<AnalyzeScheduleResult> AnalyzeAsync(string rawText);
}

public sealed class MockAiAnalyzer : IAiAnalyzer
{
    public Task<AnalyzeScheduleResult> AnalyzeAsync(string rawText)
    {
        // This mock provides a replaceable baseline until OpenAI/ML.NET integration.
        var now = DateTime.Now;
        var result = new AnalyzeScheduleResult(
            Title: rawText.Length > 20 ? rawText[..20] : rawText,
            StartAt: now.AddHours(2),
            EndAt: now.AddHours(3),
            Priority: 3,
            Reason: "Mock parser result. Replace with OpenAI API or ML.NET model.");

        return Task.FromResult(result);
    }
}
