using System.ComponentModel.DataAnnotations;

namespace FileUploaderServer.Models
{
    public class DataFile
    {
        [Key]
        public Guid id { get; set; }

        [Required]
        public string? name { get; set; }

        [Required]
        public string? description { get; set; }
        public string? filePath { get; set; }
        public string? uploader { get; set; }
        public DateTime? creationDate { get; set; }
        public string? fileType { get; set; }
        public bool isPrivate { get; set; }

    }
}
