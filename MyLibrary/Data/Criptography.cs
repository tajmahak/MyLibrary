using MyLibrary.Interop;
using System;
using System.IO;
using System.Security.Cryptography;

namespace MyLibrary.Data
{
    public static class Cryptography
    {
        /// <summary>
        /// Выполняет симметричное шифрование с помощью алгоритма AES.
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
                using (var encryptor = aes.CreateEncryptor())
                {
                    writer.Write(data.Length);
                    writer.Write(aes.IV);
                    writer.Write(PerformCryptography(data, encryptor));
                    return ms.ToArray();
                }
            }
        }
        /// <summary>
        /// Выполняет симметричное дешифрование с помощью алгоритма AES.
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

                    using (var decryptor = aes.CreateDecryptor())
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
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма MD5.
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
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма MD5.
        /// </summary>
        /// <param name="inputStream">Входные данные, для которых вычисляется хэш-код.</param>
        /// <returns></returns>
        public static byte[] GetMD5Hash(Stream inputStream)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(inputStream);
            }
        }

        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма SHA-1.
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
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма SHA-1.
        /// </summary>
        /// <param name="inputStream">Входные данные, для которых вычисляется хэш-код.</param>
        /// <returns></returns>
        public static byte[] GetSHA1Hash(Stream inputStream)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(inputStream);
            }
        }

        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма SHA-256.
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
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма SHA-256.
        /// </summary>
        /// <param name="inputStream">Входные данные, для которых вычисляется хэш-код.</param>
        /// <returns></returns>
        public static byte[] GetSHA256Hash(Stream inputStream)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(inputStream);
            }
        }

        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма CRC-32.
        /// </summary>
        /// <param name="data">Массив байтов.</param>
        /// <returns></returns>
        public static byte[] GetCRC32Hash(byte[] data)
        {
            uint crc32 = 0;
            crc32 = NativeMethods.RtlComputeCrc32(crc32, data, data.Length);
            data = BitConverter.GetBytes(crc32);
            Array.Reverse(data, 0, data.Length);
            return data;
        }
        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма CRC-32.
        /// </summary>
        /// <param name="inputStream">Входные данные, для которых вычисляется хэш-код.</param>
        /// <returns></returns>
        public static byte[] GetCRC32Hash(Stream inputStream)
        {
            uint crc32 = 0;
            int readed;
            var buffer = new byte[4096];

            while ((readed = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                crc32 = NativeMethods.RtlComputeCrc32(crc32, buffer, readed);
            }

            var hash = BitConverter.GetBytes(crc32);
            Array.Reverse(hash, 0, hash.Length);
            return hash;
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
            using (var provider = RandomNumberGenerator.Create())
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
