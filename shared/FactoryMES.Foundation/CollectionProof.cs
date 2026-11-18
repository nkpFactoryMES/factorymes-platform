using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace FactoryMES.Foundation;

// 측정값 하나를 수집 증명과 대조할 때 쓰는 문맥(대상·수집 인스턴스·설정 버전/해시·측정·수신 시각).
public sealed record MeasurementContext(
    string TargetId,
    string InstanceId,
    long ConfigVersion,
    string ConfigHash,
    DateTimeOffset MeasuredAt,
    DateTimeOffset ReceivedAt
);

// 수집 증명(collection proof): Backend가 수집 임대(lease) 때 발급한 짧은 RS256 JWT.
// Connector가 측정 배치에 붙여 보내면, 받는 쪽이 "이 대상·이 인스턴스·이 설정 버전으로
// 유효 시간 안에 측정했다"는 것을 확인한다. 맞지 않으면 SecurityTokenValidationException을 던진다.
// 호출한 쪽이 거절 로그를 남긴다(증명 문자열은 로그에 남기지 않는다).
public static class CollectionProof
{
    // A collection proof is not a service authentication token. Keep lifetime validation enabled.
    public static void Validate(
        string proof,
        RSA publicKey,
        MeasurementContext context
    )
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicKey)
            {
                // The caller owns this short-lived RSA handle; providers must not retain it.
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            },
            ValidAlgorithms = [
                SecurityAlgorithms.RsaSha256
            ],
            ValidateIssuer = true,
            ValidIssuer = "mes-backend",
            ValidateAudience = true,
            ValidAudience = "mes-equipment-collection",
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator =
                (
                    nbf,
                    exp,
                    token,
                    validation
                ) => nbf.HasValue
                && exp.HasValue
                && exp > nbf
                && exp.Value - nbf.Value <= TimeSpan.FromSeconds(15)
                && context.MeasuredAt.UtcDateTime >= nbf.Value
                && context.MeasuredAt.UtcDateTime < exp.Value
                && context.ReceivedAt.UtcDateTime <= exp.Value.AddSeconds(60)
                && context.MeasuredAt <= context.ReceivedAt.AddSeconds(2)
        };
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
        var principal = handler.ValidateToken(
            proof,
            parameters,
            out var token
        );
        string? Claim(string name) => principal.FindFirst(name)?.Value;
        if (Claim("target_id") != context.TargetId
            || Claim("instance_id") != context.InstanceId
            || Claim("config_version") != context.ConfigVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
            || Claim("config_hash") != context.ConfigHash
            || Claim("scope") != "telemetry:collect"
            || !long.TryParse(
            Claim("iat"),
            out var issuedAt
        )
            || DateTimeOffset.FromUnixTimeSeconds(issuedAt)
                .UtcDateTime > token.ValidFrom
            || token.ValidFrom - DateTimeOffset.FromUnixTimeSeconds(issuedAt)
                .UtcDateTime > TimeSpan.FromSeconds(2))
            throw new SecurityTokenValidationException("Collection proof binding rejected.");
    }
}
