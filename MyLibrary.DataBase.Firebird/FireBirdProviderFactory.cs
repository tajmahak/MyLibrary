using FirebirdSql.Data.FirebirdClient;
using System.Data.Common;

namespace MyLibrary.DataBase.Firebird
{
    public static class FireBirdProviderFactory
    {
        public static DbConnection CreateDefaultConnection(
            string database,
            string dataSource,
            string userID,
            string password,
            int? port = null,
            string charset = null,
            int? dialect = null)
        {
            FbConnectionStringBuilder conBuilder = new FbConnectionStringBuilder();
            conBuilder.ServerType = FbServerType.Default;
            conBuilder.Database = database;
            conBuilder.DataSource = dataSource;
            conBuilder.UserID = userID; // SYSDBA
            conBuilder.Password = password; // masterkey
            if (port != null)
            {
                conBuilder.Port = port.Value; // 3050
            }
            if (charset != null)
            {
                conBuilder.Charset = charset; // WIN1251
            }
            if (dialect != null)
            {
                conBuilder.Dialect = dialect.Value; // 3
            }
            FbConnection connection = new FbConnection(conBuilder.ToString());
            return connection;
        }

        public static DbConnection CreateEmbeddedConnection(
            string database,
            string userID,
            string password,
            string clientLibrary = null,
            string charset = null,
            int? dialect = null)
        {
            FbConnectionStringBuilder conBuilder = new FbConnectionStringBuilder();
            conBuilder.ServerType = FbServerType.Embedded;
            conBuilder.Database = database;
            if (clientLibrary != null)
            {
                conBuilder.ClientLibrary = clientLibrary;
            }
            if (userID != null)
            {
                conBuilder.UserID = userID;
            }
            if (password != null)
            {
                conBuilder.Password = password;
            }
            if (charset != null)
            {
                conBuilder.Charset = charset;
            }
            if (dialect != null)
            {
                conBuilder.Dialect = dialect.Value;
            }
            FbConnection connection = new FbConnection(conBuilder.ToString());
            return connection;
        }
    }
}