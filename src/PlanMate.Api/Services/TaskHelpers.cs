namespace PlanMate.Api.Services;

internal static class TaskHelpers
{
    public static int ParseTimeToMinutes(string time)
    {
        if (!TimeOnly.TryParse(time, out var parsed))
        {
            throw new ArgumentException("Invalid time format. Use HH:mm.");
        }

        return parsed.Hour * 60 + parsed.Minute;
    }

    public static string FormatMinutesRange(int startMinutes, int endMinutes)
    {
        return $"{FormatMinutes(startMinutes)}~{FormatMinutes(endMinutes)}";
    }

    public static string FormatMinutes(int totalMinutes)
    {
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{hours:D2}:{minutes:D2}";
    }

    public static int ComputePriorityScore(bool isImportant, bool isUrgent, string title)
    {
        var baseScore = (isImportant, isUrgent) switch
        {
            (true, true) => 40,
            (true, false) => 30,
            (false, true) => 20,
            _ => 10
        };

        var normalized = title.ToLowerInvariant();
        if (ContainsAny(normalized, "시험", "공부", "과제", "제출", "발표", "면접", "프로젝트"))
        {
            baseScore += 15;
        }

        if (ContainsAny(normalized, "유튜브", "넷플릭스", "게임", "딴짓", "쇼츠"))
        {
            baseScore -= 12;
        }

        return Math.Clamp(baseScore, 1, 60);
    }

    public static string DetectCategory(string title)
    {
        var normalized = title.ToLowerInvariant();
        if (ContainsAny(normalized, "공부", "시험", "과제", "학습", "복습", "강의", "자료구조", "코딩"))
        {
            return "study";
        }

        if (ContainsAny(normalized, "운동", "헬스", "런닝", "조깅", "산책", "스트레칭", "요가"))
        {
            return "exercise";
        }

        if (ContainsAny(normalized, "휴식", "낮잠", "쉬", "산책"))
        {
            return "rest";
        }

        return "general";
    }

    public static int PredictDurationMinutes(string title, string? category = null)
    {
        var normalized = title.ToLowerInvariant();
        category ??= DetectCategory(title);

        if (ContainsAny(normalized, "과제", "프로젝트", "레포트"))
        {
            return 120;
        }

        if (ContainsAny(normalized, "시험", "공부", "복습", "자료구조"))
        {
            return 90;
        }

        if (ContainsAny(normalized, "회의", "미팅"))
        {
            return 60;
        }

        if (ContainsAny(normalized, "운동", "헬스", "런닝"))
        {
            return 60;
        }

        if (ContainsAny(normalized, "유튜브", "넷플릭스", "게임"))
        {
            return 30;
        }

        return category switch
        {
            "study" => 90,
            "exercise" => 60,
            "rest" => 20,
            _ => 60
        };
    }

    public static DateTime? ParseDeadline(string? deadline)
    {
        if (string.IsNullOrWhiteSpace(deadline))
        {
            return null;
        }

        if (DateOnly.TryParse(deadline, out var dateOnly))
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] keywords) =>
        keywords.Any(text.Contains);
}
