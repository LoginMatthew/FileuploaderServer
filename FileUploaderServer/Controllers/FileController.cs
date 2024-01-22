using FileUploaderServer.Context;
using FileUploaderServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace FileUploaderServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly DataFileDbContext _context;
        public FileController(DataFileDbContext context) => _context = context;

        /// <summary>
        /// Single file uploader (unlimited size)
        /// </summary>
        [Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        [HttpPost, Route("Singleupload"), Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(string uploaderName, bool isPrivate)
        {
            try
            {
                var formCollection = await Request.ReadFormAsync();
                var file = formCollection.Files.First();
                var folderName = CheckAndCreateFolder(uploaderName, isPrivate);

                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);//file will be stored here!
                if (file.Length > 0)
                {
                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.ToString().Trim('"');
                    var fullPath = Path.Combine(pathToSave, fileName);
                    var dbPath = Path.Combine(folderName, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    return StatusCode(200, new { dbPath });
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        /// <summary>
        /// Multiple file uploader  (unlimited size)
        /// </summary>
        [HttpPost, Route("Multipleupload"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MultipleUpload(string uploaderName, bool isPrivate)
        {
            string result = "";
            try
            {
                var formCollection = await Request.ReadFormAsync();
                List<IFormFile> files = formCollection.Files.ToList();

                foreach (IFormFile item in files)
                {
                    var folderName = CheckAndCreateFolder(uploaderName, isPrivate);
                    result = await SaveFile(item, folderName);

                    DataFile newData = new DataFile();
                    newData.name = item.FileName.ToString().Trim('"');
                    newData.creationDate = DateTime.Now;
                    newData.uploader = uploaderName;
                    newData.isPrivate = isPrivate;
                    newData.id = Guid.NewGuid();
                    newData.fileType = item.ContentType.ToString();
                    newData.description = item.ContentType.ToString();
                                        
                    var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                    var dbPath = Path.Combine(folderName, item.FileName.ToString().Trim('"'));
                    newData.filePath = dbPath;

                    _context.Add(newData);
                    await _context.SaveChangesAsync();

                    if (result == "")
                    {
                        return BadRequest();
                    }                
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }

            return result == null ? BadRequest() : Ok(new { });
        }

        [EnableCors("CorsPolicy")]
        [HttpGet, Route("DownloadFile"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName, string uploaderName, bool isPrivate)
        {
            var folderName = Path.Combine(uploaderName, isPrivate ?  Path.Combine("Private", fileName)  : Path.Combine("Public", fileName));
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Files", folderName);
            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "applicaiton/octet-stream";
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var result = File(bytes, contentType, Path.GetFileName(filePath));
            return result;
        }

        [HttpDelete, Route("DeleteFile"), Authorize]
        [EnableCors("CorsPolicy")]
        public async Task<IActionResult> DeleteFile(string filePath)
        {
            try
            {
                if(!System.IO.File.Exists(filePath))
                {
                    return NotFound(new
                    {
                        StatusCode = 404,
                        Message = "File Not Exist on the server"
                    });
                }

                System.IO.File.Delete(filePath);

                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Deleted Data"
                });
            }
            catch (Exception)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Something went wrong on the server!!"
                });

                throw;
            }
        }

        /// <summary>
        /// Store the given file
        /// </summary>
        private async Task<string> SaveFile(IFormFile file, string folderName)
        {
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);//file will be stored here!
            if (file.Length > 0)
            {
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.ToString().Trim('"');
                var fullPath = Path.Combine(pathToSave, fileName);
                string dbPath = Path.Combine(folderName, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                return dbPath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Check 'Private' and 'Public' folders and create if not exist the wanted one.
        /// </summary>
        private string CheckAndCreateFolder(string uploaderName, bool isPrivate)
        {
            var folderUserName = Path.Combine("Files", isPrivate ? Path.Combine(uploaderName, "Private") : Path.Combine(uploaderName, "Public"));
            var folderName = Path.Combine("Resources", folderUserName);

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            return folderName;
        }
    }
}
