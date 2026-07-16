using Auth.Domain.Entities;
using System.Security.Claims;

namespace Auth.Application.Common.Interfaces;

public interface IJwtService
{
    (string Token, string TokenId, DateTime ExpiresAt) GenerateAccessToken(User user, IList<string> roles);
    (string Token, Guid TokenId, Guid FamilyId) GenerateRefreshToken(string userId, Guid? familyId = null);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    System.Security.Cryptography.RSA GetPublicKey();
    string GetJwksJson();
}
