using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace PrjVigiaCore.DAO
{
    public static class CryptoHelper
    {
        private const int KeySize = 256;
        private const int IvSize = 16;
        private static readonly byte[] Key;
        private static readonly object _lock = new object();

        static CryptoHelper()
        {
            using var sha = SHA256.Create();
            Key = sha.ComputeHash(Encoding.UTF8.GetBytes("tpRUc8vsZ0Kerslb6RcbqyNiqRPrEcWdtVXozjNzj"));
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            try
            {
                using var aes = Aes.Create();
                lock (_lock)
                {
                    aes.Key = Key;
                    aes.GenerateIV();
                }

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return HttpUtility.UrlEncode(Convert.ToBase64String(ms.ToArray()));
            }
            catch
            {
                return null;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;

            try
            {
                var decodedText = HttpUtility.UrlDecode(encryptedText);
                var fullCipher = Convert.FromBase64String(decodedText);

                if (fullCipher.Length < IvSize)
                    return null;

                var iv = new byte[IvSize];
                var cipher = new byte[fullCipher.Length - IvSize];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, IvSize);
                Buffer.BlockCopy(fullCipher, IvSize, cipher, 0, cipher.Length);

                using var aes = Aes.Create();
                lock (_lock)
                {
                    aes.Key = Key;
                    aes.IV = iv;
                }

                using var ms = new MemoryStream(cipher);
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
    }
}