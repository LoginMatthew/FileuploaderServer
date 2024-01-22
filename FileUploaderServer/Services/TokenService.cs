
using FileUploaderServer.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FileUploaderServer.Services
{
    public class TokenService : ITokenService
    {
        /// <summary>
        /// This generates the access token to the given cilent after credential are recieved.
        /// </summary>
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {           
            var tokeOptions = new JwtSecurityToken(
                issuer: GlobalValues.httpAddress,
                audience: GlobalValues.httpAddress,
                claims: claims,
                expires: DateTime.Now.AddMinutes(3),
                signingCredentials: GenerateSigningCredentials()
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }
        
        public List<Claim> GetClaims(UserModel user) => new List<Claim>            
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Roles)
        };      

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, //Here validate the audience and issuer depending on our use case
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GlobalValues.secretKey)),
                ValidateLifetime = false //Don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private SigningCredentials GenerateSigningCredentials()
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GlobalValues.secretKey));
            return new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        }
    }
}
