
using System.Security.Cryptography;
using System.Text;

namespace FileUploaderServer.Services
{
    public static class HashingPassword
    {
        private const int keySize = 64;
        private const int iterations = 350000;
        private static HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;

        /// <summary>
        /// Hash Password with salt
        /// </summary>
        /// <param name="password">plain text</param>
        /// <param name="salt">The salt for the given password which is needed to unhash password</param>
        /// <returns>Hashed password</returns>
        public static string HashPasword(string password, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(keySize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                hashAlgorithm,
                keySize);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Verify the entered password wiht the hashed password
        /// </summary>
        /// <param name="password">plain text password</param>
        /// <param name="hash">hashed password</param>
        /// <param name="salt">salt need to unhash the password</param>
        /// <returns>Whether the plain password and the hashed password are same or not</returns>
        public static bool VerifyPassword(string password, string hash, byte[] salt)
        {
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, keySize);
            return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromHexString(hash));
        }
    }
}
