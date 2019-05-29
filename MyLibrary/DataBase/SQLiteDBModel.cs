using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "SQLite".
    /// </summary>
    public sealed class SQLiteDBModel : DBModelBase
    {
        public SQLiteDBModel()
        {
            OpenBlock = "[";
            CloseBlock = "]";
        }

        public override DBTable[] GetTableSchema(DbConnection connection)
        {
            var tables = new List<DBTable>();

            using (var tableSchema = connection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    if ((string)tableRow["TABLE_TYPE"] != "SYSTEM_TABLE")
                    {
                        var tableName = (string)tableRow["TABLE_NAME"];
                        var table = new DBTable(this, tableName);
                        tables.Add(table);
                    }
                }
            }

            using (var dataSet = new DataSet())
            {
                foreach (var table in tables)
                {
                    var query = string.Concat("SELECT * FROM \"", table.Name, "\" LIMIT 0");
                    using (var dataAdapter = new SQLiteDataAdapter(query, (SQLiteConnection)connection))
                    {
                        dataAdapter.Fill(dataSet, 0, 0, table.Name);
                    }
                }

                for (int i = 0; i < tables.Count; i++)
                {
                    var tableRow = dataSet.Tables[i];
                    var table = tables[i];
                    for (int j = 0; j < tableRow.Columns.Count; j++)
                    {
                        var columnRow = tableRow.Columns[j];
                        var column = new DBColumn(table)
                        {
                            Index = j,
                            Name = columnRow.ColumnName,
                            DataType = columnRow.DataType
                        };
                        table.AddColumn(column);
                    }
                }
            }

            using (var columnSchema = connection.GetSchema("Columns"))
            {
                foreach (DataRow columnRow in columnSchema.Rows)
                {
                    var tableName = (string)columnRow["TABLE_NAME"];
                    var table = tables.Find(x => x.Name == tableName);
                    if (table != null)
                    {
                        var columnName = (string)columnRow["COLUMN_NAME"];
                        var column = table.Columns.Find(x => x.Name == columnName);

                        column.AllowDBNull = (bool)columnRow["IS_NULLABLE"];
                        var defaultValue = columnRow["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        column.Size = (int)columnRow["CHARACTER_MAXIMUM_LENGTH"];
                        var description = columnRow["DESCRIPTION"];
                        if (description != DBNull.Value)
                        {
                            column.Description = (string)description;
                        }
                        if ((bool)columnRow["PRIMARY_KEY"])
                        {
                            column.IsPrimary = true;
                            table.PrimaryKeyColumn = column;
                        }
                    }
                }
            }

            return tables.ToArray();
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((SQLiteCommand)command).Parameters.AddWithValue(name, value);
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
        public override DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0)
        {
            var cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            DBQueryStructureBlock block;
            List<DBQueryStructureBlock> blockList;

            var sql = new StringBuilder();
            if (query.Type == DBQueryType.Select)
            {
                PrepareSelectCommand(sql, query, cQuery);

                block = FindBlock(query, DBQueryStructureType.Distinct);
                if (block != null)
                {
                    sql.Insert(6, " DISTINCT");
                }

                block = FindBlock(query, DBQueryStructureType.Skip);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" SKIP ", block[0]));
                }

                block = FindBlock(query, DBQueryStructureType.First);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" FIRST ", block[0]));
                }

                PrepareJoinCommand(sql, query);
            }
            else if (query.Type == DBQueryType.Insert)
            {
                PrepareInsertCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryType.Update)
            {
                PrepareUpdateCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryType.Delete)
            {
                PrepareDeleteCommand(sql, query);
            }

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryType.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            PrepareUnionCommand(sql, query, cQuery);

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }
    }
}
