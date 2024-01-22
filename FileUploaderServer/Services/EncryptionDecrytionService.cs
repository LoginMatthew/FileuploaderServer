
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FileUploaderServer.Services
{
    public static class EncryptionDecrytionService
    {
        /// <summary>
        /// Encrypt data object
        /// </summary>
        /// <returns>encrypted data object</returns>
        public static async Task<string> EncryptDataToString<T>(T data)
        {
            byte[] plainText = await ToByteArray<T>(data);

            // Create an Rijndael object with key and IV.
            Rijndael rijAlg = GetRijndaelEncryptionAlgorithm();

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

            MemoryStream msEncrypt = new MemoryStream();

            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await csEncrypt.WriteAsync(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        /// <summary>
        /// Dercrypt data object
        /// </summary>
        /// <returns>Decrypted data object</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async  Task<T> DeryptDataFromByteArray<T>(string encryptedData)
        {
            try
            {
                if (encryptedData == string.Empty)
                    throw new ArgumentNullException("encryptedData is null");

                string fileContents = DecryptStringFromBytes(Convert.FromBase64String(encryptedData));

                return JsonSerializer.Deserialize<T>(fileContents);
            }
            catch (Exception)
            {
                throw;
            }            
        }

        private static async Task<byte[]> ToByteArray<T>(T obj)
        {
            if (obj == null)
                return null;

            try
            {
                return JsonSerializer.SerializeToUtf8Bytes<T>(obj);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText)
        {            
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText is null!");
            
            string plaintext = null;

            // Create an Rijndael object with key and IV
            Rijndael rijAlg = GetRijndaelEncryptionAlgorithm();

            // Create a decryptor to perform the stream transformation.
            ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream and then place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }

        private static Rijndael GetRijndaelEncryptionAlgorithm()
        {
            Rijndael rijAlg = Rijndael.Create();
            rijAlg.Key = Encoding.UTF8.GetBytes("abcdefghijklmnop"); ;
            rijAlg.IV = Encoding.UTF8.GetBytes("abcdefghijklmnop");
            return rijAlg;
        }
    }
}
