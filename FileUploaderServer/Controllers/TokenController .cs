using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileUploaderServer.Context;
using FileUploaderServer.Models;
using Microsoft.AspNetCore.Authorization;
using FileUploaderServer.Services;

namespace UploadFilesServer.Controllers
{
[Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly UserDbContext _userContext;
        private readonly ITokenService _tokenService;

        public TokenController(UserDbContext loginContext, ITokenService tokenService)
        {
            _userContext = loginContext ?? throw new ArgumentNullException(nameof(loginContext));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh(TokenApiModel tokenApiModel)
        {
            if (tokenApiModel is null)
                return BadRequest("Invalid client request");

            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;

            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default

            var user = await _userContext.Users.SingleOrDefaultAsync(u => u.UserName == username);

            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest("Invalid client request");

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;

            _userContext.SaveChanges();

            return Ok(new AuthResponseModel()
            {
                token = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [HttpPost, Authorize]
        [Route("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var username = User.Identity.Name;
            var user = await _userContext.Users.SingleOrDefaultAsync(u => u.UserName == username);

            if (user == null)
                return BadRequest();

            user.RefreshToken = null;
            _userContext.SaveChanges();

            return NoContent();
        }
    }
}
