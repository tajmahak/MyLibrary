using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using System.Collections.Generic;
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
            string connectionString = CreateDefaultConnectionString(
                database, dataSource, userID, password, port, charset, dialect);
            return CreateConnection(connectionString);
        }

        public static DbConnection CreateEmbeddedConnection(
            string database,
            string userID,
            string password,
            string clientLibrary = null,
            string charset = null,
            int? dialect = null)
        {
            string connectionString = CreateEmbeddedConnectionString(
                database, userID, password, clientLibrary, charset, dialect);
            return CreateConnection(connectionString);
        }

        public static string CreateDefaultConnectionString(
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
            return conBuilder.ToString();
        }

        public static string CreateEmbeddedConnectionString(
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
            return conBuilder.ToString();
        }

        public static DbConnection CreateConnection(string connectionString)
        {
            FbConnection connection = new FbConnection(connectionString);
            return connection;
        }

        public static void CreateDatabase(string connectionString, bool overwrite = false)
        {
            FbConnection.CreateDatabase(connectionString, overwrite);
        }

        public static string[] ParseScript(string script)
        {
            List<string> statements = new List<string>();

            FbScript fbScript = new FbScript(script);
            fbScript.Parse();
            foreach (FbStatement statement in fbScript.Results)
            {
                statements.Add(statement.Text);
            }

            return statements.ToArray();
        }
    }
}