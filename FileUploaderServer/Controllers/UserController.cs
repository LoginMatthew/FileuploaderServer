using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileUploaderServer.Context;
using FileUploaderServer.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using FileUploaderServer.Services;

namespace UploadFilesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly ITokenService _tokenService;

        public UserController(UserDbContext loginContext, ITokenService tokenService)
        {
            _context = loginContext ?? throw new ArgumentNullException(nameof(loginContext));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        [AllowAnonymous]
        [EnableCors("CorsPolicy")]
        [HttpPost, Route("Login")]
        public async Task<IActionResult> Login([FromBody] string loginData)
        {
            if (loginData == string.Empty || loginData is null)
                return BadRequest("Invalid client request");

            UserLoginModel userLogin = await EncryptionDecrytionService.DeryptDataFromByteArray<UserLoginModel>(loginData);

            try
            {
                var findUserWithName = await _context.Users.FirstOrDefaultAsync(x => x.UserName == userLogin.UserName);

                var user = findUserWithName != null && HashingPassword.VerifyPassword(userLogin.Password, findUserWithName.Password, Convert.FromHexString(findUserWithName.Salt)) ? findUserWithName : null;

                if (user is null)
                    return Unauthorized(new AuthResponseModel { errorMessage = "Invalid Authentication" });

                var accessToken = _tokenService.GenerateAccessToken(_tokenService.GetClaims(user));
                var refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(1);

                _context.SaveChanges();

                var response = await EncryptionDecrytionService.EncryptDataToString<AuthResponseModel>(new AuthResponseModel
                {
                    token = accessToken,
                    refreshToken = refreshToken,
                    role = user.Roles.Contains("Administrator") ? "Administrator" : user.Roles.Contains("Guest") ? "Guest" : "User",
                    id = user.Id,
                    expireTimeInMinutes = 3
                });

                return Ok(new { response });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost, Route("Registration")]
        [EnableCors("CorsPolicy")]
        public async Task<IActionResult> RegisterUser([FromBody] string data)
        {
            RegistNewUserModel decryptedData = await EncryptionDecrytionService.DeryptDataFromByteArray<RegistNewUserModel>(data);

            try
            {
                if (decryptedData.userName is null || decryptedData.password is null || decryptedData.roles is null)
                {
                    return BadRequest("Data object is null");
                }

                UserModel newUser = new UserModel();
                newUser.UserName = decryptedData.userName;
                newUser.Password = decryptedData.password;
                newUser.Roles = decryptedData.roles;

                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid model object");
                }

                var userExist = await _context.Users.Where(x => x.UserName == newUser.UserName).FirstOrDefaultAsync();

                if (userExist != null)
                {
                    return StatusCode(409, "User name already exist!");
                }

                newUser.Id = _context.Users.Max(x => x.Id) + 1;
                newUser.Password = HashingPassword.HashPasword(newUser.Password, out var salt);
                newUser.Salt = Convert.ToHexString(salt);

                _context.Add(newUser);
                await _context.SaveChangesAsync();

                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpDelete("Delete"), Authorize]
        [EnableCors("CorsPolicy")]
        public async Task<IActionResult> DeleteUser([FromBody] string data)
        {
            UserDeleteModel decryptedData = await EncryptionDecrytionService.DeryptDataFromByteArray<UserDeleteModel>(data);

            try
            {
                if (decryptedData == null && (decryptedData.deleteDoneByUserID == null || decryptedData.deleteDoneByUserID == 0))
                {
                    return NotFound(new
                    {
                        StatusCode = 404,
                        Message = "Not found user(s)"
                    });
                }

                var userWhoDoDeletion = await _context.Users.FindAsync(decryptedData.deleteDoneByUserID);
                var user = await _context.Users.FindAsync(decryptedData.userId);

                if (user.Id == userWhoDoDeletion.Id)
                {
                    return BadRequest(new
                    {
                        Message = "Cannot delete yourself!"
                    });
                }
                if (!userWhoDoDeletion.Roles.Contains("Administrator"))
                    return BadRequest(new
                    {
                        Message = "Not have rights to modify the selected user!"
                    });

                var checkStatusResult = await CheckUserRightsForDeletion(user, userWhoDoDeletion);
                if (checkStatusResult == null)
                {
                    _context.Remove(user);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        StatusCode = 200,
                        Message = "Deleted Data"
                    });
                }
                else
                {
                    Console.WriteLine(checkStatusResult);
                    return checkStatusResult;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    
        [EnableCors("CorsPolicy")]
        [HttpPost, Route("Update"), Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] string data)
        {
            UserResponseModel decryptedData = await EncryptionDecrytionService.DeryptDataFromByteArray<UserResponseModel>(data);

            try
            {

                if (decryptedData == null && (decryptedData.updateDoneByUserID == null || decryptedData.updateDoneByUserID == 0))
                {
                    return NotFound(new
                    {
                        StatusCode = 404,
                        Message = "Not found user(s)"
                    });
                }

                var userWhoDoUpdate = await _context.Users.FindAsync(decryptedData.updateDoneByUserID);

                if (decryptedData.id == decryptedData.updateDoneByUserID && decryptedData.roles.ToLower() != userWhoDoUpdate.Roles.ToLower())
                {
                    return BadRequest(new
                    {
                        Message = "Not have rights to modify yourself!"
                    });
                }

                var oldUser = await _context.Users.FindAsync(decryptedData.id);
                var checkStatusResult = await CheckUserRightsForUpdate(oldUser, userWhoDoUpdate);

                if (checkStatusResult == null)
                {
                    UserModel userOld = await _context.Users.FindAsync(decryptedData.id);

                    if(decryptedData.newPassword != string.Empty)
                    {
                        userOld.Password = HashingPassword.HashPasword(decryptedData.newPassword, out var salt);
                        userOld.Salt = Convert.ToHexString(salt);
                    }

                    userOld.Roles = decryptedData.roles;

                    _context.Update(userOld);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                else
                {
                    return checkStatusResult;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    

        [HttpPost, Route("GetUsers"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> GetDataFilesPaganationWithFilterTypeAndName([FromBody] string data)
        {
            DataPaginationRequest decryptedData = await EncryptionDecrytionService.DeryptDataFromByteArray<DataPaginationRequest>(data);

            try
            {
                if (decryptedData is null)
                    return BadRequest("Data object is null");

                IEnumerable<UserModel> users = await FilterUserAccordingToUserRights(decryptedData);
                
                if (decryptedData.isDescendingOrder)
                {
                    users = users.OrderByDescending(x => x.UserName);
                }
                else
                {
                    users = users.OrderBy(x => x.UserName);
                }

                if (users != null)
                {
                    List<UserResponseModel> userList = new List<UserResponseModel>();
                    foreach (var item in await RequestedDataPerPage(decryptedData, users) as IEnumerable<UserModel>)
                    {
                        userList.Add(new UserResponseModel()
                        {
                            id = item.Id,
                            userName = item.UserName,
                            roles = item.Roles
                        });
                    }

                    var response = await EncryptionDecrytionService.EncryptDataToString<ResultModel<UserResponseModel>>(new ResultModel<UserResponseModel>()
                    {
                        summary = new SummaryModel()
                        {
                            totalCount = users.Count(),
                            totalPages = (int)Math.Ceiling((decimal)users.Count() / decryptedData.pageSize),
                            acutalPage = decryptedData.page
                        },
                        listOfData = userList
                    });

                    return Ok(new { response });
                }
                else
                {
                    return StatusCode(400, "No Data");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        /// <summary>
        /// Show only those users that the user can view based on his/her rights!
        /// </summary>
        private async Task<IEnumerable<UserModel>> FilterUserAccordingToUserRights(DataPaginationRequest decryptedData)
        {
            IEnumerable<UserModel> users = null;

            if (decryptedData.isAdmin)
            {
                if (decryptedData.selectedType.ToLower() == "all")
                {
                    users = await _context.Users.Where(x => x.UserName.ToLower().Contains(decryptedData.filterNameSearch.ToLower().Trim())).ToArrayAsync();
                }
                if (decryptedData.selectedType.ToLower() != "all")
                {
                    users = await _context.Users.Where(x => x.Roles.Contains(decryptedData.selectedType) && x.UserName.ToLower().Contains(decryptedData.filterNameSearch.ToLower().Trim())).ToArrayAsync();
                }
            }
            else
            {
                if (decryptedData.selectedType.ToLower() == "all")
                {
                    users = await _context.Users.Where(x => !x.Roles.Contains("Administrator") && x.UserName.ToLower().Contains(decryptedData.filterNameSearch.ToLower().Trim())).ToArrayAsync();
                }
                if (decryptedData.selectedType.ToLower() != "all")
                {
                    users = await _context.Users.Where(x => !x.Roles.Contains("Administrator") && x.Roles.Contains(decryptedData.selectedType) && x.UserName.ToLower().Contains(decryptedData.filterNameSearch.ToLower().Trim())).ToArrayAsync();
                }
            }

            return users;
        }

        /// <summary>
        /// Calculate page number data and take the required page data!
        /// </summary>
        private async Task<IEnumerable<UserModel>> RequestedDataPerPage(DataPaginationRequest decryptedData, IEnumerable<UserModel> users)
        {
            int totalCount = users.Count();
            int takeNumber = decryptedData.pageSize;
            int skipNumber = decryptedData.page < 1 ? 0 : decryptedData.page * decryptedData.pageSize;

            if ((totalCount - ((decryptedData.page - 1) * decryptedData.pageSize)) < decryptedData.pageSize)
                takeNumber = (totalCount - (decryptedData.page - 1) * decryptedData.pageSize);
            
            return users.Skip(skipNumber).Take(takeNumber);
        }

        /// <summary>
        /// Check whether the given user is able to delete the selected user based on his/her rights.
        /// </summary>
        private async Task<IActionResult> CheckUserRightsForDeletion(UserModel user, UserModel userWhoDoDeletion)
        {
            if (user.Roles.Contains("User") && !userWhoDoDeletion.Roles.Contains("Administrator"))
            {
                return Unauthorized(new
                {
                    StatusCode = 401,
                    Message = "Not have rights to delete the selected user!"
                });
            }
            else if (user.Roles.Contains("Guest") && (!userWhoDoDeletion.Roles.Contains("User") || !userWhoDoDeletion.Roles.Contains("Administrator")))
            {
                return Unauthorized(new
                {
                    StatusCode = 401,
                    Message = "Not have rights to delete the selected user!"
                });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Check whether the given user is able to update the selected user based on his/her rights.
        /// </summary>
        private async Task<IActionResult> CheckUserRightsForUpdate(UserModel user, UserModel userWhoDoModify)
        {
            if (user == null && userWhoDoModify == null)
            {
                return NotFound(new
                {
                    Message = "Not found user(s)"
                });
            }

            //Update User and Guest rights by other user!
            else if (user.Roles.Contains("Guest") && userWhoDoModify.Roles.Contains("User"))
            {
                return null;
            }
            else if ((user.Roles.Contains("Guest") || user.Roles.Contains("User") || user.Roles.Contains("Administrator")) && userWhoDoModify.Roles.Contains("Administrator"))
            {
                return null;
            }
            else
            {
                return BadRequest(new
                {
                    Message = "Not have rights to modify the selected user!"
                });
            }
        }
    }
}
