using System.ComponentModel.DataAnnotations;

namespace FileUploaderServer.Models
{
    public class RegistNewUserModel
    {
        [Required(ErrorMessage = "User is required.")]
        public string userName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string roles { get; set; }
        public string password { get; set; }
    }    
}
