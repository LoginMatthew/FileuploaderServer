
namespace FileUploaderServer.Models
{
    public class DataPaginationRequest
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public string selectedType { get; set; }
        public bool isDescendingOrder { get; set; }
        public string filterNameSearch { get; set; }
        public bool isAdmin { get; set; }
        public string? uploader { get; set; }

    }    
}
