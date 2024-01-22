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
    public class DBDataFileController : ControllerBase
    {
        private readonly DataFileDbContext _context;
        public DBDataFileController(DataFileDbContext context) => _context = context;

        [HttpGet, Route("GetDataFiles"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> GetAllDataFiles()
        {
            try
            {
                var datafiles = await _context.DataFiles.ToListAsync();

                return Ok(datafiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpGet, Route("GetFilterTypeData"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> GetFilterTypeDataFiles([FromQuery] string fileType)
        {
            try
            {
                var datafiles = await _context.DataFiles.Where(x => x.fileType.Contains(fileType)).ToArrayAsync();

                return Ok(datafiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpGet, Route("GetFilterNameData"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> GetFilternameDataFiles([FromQuery] string fileName)
        {
            try
            {
                var datafiles = await _context.DataFiles.Where(x => x.name.Contains(fileName)).ToArrayAsync();

                return Ok(datafiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [EnableCors("CorsPolicy")]
        [HttpPost, Route("CreateDataFile")]
        public async Task<IActionResult> CreateDataFile([FromBody] string creationData)
        {
            try
            {
                DataFile datafile = null;

                try
                {
                    datafile = await EncryptionDecrytionService.DeryptDataFromByteArray<DataFile>(creationData);
                }
                catch (Exception)
                {

                    throw;
                }

                if (datafile is null)
                {
                    return BadRequest("Data object is null");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid model object");
                }

                datafile.id = Guid.NewGuid();
                _context.Add(datafile);

                await _context.SaveChangesAsync();

                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [EnableCors("CorsPolicy")]
        [HttpDelete, Route("Delete"), Authorize]
        public async Task<IActionResult> DeleteDataFile([FromBody] string idData)
        {
            object decryptedData = await EncryptionDecrytionService.DeryptDataFromByteArray<object>(idData);
            string id = decryptedData.ToString();
            var user = _context.DataFiles.Find(new Guid(id));

            if (user == null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Data file Not Found"
                });
            }
            else
            {
                _context.Remove(user);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Deleted Data"
                });
            }
        }

        [HttpPost, Route("Update"), Authorize]
        public async Task<IActionResult> UpdateDataFile([FromBody] string data)
        {
            DataFile datafile = await EncryptionDecrytionService.DeryptDataFromByteArray<DataFile>(data);

            if (datafile == null)
            {
                return BadRequest("Data object is null");
            }
            else
            {
                datafile = await isPrivateStatusChanged(datafile);
                _context.Update(datafile);
                await _context.SaveChangesAsync();
                return Ok();
            }
        }

        [HttpPost, Route("GetDataFilesPerPages"), Authorize]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
       public async Task<IActionResult> GetDataFilesPaganationWithFilterTypeAndName([FromBody] string data)
        {
            DataPaginationRequest datafile = await EncryptionDecrytionService.DeryptDataFromByteArray<DataPaginationRequest>(data);

            try
            {
                if(datafile is null)
                    return BadRequest("Data object is null");

                IEnumerable<DataFile> datafiles = await FilterDataFilesAccordingToUserRights(datafile);

                if (datafile.isDescendingOrder)
                {
                    datafiles = datafiles.OrderByDescending(x => x.name);
                }
                else
                {
                    datafiles = datafiles.OrderBy(x => x.name);
                }

                if (datafiles != null)
                {
                    var response = await EncryptionDecrytionService.EncryptDataToString<ResultModel<DataFile>>(new ResultModel<DataFile>()
                    {
                        summary = new SummaryModel()
                        {
                            totalCount = datafiles.Count(),
                            totalPages = (int)Math.Ceiling((decimal)datafiles.Count() / datafile.pageSize),
                            acutalPage = datafile.page
                        },
                        listOfData = await RequestedDataPerPage(datafile, datafiles)
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
        /// Show only those files that the user can view based on his/her rights!
        /// </summary>
        private async Task<IEnumerable<DataFile>> FilterDataFilesAccordingToUserRights(DataPaginationRequest datafile)
        {
            IEnumerable<DataFile> datafiles = null;

            if (datafile.isAdmin)
            {
                if (datafile.selectedType.ToLower() == "all")
                {
                    datafiles = await _context.DataFiles.Where(x => x.name.ToLower().Contains(datafile.filterNameSearch.ToLower())).ToArrayAsync();
                }
                if (datafile.selectedType.ToLower() != "all")
                {
                    datafiles = await _context.DataFiles.Where(x => x.fileType.Contains(datafile.selectedType) && x.name.ToLower().Contains(datafile.filterNameSearch.ToLower())).ToArrayAsync();
                }
            }
            else
            {
                if (datafile.selectedType.ToLower() == "all" && datafile.uploader != string.Empty)
                {
                    datafiles = await _context.DataFiles.Where(x => x.uploader == datafile.uploader && x.name.ToLower().Contains(datafile.filterNameSearch.ToLower())).ToArrayAsync();
                    IEnumerable<DataFile> datafiles2 = await _context.DataFiles.Where(x => x.uploader != datafile.uploader && !x.isPrivate).ToArrayAsync();
                    datafiles = datafiles.Union(datafiles2);
                }
                if (datafile.selectedType.ToLower() != "all" && datafile.uploader != string.Empty)
                {
                    datafiles = await _context.DataFiles.Where(x => x.fileType.Contains(datafile.selectedType) && x.uploader == datafile.uploader && x.name.ToLower().Contains(datafile.filterNameSearch.ToLower())).ToArrayAsync();
                    IEnumerable<DataFile> datafiles2 = await _context.DataFiles.Where(x => x.fileType.Contains(datafile.selectedType) && x.uploader != datafile.uploader && !x.isPrivate).ToArrayAsync();
                    datafiles = datafiles.Union(datafiles2);
                }
            }

            return datafiles;
        }

        /// <summary>
        /// Calculate page number data and take the required page data!
        /// </summary>
        private async Task<IEnumerable<DataFile>> RequestedDataPerPage(DataPaginationRequest datafile, IEnumerable<DataFile> datafiles)
        {
            int totalCount = datafiles.Count();
            int takeNumber = datafile.pageSize;
            int skipNumber = datafile.page < 1 ? 0 : datafile.page * datafile.pageSize;

            if ((totalCount - ((datafile.page - 1) * datafile.pageSize)) < datafile.pageSize)
                takeNumber = (totalCount - (datafile.page - 1) * datafile.pageSize);

            return datafiles.Skip(skipNumber).Take(takeNumber);
        }

        /// <summary>
        /// Check and modify file's folder
        /// </summary>
        private async Task<DataFile> isPrivateStatusChanged(DataFile datafile)
        {
            datafile.filePath = await MoveFile(datafile.uploader, datafile.filePath, datafile.filePath.Split('\\')[datafile.filePath.Split('\\').Length - 1], datafile.isPrivate);

            return datafile;
        }

        /// <summary>
        /// Move file to the appropriate folder
        /// </summary>
        private async Task<string> MoveFile(string uploaderName, string sourcePath, string fileName, bool isPrivate)
        {
            var folderUserName = Path.Combine("Files", isPrivate ? Path.Combine(uploaderName, "Private") : Path.Combine(uploaderName, "Public"));
            var folderName = Path.Combine("Resources", folderUserName);

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            folderName = Path.Combine(folderName, fileName);

            if (Path.Combine(folderUserName, fileName) != sourcePath)
            {
                try
                {
                    using (FileStream sourceStream = System.IO.File.Open(sourcePath, FileMode.Open))
                    {
                        using (FileStream destinationStream = System.IO.File.Create(folderName))
                        {
                            await sourceStream.CopyToAsync(destinationStream);
                            Console.WriteLine("File Moved");
                            sourceStream.Close();
                            System.IO.File.Delete(sourcePath);
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return folderName;
        }
    }
}
