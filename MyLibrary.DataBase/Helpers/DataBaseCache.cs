using System;
using System.Text;

namespace MyLibrary.DataBase.Helpers
{
    public class DataBaseCache
    {
        private readonly DBContext context;
        private readonly string tableName, keyColumnName, dataColumnName, timeColumnName;


        public DataBaseCache(DBContext context, string tableName, string keyColumnName, string dataColumnName, string timeColumnName)
        {
            this.context = context;
            this.tableName = tableName;
            this.keyColumnName = tableName + "." + keyColumnName;
            this.dataColumnName = tableName + "." + dataColumnName;
            this.timeColumnName = tableName + "." + timeColumnName;
        }


        public CacheContent<byte[]> LoadData(string key)
        {
            string hash = CalculateHash(key);

            DBRow row;
            lock (context)
            {
                row = context.Select(tableName)
                    .Select(timeColumnName, dataColumnName)
                    .Where(keyColumnName, hash)
                    .ReadRow();
            }

            if (row == null)
            {
                return null;
            }

            return new CacheContent<byte[]>
            {
                CreateTime = row.GetDateTime(timeColumnName),
                Data = row.GetBytes(dataColumnName),
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
                Data = Data.DecompressText(content.Data),
            };
        }

        public void SaveData(string key, byte[] data)
        {
            string hash = CalculateHash(key);
            lock (context)
            {
                context.Insert(tableName)
                    .UpdateOrInsert(keyColumnName)
                    .Set(keyColumnName, hash)
                    .Set(dataColumnName, data)
                    .Set(timeColumnName, DateTime.Now)
                    .Execute();
            }
        }

        public void SaveString(string key, string text)
        {
            byte[] data = Data.CompressText(text);
            SaveData(key, data);
        }

        public void Clear(DateTime limitDate)
        {
            lock (context)
            {
                context.Delete(tableName)
                    .Where(timeColumnName, "<", limitDate)
                    .Execute();
            }
        }


        private string CalculateHash(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            data = Cryptography.GetMD5Hash(data);
            return Data.ToHexText(data);
        }
    }

    public class CacheContent<T>
    {
        public DateTime CreateTime { get; set; }
        public T Data { get; set; }
    }
}
