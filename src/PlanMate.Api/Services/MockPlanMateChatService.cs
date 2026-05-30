namespace PlanMate.Api.Services;

public sealed class MockPlanMateChatService(ITaskStore taskStore) : IPlanMateChatService
{
    public Task<ChatResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Message.Trim();
        var normalized = message.ToLowerInvariant();
        var scheduleContext = BuildScheduleContext();

        var reply = normalized switch
        {
            _ when ContainsAny(normalized, "안녕", "하이", "hello") =>
                "안녕하세요! Plan mate AI예요. 일정 정리, 우선순위, 운동 루틴, 공부 방법 등 무엇이든 물어보세요.",

            _ when ContainsAny(normalized, "오늘", "뭐 해", "뭐하", "계획") =>
                $"오늘은 중요한 일 3개만 고르는 게 좋아요.\n{scheduleContext}\n" +
                "아직 일정이 없다면 '일정' 탭에서 할 일을 등록해 보세요. '추천' 탭에는 주말 운동 루틴도 있어요.",

            _ when ContainsAny(normalized, "우선순위", "순서", "먼저") =>
                "우선순위는 ① 마감이 임박한 일 ② 다른 일에 영향이 큰 일 ③ 30분 안에 끝낼 수 있는 일 순으로 정하면 좋아요. " +
                $"지금 일정 기준으로는 가장 이른 시간부터 차근차근 진행하세요.\n{scheduleContext}",

            _ when ContainsAny(normalized, "운동", "헬스", "루틴", "스트레칭") =>
                "운동은 워밍업 10분 → 메인 30~40분 → 쿨다운 10분 구조가 안전해요. " +
                "주말 루틴이 필요하면 상단 '추천' 메뉴에서 랜덤 운동 시간표를 받아보세요.",

            _ when ContainsAny(normalized, "공부", "시험", "집중", "딴짓") =>
                "공부는 25분 집중 + 5분 휴식을 4세트 반복해 보세요. " +
                "시작 전에 '오늘 끝낼 목표 1개'만 적으면 훨씬 덜 막막해요.",

            _ when ContainsAny(normalized, "회의", "미팅") =>
                "회의 전에는 안건 3개, 결정해야 할 것 1개, 준비물을 미리 적어 두세요. " +
                "회의 후 5분 안에 할 일·담당자·마감일만 정리하면 실행력이 올라가요.",

            _ when ContainsAny(normalized, "스트레스", "피곤", "지침", "번아웃") =>
                "지칠 때는 할 일을 줄이고, 수면·수분·가벼운 산책부터 챙기세요. " +
                "오늘은 'must do 1개'만 남기고 나머지는 내일로 미뤄도 괜찮아요.",

            _ when ContainsAny(normalized, "시간관리", "시간 관리", "루틴") =>
                "시간 관리는 완벽한 계획보다 '실행 가능한 블록'이 중요해요. " +
                "오전·오후·저녁으로 3덩어리만 나눠 각각 핵심 1개씩 배치해 보세요.",

            _ when ContainsAny(normalized, "추천", "추천해") =>
                "Plan mate에서는 '추천' 탭에 주말 운동 루틴이 준비되어 있어요. " +
                "버튼을 누를 때마다 다른 시간표가 랜덤으로 나옵니다.",

            _ when message.Length < 4 =>
                "조금만 더 구체적으로 말해 주시면 더 맞춤 답변을 드릴게요. 예: '오늘 일정 어떻게 짜면 좋을까?'",

            _ => BuildContextualReply(message, scheduleContext)
        };

        return Task.FromResult(new ChatResponse(reply, UsedGemini: false));
    }

    private string BuildScheduleContext()
    {
        var tasks = taskStore.GetAllSorted();
        if (tasks.Count == 0)
        {
            return "현재 등록된 일정이 없어요.";
        }

        var lines = tasks.Select(t => $"- {t.Time} {t.Title}");
        return "등록된 일정:\n" + string.Join("\n", lines);
    }

    private static string BuildContextualReply(string message, string scheduleContext)
    {
        return $"질문을 잘 받았어요. '{message}'에 대해 이렇게 제안할게요.\n\n" +
               "1) 목표를 한 문장으로 줄이기\n" +
               "2) 30분 안에 할 수 있는 첫 행동 정하기\n" +
               "3) 끝나면 5분 안에 다음 일정으로 넘기기\n\n" +
               $"{scheduleContext}\n\n" +
               "더 똑똑한 대화를 원하시면 appsettings.json에 Google API 키(Gemini)를 넣으면 실제 AI 모델로 답변합니다.";
    }

    private static bool ContainsAny(string text, params string[] keywords) =>
        keywords.Any(text.Contains);
}
