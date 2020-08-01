using MyLibrary.Data;
using System;
using System.Text;

namespace MyLibrary.DataBase.Data
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
            string hash = CalculateHash(key);

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
                CreateTime = row.GetDateTime(_timeColumnName),
                Data = row.GetBytes(_dataColumnName),
            };
        }
        public CacheContent<string> LoadString(string key)
        {
            CacheContent<byte[]> content = LoadData(key);

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
            string hash = CalculateHash(key);
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
            byte[] data = Format.CompressText(text);
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
            byte[] data = Encoding.UTF8.GetBytes(text);
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
