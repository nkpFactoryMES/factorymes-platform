using System.Globalization;

namespace FactoryMES.Contracts;

// 로컬 추론 주소·모델을 고정해 사용자 설정으로 임의 서버나 클라우드를 호출하지 못하게 한다.
public static class LocalAiPolicy
{
    public const string Model = "qwen2.5:1.5b";
    public const string Endpoint = "http://local-ai:11434/api/generate";
    public const long TimeoutMs = 120000;

    public static bool IsAllowed(AiSettings settings) =>
        settings.CredentialRef is null && (settings.ProviderKind switch
        {
            "MOCK" => settings.ApprovedEndpoint is null && settings.Model == "mock-trend-v1",
            "LOCAL" => settings.ApprovedEndpoint == Endpoint
                && settings.Model == Model && settings.TimeoutMs == TimeoutMs,
            _ => false
        });
}

public sealed record MaintenanceAssessment(
    string PolicyVersion,
    string Risk,
    string[] Findings,
    string[] Actions
);

// Oracle에서 확정한 집계 입력을 받아 시연용 상태 점검 근거를 계산한다.
// 학습 모델의 고장 확률·잔여 수명 예측이 아니며 실제 설비의 정비 기준으로 사용하지 않는다.
public static class MaintenanceRules
{
    public static MaintenanceAssessment Evaluate(AnalysisInput input)
    {
        var temperature = input.Metrics.Where(x => x.MetricCode == "TEMPERATURE"
            && x.Unit is "C" or "°C" or "℃").ToArray();
        var findings = new List<string>
        {
            $"전체 관측 비율 {input.Coverage}, 표본 {input.SampleCount}건.",
            $"기간 중 정지 {input.DowntimeSeconds}초, 경보 {input.AlarmCount}건."
        };
        var risk = "UNKNOWN";
        var action = "데이터 수집 상태와 지표 단위를 확인한 뒤 담당자가 다시 검토하세요.";
        if (decimal.TryParse(
            input.Coverage,
            CultureInfo.InvariantCulture,
            out var coverage
        )
            && coverage >= 0.5m && input.SampleCount >= 2 && temperature.Length == 1
            && temperature[0].Observation is { } observed && observed.SampleCount >= 2
            && decimal.TryParse(
                observed.Coverage,
                CultureInfo.InvariantCulture,
                out var metricCoverage
            )
            && metricCoverage >= 0.5m
            && observed.LastSampledAt.Kind == DateTimeKind.Utc
            && observed.LastSampledAt >= input.PeriodEnd.AddMinutes(-2)
            && observed.LastSampledAt < input.PeriodEnd
            && decimal.TryParse(
                temperature[0].Last,
                CultureInfo.InvariantCulture,
                out var last
            ))
        {
            risk = last >= 35m ? "HIGH" : last >= 30m ? "MEDIUM" : "LOW";
            findings.Add($"온도 관측 비율 {observed.Coverage}, 표본 {observed.SampleCount}건.");
            findings.Add($"마지막 온도 {temperature[0].Last}°C. 시연 기준 주의30/높음35°C.");
            action = risk is "HIGH" or "MEDIUM"
                ? "담당자가 온도 센서·냉각 계통과 현장 운전 조건을 점검하세요."
                : "현장 점검 주기를 유지하고 온도 변화를 관찰하세요.";
        }
        else
        {
            findings.Add("표본·관측 비율 또는 유일한 섭씨 온도 근거가 부족해 판단을 보류했습니다.");
        }

        return new MaintenanceAssessment(
            "demo-temperature-v1",
            risk,
            findings.ToArray(),
            new[] {
                action,
                "AI 설명은 비신뢰 참고 자료이며 실제 조치는 담당자가 결정합니다."
            }
        );
    }

    public static bool Matches(AnalysisInput input, MaintenanceAssessment? assessment) =>
        assessment is not null
        && CollectionFormat.Hash(Evaluate(input)) == CollectionFormat.Hash(assessment);
}




