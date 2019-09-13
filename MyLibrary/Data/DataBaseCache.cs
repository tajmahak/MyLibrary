using MyLibrary.DataBase;
using System;
using System.Text;

namespace MyLibrary.Data
{
    public class DataBaseCache
    {
        private readonly DBContext _context;
        public DataBaseCache(DBContext context)
        {
            _context = context;
        }

        public CacheContent<byte[]> LoadData(string key)
        {
            var hash = CalculateHash(key);

            DBRow row;
            lock (_context)
            {
                row = _context.Query(DataCacheTable.TableName)
                    .Select(DataCacheTable.Time, DataCacheTable.Data)
                    .Where(DataCacheTable.Hash, hash)
                    .ReadRow();
            }

            if (row == null)
            {
                return null;
            }

            return new CacheContent<byte[]>
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

            return new CacheContent<string>
            {
                CreateTime = content.CreateTime,
                Data = Format.DecompressText(content.Data),
            };
        }
        public void SaveData(string key, byte[] data)
        {
            var hash = CalculateHash(key);
            lock (_context)
            {
                _context.Query(DataCacheTable.TableName)
                    .UpdateOrInsert(DataCacheTable.Hash)
                    .Set(DataCacheTable.Hash, hash)
                    .Set(DataCacheTable.Data, data)
                    .Set(DataCacheTable.Time, DateTime.Now)
                    .Execute();
            }
        }
        public void SaveString(string key, string text)
        {
            var data = Format.CompressText(text);
            SaveData(key, data);
        }
        public void Clear(DateTime limitDate)
        {
            lock (_context)
            {
                _context.Query(DataCacheTable.TableName)
                    .Delete()
                    .Where(DataCacheTable.Time, "<", limitDate)
                    .Execute();
            }
        }

        private string CalculateHash(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            data = Cryptography.GetMD5Hash(data);
            return Format.ToHexText(data);
        }

        private static class DataCacheTable
        {
            public const string TableName = "DATACACHE";
            public const string Hash = "DATACACHE.HASH";
            public const string Data = "DATACACHE.DATA";
            public const string Time = "DATACACHE.TIME";
        }
    }

    public class CacheContent<T>
    {
        public DateTime CreateTime { get; set; }
        public T Data { get; set; }
    }
}
