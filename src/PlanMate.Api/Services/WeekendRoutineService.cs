namespace PlanMate.Api.Services;

public record RoutineSlot(string Time, string Activity, string Tip);

public record WeekendRoutinePlan(string Title, string DayLabel, string Summary, IReadOnlyList<RoutineSlot> Slots);

public interface IWeekendRoutineService
{
    WeekendRoutinePlan GetRandomPlan();
}

public sealed class WeekendRoutineService : IWeekendRoutineService
{
    private static readonly Random Random = new();

    private static readonly List<Func<WeekendRoutinePlan>> Plans =
    [
        BuildLightRunPlan,
        BuildGymStrengthPlan,
        BuildHomeWorkoutPlan,
        BuildOutdoorActivePlan,
        BuildRecoveryYogaPlan
    ];

    public WeekendRoutinePlan GetRandomPlan() => Plans[Random.Next(Plans.Count)]();

    private static WeekendRoutinePlan BuildLightRunPlan() => new(
        "가벼운 러닝 & 코어 루틴",
        "토요일",
        "유산소 위주로 부담 없이 몸을 깨우고, 코어로 마무리하는 주말 플랜이에요.",
        [
            Slot("08:00", "기상 · 물 500ml · 가벼운 스트레칭 10분", "관절 가동으로 시작하세요."),
            Slot("08:30", "브런치 (단백질+탄수화물)", "운동 1시간 전 가볍게 드세요."),
            Slot("09:30", "워밍업 걷기 10분", "호흡을 고르게 맞추세요."),
            Slot("09:45", "조깅 25분", "대화 가능한 페이스가 적당해요."),
            Slot("10:15", "인터벌 질주 6회 (1분 달리기 / 1분 걷기)", "무리하지 말고 6회만."),
            Slot("10:45", "쿨다운 걷기 5분 + 스트레칭 10분", "종아리·허벅지 위주로."),
            Slot("11:30", "코어 서킷 3세트 (플랭크·크런치·버피)", "세트 사이 1분 휴식."),
            Slot("13:00", "점심 · 단백질 보충", "닭가슴살·두부·계란 추천."),
            Slot("16:00", "가벼운 산책 30분", "회복 산책으로 컨디션 유지."),
            Slot("20:30", "수면 준비 · 폼롤러 10분", "다음 날 근육통 예방.")
        ]);

    private static WeekendRoutinePlan BuildGymStrengthPlan() => new(
        "헬스 근력 + 유산소 믹스",
        "토요일",
        "상체·하체를 나눠 근력 운동 후, 짧은 유산소로 마무리해요.",
        [
            Slot("08:30", "아침 식사 · 워밍업", "공복 운동은 피하는 게 좋아요."),
            Slot("09:30", "헬스장 도착 · 동적 스트레칭", "어깨·고관절 풀기."),
            Slot("10:00", "하체 메인 (스쿼트·런지·레그프레스)", "무게는 8~12회 가능한 중량."),
            Slot("11:00", "상체 보조 (랫풀다운·푸시업·덤벨 로우)", "자세 우선, 3세트."),
            Slot("12:00", "유산소 20분 (사이클 또는 트레드밀)", "중강도로 땀 살짝."),
            Slot("12:30", "쿨다운 · 샤워", "단백질 쉐이크 준비."),
            Slot("13:30", "점심 (고단백)", "탄수는 현미·고구마 정도."),
            Slot("15:00", "휴식 · 스트레칭 15분", "허리·햄스트링 위주."),
            Slot("17:00", "가벼운 수영 또는 걷기 (선택)", "관절 부담 적은 유산소."),
            Slot("21:00", "일찍 취침", "근육 회복은 수면이 핵심.")
        ]);

    private static WeekendRoutinePlan BuildHomeWorkoutPlan() => new(
        "홈트 집중 루틴",
        "일요일",
        "집에서 40~50분 운동 + 휴식을 반복하는 균형 플랜이에요.",
        [
            Slot("09:00", "물·간단 스트레칭", "운동 매트 준비."),
            Slot("09:20", "전신 워밍업 10분", "점핑잭·하이니·암서클."),
            Slot("09:35", "하체 (스쿼트·왕복런지) 4세트", "무릎 방향 주의."),
            Slot("10:05", "상체 (푸시업·숄더탭) 4세트", "무릎 푸시업도 OK."),
            Slot("10:35", "복부 (마운틴클라이머·레그레이즈)", "코어 15분."),
            Slot("11:00", "타바타 4라운드 (20초 운동/10초 휴식)", "버피·스쿼트 점프 번갈아."),
            Slot("11:30", "쿨다운 요가 10분", "호흡 길게."),
            Slot("12:30", "점심", "채소·단백질 골고루."),
            Slot("15:30", "스트레칭·폼롤러", "뭉친 부위 집중."),
            Slot("19:00", "저녁 산책 20분", "소화·회복에 도움.")
        ]);

    private static WeekendRoutinePlan BuildOutdoorActivePlan() => new(
        "야외 액티브 주말",
        "일요일",
        "밖에서 움직이며 기분 전환도 하는 루틴이에요.",
        [
            Slot("07:30", "기상 · 아침 공복 걷기 15분", "가볍게만."),
            Slot("08:30", "아침 식사", "바나나·요거트·견과류."),
            Slot("09:30", "공원 조깅 30분", "음악·팟캐스트와 함께."),
            Slot("10:15", "맨몸 근력 (벤치 딥스·스텝업)", "공원 벤치 활용."),
            Slot("11:00", "자전거 라이딩 40분", "평지 위주 추천."),
            Slot("12:30", "점심 · 수분 보충", "이온음료 또는 물."),
            Slot("14:00", "휴식 · 독서/낮잠 30분", "과훈련 방지."),
            Slot("16:00", "가벼운 등산 또는 계단 오르기 25분", "페이스 일정하게."),
            Slot("18:00", "저녁 · 단백질 식사", "회복 식단."),
            Slot("20:00", "야간 스트레칭 15분", "종아리·종아리 앞쪽.")
        ]);

    private static WeekendRoutinePlan BuildRecoveryYogaPlan() => new(
        "회복·요가 중심 루틴",
        "토요일",
        "무리한 운동 대신 몸을 풀고 유연성을 챙기는 날이에요.",
        [
            Slot("08:00", "따뜻한 물 · 명상 5분", "호흡부터 정리."),
            Slot("08:30", "요가 워밍업 (태양예배 2라운드)", "천천히 동작."),
            Slot("09:15", "하체 요가 (전사·전굴·비둘기)", "무릎 통증 시 변형."),
            Slot("10:00", "상체·코어 요가 20분", "플랭크·측면 플랭크."),
            Slot("10:40", "밸런스 동작 (트리·이글)", "한쪽씩 30초."),
            Slot("11:15", "샤바아사나(최종 휴식) 10분", "완전히 이완."),
            Slot("12:00", "점심 · 채소 위주", "가벼운 식사."),
            Slot("14:30", "폼롤러 + 고정 스트레칭", "뭉친 부위 2곳만."),
            Slot("16:30", "산책 40분", "걷기만으로도 충분해요."),
            Slot("20:30", "따뜻한 샤워 · 스트레칭", "숙면 준비.")
        ]);

    private static RoutineSlot Slot(string time, string activity, string tip) => new(time, activity, tip);
}
