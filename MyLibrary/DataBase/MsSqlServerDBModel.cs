using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "Microsoft SQL Server"
    /// </summary>
    public class MsSqlServerDBModel : DBModelBase
    {
        public MsSqlServerDBModel()
        {
            OpenBlock = "[";
            CloseBlock = "]";
        }

        protected override string GetInsertCommandText(DBTable table)
        {
            var sql = new StringBuilder();

            Concat(sql, "INSERT INTO ", GetName(table.Name), "(");

            int index = 0;
            foreach (var column in table.Columns)
            {
                if (index > 0)
                {
                    Concat(sql, ',');
                }
                if (!column.IsPrimary)
                {
                    Concat(sql, GetName(column.Name));
                    index++;
                }
            }

            Concat(sql, ") OUTPUT INSERTED.", GetName(table.PrimaryKeyColumn.Name), " VALUES(");

            index = 0;
            foreach (var column in table.Columns)
            {
                if (index > 0)
                {
                    Concat(sql, ',');
                }
                if (!column.IsPrimary)
                {
                    Concat(sql, "@p", index);
                    index++;
                }
            }

            Concat(sql, ")");

            return sql.ToString();
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((SqlCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DBTable[] GetTableSchema(DbConnection connection)
        {
            var tables = new List<DBTable>();

            using (var tableSchema = connection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    var table = new DBTable(this);
                    table.Name = (string)tableRow["TABLE_NAME"];
                    tables.Add(table);
                }
            }

            using (var dataSet = new DataSet())
            {
                foreach (var table in tables)
                {
                    var query = string.Concat("SELECT TOP 0 * FROM [", table.Name, "]");
                    using (var dataAdapter = new SqlDataAdapter(query, (SqlConnection)connection))
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
                            OrderIndex = j,
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

                        column.NotNull = (string)columnRow["IS_NULLABLE"] == "NO";
                        var defaultValue = columnRow["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = defaultValue.Trim('(', ')', '\'');
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        if (columnRow["CHARACTER_MAXIMUM_LENGTH"] is int maximumLength)
                        {
                            column.Size = maximumLength;
                        }
                    }
                }
            }

            using (var primaryKeySchema = connection.GetSchema("IndexColumns"))
            {
                foreach (DataRow primaryKeyRow in primaryKeySchema.Rows)
                {
                    if ((byte)primaryKeyRow["KeyType"] == 56)
                    {
                        var tableName = (string)primaryKeyRow["table_name"];
                        var table = tables.Find(x => x.Name == tableName);

                        var columnName = (string)primaryKeyRow["column_name"];
                        var column = table.Columns.Find(x => x.Name == columnName);
                        column.IsPrimary = true;
                        table.PrimaryKeyColumn = column;
                    }
                }
            }

            return tables.ToArray();
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
                    sql.Insert(6, string.Concat(" TOP ", block[0]));
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
