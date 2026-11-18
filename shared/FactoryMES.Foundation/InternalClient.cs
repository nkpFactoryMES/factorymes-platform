using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FactoryMES.Foundation;

// Service authentication is separate from the short-lived measurement authorization.
// 서비스 간 내부 HTTP 호출기. Backend에서 받은 대상(audience)별 Service JWT를 4분간 재사용하고,
// 요청마다 Bearer로 붙여 보낸다. 실패 응답은 예외(EnsureSuccessStatusCode)로 호출한 쪽에 넘긴다.
// 로그: 대상·경로(쿼리 제외)·상태·소요 시간만 남긴다. 토큰·client secret·본문은 남기지 않는다.
public sealed class InternalClient(
    IHttpClientFactory factory,
    string clientId,
    ILogger<InternalClient> logger
)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, (string Token, DateTime Expires)> tokens = [];

    public static void Register(IServiceCollection services, string clientId)
    {
        services.AddHttpClient("mes-internal", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        services.AddSingleton(provider => new InternalClient(
            provider.GetRequiredService<IHttpClientFactory>(),
            clientId,
            provider.GetRequiredService<ILogger<InternalClient>>()
        ));
    }

    public static string Address(string audience) => audience switch
    {
        "mes-backend" => "http://mes-backend:8080",
        "integration-gateway" => "http://integration-gateway:8080",
        "equipment-connector" => "http://equipment-connector:8080",
        "ai-connector" => "http://ai-connector:8080",
        _ => throw new ArgumentException("Unknown internal audience")
    };

    // 대상 서비스용 Service JWT. 캐시가 살아 있으면 재사용하고, 없으면 Backend 토큰 발급을 부른다.
    private async Task<string> TokenAsync(string audience, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (tokens.TryGetValue(audience, out var cached) && cached.Expires > DateTime.UtcNow)
                return cached.Token;
            using var client = factory.CreateClient("mes-internal");
            using var response = await client.PostAsJsonAsync(
                Address("mes-backend") + "/internal/v1/auth/token",
                new
                {
                    clientId,
                    clientSecret = SecretFiles.Read("/run/secrets/" + clientId + "_client_secret"),
                    audience
                },
                ct
            );
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Service token for {Audience} refused: HTTP {Status} (client {ClientId})",
                    audience,
                    (int)response.StatusCode,
                    clientId
                );
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var token = body.GetProperty("accessToken").GetString()!;
            tokens[audience] = (token, DateTime.UtcNow.AddSeconds(240));
            logger.LogDebug(
                "Service token for {Audience} issued to {ClientId}; cached 240 s",
                audience,
                clientId
            );
            return token;
        }
        finally { gate.Release(); }
    }

    // body가 없으면 GET, 있으면 POST(put이면 PUT). 2xx가 아니면 HttpRequestException을 던진다.
    public async Task<T> SendAsync<T>(
        string audience,
        string path,
        object? body,
        CancellationToken ct,
        bool put = false,
        TimeSpan? requestTimeout = null
    )
    {
        using var client = factory.CreateClient("mes-internal");
        // 장시간 GPU 분석 호출만 명시적으로 예산을 늘리고 인증 발급과 일반 호출은 5초를 유지한다.
        if (requestTimeout is not null)
        {
            client.Timeout = requestTimeout.Value;
        }
        using var request = new HttpRequestMessage(
            body is null ? HttpMethod.Get : put ? HttpMethod.Put : HttpMethod.Post,
            Address(audience) + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await TokenAsync(audience, ct));
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType());
        var started = Stopwatch.GetTimestamp();
        var route = path.Split('?')[0];
        using var response = await client.SendAsync(request, ct);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug(
                "Internal {Method} {Audience}{Path} -> {Status} in {ElapsedMs:0} ms",
                request.Method,
                audience,
                route,
                (int)response.StatusCode,
                elapsed
            );
        }
        else
        {
            logger.LogWarning(
                "Internal {Method} {Audience}{Path} failed: HTTP {Status} in {ElapsedMs:0} ms",
                request.Method,
                audience,
                route,
                (int)response.StatusCode,
                elapsed
            );
        }

        response.EnsureSuccessStatusCode();
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return default!;
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}




