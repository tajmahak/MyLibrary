using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "FireBird".
    /// </summary>
    public sealed class FireBirdDBModel : DBModelBase
    {
        public FireBirdDBModel()
        {
            OpenBlock = CloseBlock = '\"';
        }

        public override DBTable[] GetTableSchema(DbConnection connection)
        {
            var tables = new List<DBTable>();

            using (var tableSchema = connection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    if ((short)tableRow["IS_SYSTEM_TABLE"] == 0)
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
                    var query = string.Concat("SELECT FIRST 0 * FROM \"", table.Name, "\"");
                    using (var dataAdapter = new FbDataAdapter(query, (FbConnection)connection))
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

                        column.AllowDBNull = (bool)columnRow["IS_NULLABLE"];
                        var defaultValue = columnRow["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = defaultValue.Remove(0, 8);
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        column.Size = (int)columnRow["COLUMN_SIZE"];
                        var description = columnRow["DESCRIPTION"];
                        if (description != DBNull.Value)
                        {
                            column.Description = (string)description;
                        }
                    }
                }
            }

            using (var primaryKeySchema = connection.GetSchema("PrimaryKeys"))
            {
                foreach (DataRow primaryKeyRow in primaryKeySchema.Rows)
                {
                    var tableName = (string)primaryKeyRow["TABLE_NAME"];
                    var table = tables.Find(x => x.Name == tableName);

                    var columnName = (string)primaryKeyRow["COLUMN_NAME"];
                    var column = table.Columns.Find(x => x.Name == columnName);
                    column.IsPrimary = true;
                    table.PrimaryKeyColumn = column;
                }
            }

            return tables.ToArray();
        }
        public override string GetDefaultInsertCommand(DBTable table)
        {
            return string.Concat(GetInsertCommand(table), " RETURNING ", GetName(table.Columns[table.PrimaryKeyColumn.Index].Name));
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((FbCommand)command).Parameters.AddWithValue(name, value);
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
        }
    }
}
