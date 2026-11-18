using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FactoryMES.Foundation;

// 모든 FactoryMES 서비스(Backend·Gateway·Connector·Jobs)가 공통으로 쓰는 시작 구성.
// - 서비스 간 인증(Service JWT) 검증, 진단 권한, Problem 응답 형식, JSON 엄격 해석
// - 로컬/Mock 실행만 허용하는 비용 정책 확인
// - 요청 경계 로그(경로·상태·correlationId·소요 시간)와 공통 오류 응답
public static class ServiceFoundation
{
    public const string ServiceScheme = "ServiceJwt";

    // 서비스 시작 시 한 번 부른다. 공개키 파일로 다른 서비스가 보낸 Service JWT를 검증한다.
    public static void AddLocalFoundation(this WebApplicationBuilder builder, string audience)
    {
        builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
        CostPolicy.EnsureLocalOnly(builder.Configuration);
        var publicKey =
            SecretFiles.Read(
            builder.Configuration["Security:PublicKeyFile"]
            ?? "/run/secrets/service_public_key"
        );
        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKey);
        builder.Services.AddSingleton(rsa);
        builder.Services.AddAuthentication(ServiceScheme)
            .AddJwtBearer(ServiceScheme, options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidAlgorithms = [
                    SecurityAlgorithms.RsaSha256
                ],
                ValidateIssuer = true,
                ValidIssuer = "factorymes-backend",
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(2),
                NameClaimType = "client_id"
            };
        });
        builder.Services.AddAuthorization(
            options => options.AddPolicy(
                "ServiceDiagnostics",
                policy => policy.AddAuthenticationSchemes(ServiceScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim("scope", "diagnostics:read")
            )
        );
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            // The public Problem contract uses correlationId, not an extra traceId property.
            context.ProblemDetails.Extensions.Remove("traceId");
            context.ProblemDetails.Extensions["correlationId"] =
                context.HttpContext.TraceIdentifier;
            if (!context.ProblemDetails.Extensions.ContainsKey("code"))
                context.ProblemDetails.Extensions["code"] = "RequestFailed";
        });
        builder.Services.ConfigureHttpJsonOptions(
            options => options.SerializerOptions.UnmappedMemberHandling =
            System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        );
        builder.Services.AddHealthChecks();
    }

    // 컨테이너 상태 확인(/health/live)과 서비스 진단(/internal/v1/status) 엔드포인트.
    public static void MapServiceBasics(this WebApplication app, string name)
    {
        app.MapGet("/health/live", () => Results.Ok(new
        {
            status = "live",
            service = name
        }));
        app.MapGet(
            "/internal/v1/status",
            () => Results.Ok(new
        {
            service = name,
            stage = "skeleton",
            cloudAiEnabled = false,
            externalDeliveryEnabled = false
        })
        )
            .RequireAuthorization("ServiceDiagnostics");
    }

    // 요청 파이프라인 맨 앞에서 부른다.
    // 1) 요청 경계 로그 2) 처리되지 않은 예외 → 500 Problem 3) 본문 없는 4xx/5xx → Problem.
    public static void UseMesErrors(this WebApplication app)
    {
        UseRequestLog(app);

        // A request the framework could not read (bad JSON, unknown field) is a client error.
        // Keep its 400 status instead of turning it into a 500 server fault.
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
                ? badRequest.StatusCode
                : StatusCodes.Status500InternalServerError
        });
        app.UseStatusCodePages(async status =>
        {
            var code = status.HttpContext.Response.StatusCode;
            await Results.Problem(
                statusCode: code,
                title: code == 401
                ? "Authentication required"
                : code == 403
                ? "Access denied"
                : "Request rejected",
                extensions: new Dictionary<string, object?>
            {
                ["code"] =
                code == 401
                ? "Unauthenticated"
                : code == 403
                ? "Forbidden"
                : "RequestFailed"
            }
            )
                .ExecuteAsync(status.HttpContext);
        });
    }

    // 요청 하나가 끝날 때 한 줄을 남긴다.
    // 남기는 것: 메서드, 경로(쿼리 제외), 상태 코드, correlationId, 소요 시간, 호출 서비스(client_id).
    // 남기지 않는 것: 쿼리 문자열, 헤더(토큰·쿠키·CSRF), 요청·응답 본문, 사용자 개인정보.
    // 등급: 4xx·5xx는 Warning(거절·실패), 정상 응답은 Debug, 상태 확인(/health)은 항상 Debug.
    // 예외 자체의 Error 로그는 ASP.NET 예외 처리기가 그대로 남긴다(여기서 낮추지 않는다).
    private static void UseRequestLog(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("FactoryMES.Foundation.RequestLog");
        app.Use(async (context, next) =>
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await next(context);
            }
            finally
            {
                var status = context.Response.StatusCode;
                var path = context.Request.Path.Value ?? "/";
                var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var caller = context.User.FindFirst("client_id")?.Value ?? "-";
                var level = path.StartsWith("/health/", StringComparison.Ordinal)
                    ? LogLevel.Debug
                    : status >= 400
                        ? LogLevel.Warning
                        : LogLevel.Debug;
                logger.Log(
                    level,
                    "HTTP {Method} {Path} -> {Status} in {ElapsedMs:0} ms "
                        + "(correlationId {CorrelationId}, caller {Caller})",
                    context.Request.Method,
                    path,
                    status,
                    elapsed,
                    context.TraceIdentifier,
                    caller
                );
            }
        });
    }
}

// 비밀 파일(/run/secrets/*)을 읽는다. 값은 호출한 쪽에서만 쓰고 로그에 남기지 않는다.
public static class SecretFiles
{
    public static string Read(string path) => File.ReadAllText(path)
        .Trim();
}

// 유료 클라우드 AI와 외부 실발송이 설정으로 켜져 있으면 서비스를 시작하지 않는다.
public static class CostPolicy
{
    public static void EnsureLocalOnly(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("ExecutionPolicy:CloudAiEnabled")
            || configuration.GetValue<bool>("ExecutionPolicy:ExternalDeliveryEnabled"))
            throw new InvalidOperationException(
                "This scaffold permits local/Mock execution only. Cloud AI and external delivery must remain disabled."
            );
    }
}

// 컨테이너 HEALTHCHECK가 "--healthcheck <loopback URL>"로 실행할 때만 동작하는 자기 점검.
// 서비스를 띄우지 않고 응답 코드로 종료 코드(0/1)를 정한다.
public static class HealthProbe
{
    public static async Task<bool> TryRunAsync(string[] args)
    {
        if (args.Length != 2 || args[0] != "--healthcheck")
            return false;
        var uri = new Uri(args[1]);
        if (!uri.IsLoopback)
            throw new InvalidOperationException("Health probe is loopback only.");
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            (
                request,
                certificate,
                chain,
                errors
            ) => request.RequestUri?.IsLoopback == true; // Only the explicit local container health probe.
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(4)
        };
        try
        {
            using var response = await client.GetAsync(uri);
            Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (HttpRequestException)
        {
            Environment.ExitCode = 1;
        }
        catch (TaskCanceledException)
        {
            Environment.ExitCode = 1;
        }

        return true;
    }
}
