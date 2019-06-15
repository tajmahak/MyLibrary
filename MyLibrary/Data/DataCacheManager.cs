using MyLibrary.DataBase;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MyLibrary.Data
{
    public class DataCacheManager : IDisposable
    {
        private readonly DBContext _context;

        public DataCacheManager(DBContext context)
        {
            _context = context;
        }

        public CacheContent<byte[]> LoadData(string key)
        {
            var hash = CalculateMD5(key);

            var cmd = _context.Query(DataCacheTable._)
                .Select(DataCacheTable.Time, DataCacheTable.Data)
                .Where(DataCacheTable.Hash, hash);

            DBRow row;
            lock (_context)
            {
                row = _context.Get(cmd);
            }

            if (row == null)
            {
                return null;
            }

            return new CacheContent<byte[]>()
            {
                CreateTime = row.Get<DateTime>(DataCacheTable.Time),
                Data = row.Get<byte[]>(DataCacheTable.Data),
            };
        }
        public CacheContent<string> LoadString(string key)
        {
            var content = LoadData(key);

            if (content == null)
            {
                return null;
            }

            return new CacheContent<string>()
            {
                CreateTime = content.CreateTime,
                Data = DecompressText(content.Data),
            };
        }
        public void SaveData(string key, byte[] data)
        {
            Save(key, data, CacheType.Data);
        }
        public void SaveString(string key, string text)
        {
            var data = CompressText(text);
            Save(key, data, CacheType.String);
        }
        public void Clear(DateTime limitDate)
        {
            var cmd = _context.Query(DataCacheTable._)
                .Delete()
                .Where(DataCacheTable.Time, "<", limitDate);

            lock (_context)
            {
                _context.Execute(cmd);
            }
        }
        public void Dispose()
        {
            _context.Dispose();
        }

        private void Save(string key, byte[] data, CacheType type)
        {
            var hash = CalculateMD5(key);

            var cmd = _context.Query(DataCacheTable._)
                .UpdateOrInsert(DataCacheTable.Hash)
                .Set(DataCacheTable.Hash, hash)
                .Set(DataCacheTable.Data, data)
                .Set(DataCacheTable.Time, DateTime.Now)
                .Set(DataCacheTable.Type, type);

            lock (_context)
            {
                _context.Execute(cmd);
            }
        }
        private string CalculateMD5(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);

            using (var md5 = MD5.Create())
            {
                data = md5.ComputeHash(data);
            }

            var str = new StringBuilder(32);
            for (var i = 0; i < 16; i++)
            {
                str.Append(data[i].ToString("X2"));
            }

            return str.ToString();
        }
        private static byte[] CompressText(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);

            using (var mem = new MemoryStream())
            {
                using (var stream = new DeflateStream(mem, CompressionMode.Compress, true))
                {
                    stream.Write(data, 0, data.Length);
                }
                return mem.ToArray();
            }
        }
        private static string DecompressText(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                using (var stream = new DeflateStream(mem, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private enum CacheType
        {
            Data = 1,
            String = 2,
        }
        private static class DataCacheTable
        {
            public const string _ = "DATACACHE";
            public const string Hash = "DATACACHE.HASH";
            public const string Data = "DATACACHE.DATA";
            public const string Time = "DATACACHE.TIME";
            public const string Type = "DATACACHE.TYPE";
        }
    }

    public class CacheContent<T>
    {
        public DateTime CreateTime { get; set; }
        public T Data { get; set; }
    }
}
