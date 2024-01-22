namespace FileUploaderServer
{
    public static class GlobalValues
    {
        public static string IPAddress = "192.168.1.1";//an example ip address to rewrite for use any devices
        //public static string IPAddress = "127.0.0.1";
        public static int portNumber = 5003;
        public static string httpAddress = "http://" + IPAddress + ":" + portNumber;
        public static string secretKey = "superSecretKey@123456";
    }
}