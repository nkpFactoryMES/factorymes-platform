using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryMES.Foundation;

// 브라우저 사용자 세션 쿠키(mes.session) 구성. HttpOnly·Secure·SameSite=Lax, 30분 고정 만료.
// API 요청이므로 로그인 페이지로 돌리지 않고 401/403 상태만 돌려준다.
// Data Protection 키는 공유 폴더에 두어 Web·Backend가 같은 쿠키를 해석한다.
public static class SessionCookies
{
    public const string Scheme = "MesCookie";

    public static AuthenticationBuilder AddMesCookies(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CookieAuthenticationOptions>? configure =
        null
    )
    {
        services.AddDataProtection()
            .SetApplicationName("FactoryMES.Sessions.v1")
            .PersistKeysToFileSystem(
            new DirectoryInfo(configuration["Security:DataProtectionPath"]
            ?? "/var/factorymes/keys")
        );
        return services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Scheme;
            options.DefaultChallengeScheme = Scheme;
            options.DefaultSignInScheme = Scheme;
        })
            .AddCookie(Scheme, options =>
        {
            options.Cookie.Name = "mes.session";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
            configure?.Invoke(options);
        });
    }
}
