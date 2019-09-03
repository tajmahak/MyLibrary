using System;
using System.IO;
using System.Security.Cryptography;

namespace MyLibrary.MyLibrary.Data
{
    public static class Cryptography
    {
        public static byte[] EncryptAES(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = iv.Length * 8;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
        public static byte[] DecryptAES(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = iv.Length * 8;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

    }
}
