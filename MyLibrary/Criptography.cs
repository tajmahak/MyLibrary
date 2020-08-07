using System;
using System.IO;
using System.Security.Cryptography;

namespace MyLibrary
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
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.GenerateIV();

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
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
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int dataLength = reader.ReadInt32();

                    aes.Key = key;
                    aes.IV = reader.ReadBytes(aes.IV.Length);

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] decryptData = reader.ReadBytes((int)(ms.Length - ms.Position));
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
            using (MD5 md5 = MD5.Create())
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
            using (MD5 md5 = MD5.Create())
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
            using (SHA1 sha1 = SHA1.Create())
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
            using (SHA1 sha1 = SHA1.Create())
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
            using (SHA256 sha256 = SHA256.Create())
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
            using (SHA256 sha256 = SHA256.Create())
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
            //!!!
            throw new NotImplementedException();
            //uint crc32 = 0;
            //crc32 = NativeMethods.RtlComputeCrc32(crc32, data, data.Length);
            //data = BitConverter.GetBytes(crc32);
            //Array.Reverse(data, 0, data.Length);
            //return data;
        }
        /// <summary>
        /// Вычисляет хэш-значение для заданного массива байтов с использованием алгоритма CRC-32.
        /// </summary>
        /// <param name="inputStream">Входные данные, для которых вычисляется хэш-код.</param>
        /// <returns></returns>
        public static byte[] GetCRC32Hash(Stream inputStream)
        {
            //!!!
            throw new NotImplementedException();
            //uint crc32 = 0;
            //int readed;
            //byte[] buffer = new byte[4096];

            //while ((readed = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            //{
            //    crc32 = NativeMethods.RtlComputeCrc32(crc32, buffer, readed);
            //}

            //byte[] hash = BitConverter.GetBytes(crc32);
            //Array.Reverse(hash, 0, hash.Length);
            //return hash;
        }

        /// <summary>
        /// Заполняет массив байтов криптостойкой случайной последовательностью значений.
        /// </summary>
        /// <param name="length">Длина получаемого массива байт.</param>
        /// <param name="nonZero">Заполнять ненулевыми значениями.</param>
        /// <returns></returns>
        public static byte[] GetRandomBytes(int length, bool nonZero = false)
        {
            byte[] data = new byte[length];
            using (RandomNumberGenerator provider = RandomNumberGenerator.Create())
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
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }
}
