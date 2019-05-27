using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace MyLibrary.DataBase
{
    public class SQLiteDBModel : DBModelBase
    {
        public SQLiteDBModel()
        {
            OpenBlock = '[';
            CloseBlock = ']';
            ParameterPrefix = '@';
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            return ((SQLiteConnection)connection).CreateCommand();
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            var connection = (SQLiteConnection)command.Connection;
            lock (connection)
            {
                command.ExecuteNonQuery();
                return connection.LastInsertRowId;
            }
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((SQLiteCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0)
        {
            var cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            var sql = new StringBuilder();
            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareSelectCommand(sql, query, cQuery);
                PrepareJoinCommand(sql, query);
            }
            else if (query.Type == DBQueryTypeEnum.Insert)
            {
                PrepareInsertCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Update)
            {
                PrepareUpdateCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Delete)
            {
                PrepareDeleteCommand(sql, query);
            }

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            PrepareUnionCommand(sql, query, cQuery);

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }
        public override Dictionary<string, Type> GetDataTypes()
        {
            //!!!
            throw new NotImplementedException();
        }
    }
}
