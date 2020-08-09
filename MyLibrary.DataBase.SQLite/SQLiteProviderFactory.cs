using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace MyLibrary.DataBase.SQLite
{
    public static class SQLiteProviderFactory
    {
        public static DbConnection CreateConnection(string dataSource)
        {
            SQLiteConnectionStringBuilder conBuilder = new SQLiteConnectionStringBuilder();
            conBuilder.DataSource = dataSource;

            var dbConnection = new SQLiteConnection(conBuilder.ToString());
            return dbConnection;
        }
    }
}
