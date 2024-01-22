
using FileUploaderServer.Models;
using System.Security.Claims;

namespace FileUploaderServer.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        List<Claim> GetClaims(UserModel user);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
