namespace PlanMate.Api.Services;

public interface ITaskAdvisor
{
    Task<AdviceResult> GetAdviceAsync(string title, string time, CancellationToken cancellationToken = default);
}

public sealed class TaskAdvisorService : ITaskAdvisor
{
    public Task<AdviceResult> GetAdviceAsync(string title, string time, CancellationToken cancellationToken = default)
    {
        var advice = AdviceTextHelper.NormalizeForCard(GetAdvice(title, time), title);
        return Task.FromResult(new AdviceResult(advice, UsedGemini: false));
    }

    public string GetAdvice(string title, string time)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var minutes = ParseTimeToMinutes(time);
        var timeTip = GetTimeTip(minutes);
        var taskTip = GetTaskTip(normalized, title);

        return $"{taskTip} {timeTip}".Trim();
    }

    private static string GetTaskTip(string normalized, string originalTitle)
    {
        if (ContainsAny(normalized, "회의", "미팅", "meeting"))
        {
            return $"「{originalTitle}」은(는) 시작 10분 전에 도착하고, 안건 3가지를 미리 정리해 두면 효율적이에요.";
        }

        if (ContainsAny(normalized, "공부", "학습", "시험", "복습", "과제"))
        {
            return $"「{originalTitle}」은(는) 25분 집중 + 5분 휴식(포모도로)으로 나누고, 핵심 개념 1개만 먼저 정리해 보세요.";
        }

        if (ContainsAny(normalized, "운동", "헬스", "조깅", "러닝", "요가"))
        {
            return $"「{originalTitle}」 전후로 가벼운 스트레칭 5분을 넣고, 수분 섭취를 챙기면 부상 예방에 좋아요.";
        }

        if (ContainsAny(normalized, "보고서", "발표", "ppt", "프레젠"))
        {
            return $"「{originalTitle}」은(는) 결론 → 근거 → 실행안 순으로 초안을 쓰고, 마지막 10분은 표현만 다듬으세요.";
        }

        if (ContainsAny(normalized, "이메일", "메일", "답장"))
        {
            return $"「{originalTitle}」은(는) 제목-요청사항-마감일 3줄로 짧게 쓰고, 답장은 한 번에 처리하는 게 좋아요.";
        }

        if (ContainsAny(normalized, "청소", "정리", "설거지", "빨래"))
        {
            return $"「{originalTitle}」은(는) 15분 타이머를 켜고, 보이는 곳부터 빠르게 치우면 부담이 줄어요.";
        }

        if (ContainsAny(normalized, "병원", "진료", "검진"))
        {
            return $"「{originalTitle}」 전에 보험증·문진표·궁금한 증상 메모를 준비해 두세요.";
        }

        if (ContainsAny(normalized, "쇼핑", "장보기", "마트"))
        {
            return $"「{originalTitle}」은(는) 구매 목록을 카테고리별로 적고, 예산 상한을 먼저 정해 두세요.";
        }

        if (ContainsAny(normalized, "약속", "만나", "데이트", "식사"))
        {
            return $"「{originalTitle}」은(는) 이동 시간을 20% 여유 있게 잡고, 장소·시간을 한 번 더 확인하세요.";
        }

        return $"「{originalTitle}」은(는) 가장 작은 첫 단계 1개만 정한 뒤, 시작 5분 안에 실행에 옮겨 보세요.";
    }

    private static string GetTimeTip(int sortMinutes)
    {
        return sortMinutes switch
        {
            < 8 * 60 => "이른 시간이니 전날 밤에 준비물을 챙겨 두면 아침이 편해요.",
            < 12 * 60 => "오전 일정은 집중력이 높으니 어려운 작업을 앞에 배치하세요.",
            < 14 * 60 => "점심 전후 30분은 가벼운 일정으로 두면 컨디션 관리에 좋아요.",
            < 18 * 60 => "오후에는 진행 상황을 한 번 점검하고 우선순위를 조정해 보세요.",
            < 21 * 60 => "저녁 시간대엔 마무리·정리 작업 위주로 계획하면 좋아요.",
            _ => "늦은 시간이니 무리한 작업보다 내일 이월할 항목을 정리해 두세요."
        };
    }

    private static int ParseTimeToMinutes(string time)
    {
        if (!TimeOnly.TryParse(time, out var parsed))
        {
            return 12 * 60;
        }

        return parsed.Hour * 60 + parsed.Minute;
    }

    private static bool ContainsAny(string text, params string[] keywords) =>
        keywords.Any(text.Contains);
}
