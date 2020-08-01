using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "SQLite".
    /// </summary>
    public sealed class SQLiteProvider : DBProvider
    {
        public SQLiteProvider()
        {
            OpenBlock = "[";
            CloseBlock = "]";
        }

        public override void FillTableSchema(DbConnection dbConnection)
        {
            using (DataTable tableSchema = dbConnection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    if ((string)tableRow["TABLE_TYPE"] != "SYSTEM_TABLE")
                    {
                        DBTable table = new DBTable()
                        {
                            Name = (string)tableRow["TABLE_NAME"]
                        };
                        Tables.Add(table);
                    }
                }
            }

            using (DataSet dataSet = new DataSet())
            {
                foreach (DBTable table in Tables)
                {
                    string query = string.Concat("SELECT * FROM \"", table.Name, "\" LIMIT 0");
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(query, (SQLiteConnection)dbConnection))
                    {
                        dataAdapter.Fill(dataSet, 0, 0, table.Name);
                    }
                }

                for (int i = 0; i < Tables.Count; i++)
                {
                    DataTable tableRow = dataSet.Tables[i];
                    DBTable table = Tables[i];
                    for (int j = 0; j < tableRow.Columns.Count; j++)
                    {
                        DataColumn columnRow = tableRow.Columns[j];
                        DBColumn column = new DBColumn(table)
                        {
                            OrderIndex = j,
                            Name = columnRow.ColumnName,
                            DataType = columnRow.DataType
                        };
                        table.Columns.Add(column);
                    }
                }
            }

            using (DataTable columnSchema = dbConnection.GetSchema("Columns"))
            {
                foreach (DataRow columnRow in columnSchema.Rows)
                {
                    string tableName = (string)columnRow["TABLE_NAME"];
                    if (Tables.Contains(tableName))
                    {
                        DBTable table = Tables[tableName];
                        string columnName = (string)columnRow["COLUMN_NAME"];
                        DBColumn column = table.Columns.Find(x => x.Name == columnName);

                        column.NotNull = (bool)columnRow["IS_NULLABLE"] == false;
                        string defaultValue = columnRow["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        column.Size = (int)columnRow["CHARACTER_MAXIMUM_LENGTH"];
                        object description = columnRow["DESCRIPTION"];
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
        }
        public override DbParameter CreateParameter(string name, object value)
        {
            return new SQLiteParameter(name, value);
        }
        public override object ExecuteInsertCommand(DbCommand dbCommand)
        {
            SQLiteConnection dbConnection = (SQLiteConnection)dbCommand.Connection;
            lock (dbConnection)
            {
                dbCommand.ExecuteNonQuery();
                return dbConnection.LastInsertRowId;
            }
        }
        public override DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0)
        {
            DBCompiledQuery cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            StringBuilder sql = new StringBuilder();
            if (query.StatementType == StatementType.Select)
            {
                PrepareSelectBlock(sql, query, cQuery);

                DBQueryStructureBlock block = query.Structure.Find(DBQueryStructureType.Distinct);
                if (block != null)
                {
                    sql.Insert(6, " DISTINCT");
                }

                block = query.Structure.Find(DBQueryStructureType.Offset);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" SKIP ", block[0]));
                }

                block = query.Structure.Find(DBQueryStructureType.Limit);
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
