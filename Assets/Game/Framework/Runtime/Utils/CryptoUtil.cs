using System;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Utils
{
    public static class CryptoUtil
    {
        #region SHA256

        public static string SHA256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        #endregion

        #region AES

        public static string AESEncrypt(string plainText, string key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                var encryptor = aes.CreateEncryptor();
                var bytes = Encoding.UTF8.GetBytes(plainText);
                var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Convert.ToBase64String(encrypted);
            }
        }

        public static string AESDecrypt(string cipherText, string key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                var decryptor = aes.CreateDecryptor();
                var bytes = Convert.FromBase64String(cipherText);
                var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        #endregion

        #region MD5

        public static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        #endregion
    }
}
