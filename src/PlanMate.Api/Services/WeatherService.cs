using System.Text.Json;

namespace PlanMate.Api.Services;

public interface IWeatherService
{
    Task<WeatherRecommendation> GetRecommendationAsync(CancellationToken cancellationToken = default);
}

public sealed class WeatherService(HttpClient httpClient) : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WeatherRecommendation> GetRecommendationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                "https://api.open-meteo.com/v1/forecast?latitude=37.5665&longitude=126.9780" +
                "&current=temperature_2m,weather_code&timezone=Asia%2FSeoul";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Fallback("날씨 정보를 불러오지 못했어요.");
            }

            using var document = JsonDocument.Parse(body);
            var current = document.RootElement.GetProperty("current");
            var code = current.GetProperty("weather_code").GetInt32();
            var temp = current.GetProperty("temperature_2m").GetDouble();
            return BuildRecommendation(code, temp);
        }
        catch
        {
            return Fallback("날씨 API 연결에 실패했어요.");
        }
    }

    private static WeatherRecommendation BuildRecommendation(int code, double temp)
    {
        var tempLabel = $"{Math.Round(temp)}°C";

        if (code is 51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82)
        {
            return new WeatherRecommendation(
                "비",
                tempLabel,
                "비가 오니 실내 운동을 추천해요.",
                "홈트 · 요가 · 스트레칭 · 실내 클라이밍",
                "🌧️");
        }

        if (code is 71 or 73 or 75 or 77 or 85 or 86)
        {
            return new WeatherRecommendation(
                "눈",
                tempLabel,
                "눈이 오니 실내 운동과 가벼운 스트레칭을 추천해요.",
                "실내 유산소 · 요가 · 맨몸 근력",
                "❄️");
        }

        if (code is 0 or 1 or 2)
        {
            return new WeatherRecommendation(
                "맑음",
                tempLabel,
                "날씨가 좋아요! 가벼운 산책이나 야외 러닝을 추천해요.",
                "산책 · 조깅 · 공원 운동",
                "☀️");
        }

        return new WeatherRecommendation(
            "흐림",
            tempLabel,
            "실내·실외 모두 가능해요. 가벼운 산책 또는 실내 운동을 골라보세요.",
            "산책 · 실내 근력 · 스트레칭",
            "⛅");
    }

    private static WeatherRecommendation Fallback(string message) =>
        new("알 수 없음", "-", message, "실내 스트레칭 · 홈트", "🌤️");
}
