using MyLibrary.DataBase;
using System;
using System.Text;

namespace MyLibrary.Data
{
    public class DataBaseCache
    {
        private readonly DBContext _context;
        private readonly string _tableName, _keyColumnName, _dataColumnName, _timeColumnName;

        public DataBaseCache(DBContext context, string tableName, string keyColumnName, string dataColumnName, string timeColumnName)
        {
            _context = context;
            _tableName = tableName;
            _keyColumnName = tableName + "." + keyColumnName;
            _dataColumnName = tableName + "." + dataColumnName;
            _timeColumnName = tableName + "." + timeColumnName;
        }

        public CacheContent<byte[]> LoadData(string key)
        {
            var hash = CalculateHash(key);

            DBRow row;
            lock (_context)
            {
                row = _context.Select(_tableName)
                    .Select(_timeColumnName, _dataColumnName)
                    .Where(_keyColumnName, hash)
                    .ReadRow();
            }

            if (row == null)
            {
                return null;
            }

            return new CacheContent<byte[]>
            {
                CreateTime = row.Get<DateTime>(_timeColumnName),
                Data = row.Get<byte[]>(_dataColumnName),
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
                _context.Insert(_tableName)
                    .UpdateOrInsert(_keyColumnName)
                    .Set(_keyColumnName, hash)
                    .Set(_dataColumnName, data)
                    .Set(_timeColumnName, DateTime.Now)
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
                _context.Delete(_tableName)
                    .Where(_timeColumnName, "<", limitDate)
                    .Execute();
            }
        }

        private string CalculateHash(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            data = Cryptography.GetMD5Hash(data);
            return Format.ToHexText(data);
        }
    }

    public class CacheContent<T>
    {
        public DateTime CreateTime { get; set; }
        public T Data { get; set; }
    }
}
