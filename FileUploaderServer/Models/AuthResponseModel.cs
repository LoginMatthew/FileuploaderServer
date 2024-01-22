namespace FileUploaderServer.Models
{
    [Serializable]
    public class AuthResponseModel
    {
        public string? errorMessage { get; set; }
        public string? token { get; set; }
        public string? refreshToken { get; set; }
        public string? role { get; set; }
        public long? id { get; set; }
        public int? expireTimeInMinutes { get; set; }
    }
}
