using MyLibrary.Data;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "Microsoft SQL Server"
    /// </summary>
    public sealed class MSSQLServerProvider : DBProvider
    {
        public MSSQLServerProvider()
        {
            OpenBlock = "[";
            CloseBlock = "]";
        }

        protected override string GetInsertCommandText(DBTable table)
        {
            StringBuilder sql = new StringBuilder();

            sql.Concat("INSERT INTO ", GetShortName(table.Name), "(");

            int index = 0;
            foreach (DBColumn column in table.Columns)
            {
                if (index > 0)
                {
                    sql.Concat(',');
                }
                if (!column.IsPrimary)
                {
                    sql.Concat(GetShortName(column.Name));
                    index++;
                }
            }

            sql.Concat(") OUTPUT INSERTED.", GetShortName(table.PrimaryKeyColumn.Name), " VALUES(");

            index = 0;
            foreach (DBColumn column in table.Columns)
            {
                if (index > 0)
                {
                    sql.Concat(',');
                }
                if (!column.IsPrimary)
                {
                    sql.Concat("@p", index);
                    index++;
                }
            }

            sql.Concat(")");

            return sql.ToString();
        }
        public override DbParameter CreateParameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }
        public override void FillTableSchema(DbConnection dbConnection)
        {
            using (DataTable tableSchema = dbConnection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    DBTable table = new DBTable()
                    {
                        Name = (string)tableRow["TABLE_NAME"]
                    };
                    Tables.Add(table);
                }
            }

            using (DataSet dataSet = new DataSet())
            {
                foreach (DBTable table in Tables)
                {
                    string query = string.Concat("SELECT TOP 0 * FROM [", table.Name, "]");
                    using (SqlDataAdapter dataAdapter = new SqlDataAdapter(query, (SqlConnection)dbConnection))
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

                        column.NotNull = (string)columnRow["IS_NULLABLE"] == "NO";
                        string defaultValue = columnRow["COLUMN_DEFAULT"].ToString();
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

            using (DataTable primaryKeySchema = dbConnection.GetSchema("IndexColumns"))
            {
                foreach (DataRow primaryKeyRow in primaryKeySchema.Rows)
                {
                    if ((byte)primaryKeyRow["KeyType"] == 56)
                    {
                        string tableName = (string)primaryKeyRow["table_name"];
                        DBTable table = Tables[tableName];

                        string columnName = (string)primaryKeyRow["column_name"];
                        DBColumn column = table.Columns.Find(x => x.Name == columnName);
                        column.IsPrimary = true;
                        table.PrimaryKeyColumn = column;
                    }
                }
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
