using System;
using System.IO;
using System.Security.Cryptography;

namespace MyLibrary.Data
{
    public static class Cryptography
    {
        /// <summary>
        /// Выполняет симметричное шифрование с помощью алгоритма <see cref="Aes"/>.
        /// </summary>
        /// <param name="data">Данные, которые необходимо зашифровать.</param>
        /// <param name="key">Секретный ключ, который должен использоваться (128/192/256 бит).</param>
        /// <returns></returns>
        public static byte[] EncryptAES(byte[] data, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

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
        /// <summary>
        /// Выполняет симметричное дешифрование с помощью алгоритма <see cref="Aes"/>.
        /// </summary>
        /// <param name="data">Данные, которые необходимо дешифровать.</param>
        /// <param name="key">Секретный ключ, который должен использоваться (128/192/256 бит).</param>
        /// <returns></returns>
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

        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма <see cref="MD5"/>.
        /// </summary>
        /// <param name="data">Массив байтов.</param>
        /// <returns></returns>
        public static byte[] GetMD5Hash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }
        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма <see cref="SHA1"/>.
        /// </summary>
        /// <param name="data">Массив байтов.</param>
        /// <returns></returns>
        public static byte[] GetSHA1Hash(byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data);
            }
        }
        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма <see cref="SHA256"/>.
        /// </summary>
        /// <param name="data">Массив байтов.</param>
        /// <returns></returns>
        public static byte[] GetSHA256Hash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        /// <summary>
        /// Заполняет массив байтов криптостойкой случайной последовательностью значений.
        /// </summary>
        /// <param name="length">Длина получаемого массива байт.</param>
        /// <param name="nonZero">Заполнять ненулевыми значениями.</param>
        /// <returns></returns>
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
