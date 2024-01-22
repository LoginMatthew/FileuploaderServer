using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUploaderServer.Models
{
    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string? Password { get; set; }
        public string? RefreshToken { get; set; }
        public string? Roles { get; set; }
        public string Salt { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }    
}
