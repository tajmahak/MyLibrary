using System;
using System.IO;
using System.Security.Cryptography;

namespace MyLibrary.Data
{
    public static class Cryptography
    {
        public static byte[] EncryptAES(byte[] data, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;

                aes.Key = key;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    writer.Write(data.Length);
                    writer.Write(aes.IV);
                    writer.Write(PerformCryptography(data, encryptor));
                    return ms.ToArray();
                }
            }
        }
        public static byte[] DecryptAES(byte[] data, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    var dataLength = reader.ReadInt32();

                    aes.Key = key;
                    aes.IV = reader.ReadBytes(aes.IV.Length);

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        var decryptData = reader.ReadBytes((int)(ms.Length - ms.Position));
                        decryptData = PerformCryptography(decryptData, decryptor);

                        if (decryptData.Length != dataLength)
                        {
                            Array.Resize(ref decryptData, dataLength);
                        }

                        return decryptData;
                    }
                }
            }








        }
        public static byte[] GetRandomBytes(int length, bool nonZero = false)
        {
            var data = new byte[length];
            using (var provider = RNGCryptoServiceProvider.Create())
            {
                if (nonZero)
                {
                    provider.GetNonZeroBytes(data);
                }
                else
                {
                    provider.GetBytes(data);
                }
            }
            return data;
        }

        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (var ms = new MemoryStream())
            using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }
}
