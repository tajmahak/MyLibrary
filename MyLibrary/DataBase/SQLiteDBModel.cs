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
                        var table = new DBTable(this);
                        table.Name = (string)tableRow["TABLE_NAME"];
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

                for (var i = 0; i < tables.Count; i++)
                {
                    var tableRow = dataSet.Tables[i];
                    var table = tables[i];
                    for (var j = 0; j < tableRow.Columns.Count; j++)
                    {
                        var columnRow = tableRow.Columns[j];
                        var column = new DBColumn(table)
                        {
                            OrderIndex = j,
                            Name = columnRow.ColumnName,
                            DataType = columnRow.DataType
                        };
                        table.Columns.Add(column);
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

                        column.NotNull = (bool)columnRow["IS_NULLABLE"] == false;
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

            var sql = new StringBuilder();
            if (query.StatementType == StatementType.Select)
            {
                PrepareSelectBlock(sql, query, cQuery);

                var block = query.FindBlock(DBQueryStructureType.Distinct);
                if (block != null)
                {
                    sql.Insert(6, " DISTINCT");
                }

                block = query.FindBlock(DBQueryStructureType.Offset);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" SKIP ", block[0]));
                }

                block = query.FindBlock(DBQueryStructureType.Limit);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" FIRST ", block[0]));
                }

                PrepareJoinBlock(sql, query);
                PrepareWhereBlock(sql, query, cQuery);
                PrepareGroupByBlock(sql, query);
                PrepareHavingBlock(sql, query, cQuery);
                PrepareUnionBlock(sql, query, cQuery);
                PrepareOrderByBlock(sql, query);
            }
            else if (query.StatementType == StatementType.Insert)
            {
                PrepareInsertBlock(sql, query, cQuery);
                PrepareWhereBlock(sql, query, cQuery);
            }
            else if (query.StatementType == StatementType.Update)
            {
                PrepareUpdateBlock(sql, query, cQuery);
                PrepareWhereBlock(sql, query, cQuery);
            }
            else if (query.StatementType == StatementType.Delete)
            {
                PrepareDeleteBlock(sql, query);
                PrepareWhereBlock(sql, query, cQuery);
            }

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }
    }
}
