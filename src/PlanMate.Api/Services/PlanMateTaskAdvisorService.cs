namespace PlanMate.Api.Services;

public sealed class PlanMateTaskAdvisorService(
    GeminiCompletionService gemini,
    TaskAdvisorService mockAdvisor,
    GeminiTaskAdvisorService geminiAdvisor) : ITaskAdvisor
{
    public async Task<AdviceResult> GetAdviceAsync(string title, string time, CancellationToken cancellationToken = default)
    {
        if (gemini.IsConfigured)
        {
            try
            {
                var advice = await geminiAdvisor.GetAdviceAsync(title, time, cancellationToken);
                return new AdviceResult(advice, UsedGemini: true);
            }
            catch
            {
                var fallback = AdviceTextHelper.NormalizeForCard(mockAdvisor.GetAdvice(title, time), title);
                return new AdviceResult(fallback, UsedGemini: false);
            }
        }

        return new AdviceResult(mockAdvisor.GetAdvice(title, time), UsedGemini: false);
    }
}

public record AdviceResult(string Advice, bool UsedGemini);
