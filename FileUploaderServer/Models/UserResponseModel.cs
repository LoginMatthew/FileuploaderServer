using System.ComponentModel.DataAnnotations;

namespace FileUploaderServer.Models
{
    public class UserResponseModel
    {
        [Required(ErrorMessage = "User is required.")]
        public string? userName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string? roles { get; set; }
        public long id { get; set; }
        public string newPassword { get; set; }
        public long? updateDoneByUserID { get; set; }
    }    
}
