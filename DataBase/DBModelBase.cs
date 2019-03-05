using System.Collections.Generic;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    public abstract class DBModelBase
    {
        public bool IsInitialized { get; protected internal set; }
        public DBTable[] Tables { get; protected internal set; }
        protected internal Dictionary<DBTable, string> DefaultSelectCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultInsertCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultUpdateCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultDeleteCommandsDict { get; private set; }
        protected internal Dictionary<string, DBTable> TablesDict { get; private set; }
        protected internal Dictionary<string, DBColumn> ColumnsDict { get; private set; }

        public DBModelBase()
        {
            DefaultSelectCommandsDict = new Dictionary<DBTable, string>();
            DefaultInsertCommandsDict = new Dictionary<DBTable, string>();
            DefaultUpdateCommandsDict = new Dictionary<DBTable, string>();
            DefaultDeleteCommandsDict = new Dictionary<DBTable, string>();
            TablesDict = new Dictionary<string, DBTable>();
            ColumnsDict = new Dictionary<string, DBColumn>();
        }

        public abstract void Initialize(DbConnection connection);
        public abstract void AddParameter(DbCommand command, string name, object value);
        public abstract object ExecuteInsertCommand(DbCommand command);
        public abstract DbCommand BuildCommand(DbConnection connection, DBCommand command);

        public DBContext CreateDBContext(DbConnection connection)
        {
            var context = new DBContext(this, connection);
            return context;
        }
        public DBCommand CreateDBCommand(string tableName)
        {
            var table = GetTable(tableName);
            var cmd = new DBCommand(table);
            return cmd;
        }
        public DBTable GetTable(string tableName)
        {
            DBTable table;
            if (!TablesDict.TryGetValue(tableName, out table))
                throw DBInternal.UnknownTableException(tableName);
            return table;
        }
        public DBColumn GetColumn(string columnName)
        {
            DBColumn column;
            if (!ColumnsDict.TryGetValue(columnName, out column))
                throw DBInternal.UnknownColumnException(null, columnName);
            return column;
        }
    }
}
