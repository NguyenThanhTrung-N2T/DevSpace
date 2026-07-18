using Auth.Application.Common.Models;
using System.Security.Claims;

namespace Auth.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtService
{
    AccessTokenResult GenerateAccessToken(UserInfo user);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    System.Security.Cryptography.RSA GetPublicKey();
    string GetJwksJson();
}
