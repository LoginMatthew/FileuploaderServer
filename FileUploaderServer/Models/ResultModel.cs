
namespace FileUploaderServer.Models
{
    public class ResultModel<T> where T : class
    {
        public SummaryModel summary { get; set; }
        public IEnumerable<T> listOfData { get; set; }
    }
}
