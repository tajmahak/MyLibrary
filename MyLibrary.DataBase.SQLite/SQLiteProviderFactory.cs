using System.Data.Common;
using System.Data.SQLite;

namespace MyLibrary.DataBase.SQLite
{
    public static class SQLiteProviderFactory
    {
        public static DbConnection CreateConnection(string dataSource)
        {
            SQLiteConnectionStringBuilder conBuilder = new SQLiteConnectionStringBuilder();
            conBuilder.DataSource = dataSource;

            SQLiteConnection dbConnection = new SQLiteConnection(conBuilder.ToString());
            return dbConnection;
        }
    }
}
