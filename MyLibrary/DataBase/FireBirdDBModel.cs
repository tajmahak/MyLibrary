using FirebirdSql.Data.FirebirdClient;
using MyLibrary.Data;
using System;
using System.Data;
using System.Data.Common;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Модель БД "FireBird".
    /// </summary>
    public sealed class FireBirdDBModel : DBModelBase
    {
        public FireBirdDBModel()
        {
            OpenBlock = CloseBlock = "\"";
        }

        public override void FillTableSchema(DbConnection connection)
        {
            #region Tables
            using (var tableSchema = connection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    if ((short)tableRow["IS_SYSTEM_TABLE"] == 0)
                    {
                        var table = new DBTable(this)
                        {
                            Name = (string)tableRow["TABLE_NAME"]
                        };
                        Tables.Add(table);
                    }
                }
            }
            #endregion

            using (var dataSet = new DataSet())
            {
                foreach (var table in Tables)
                {
                    var query = string.Concat("SELECT FIRST 0 * FROM \"", table.Name, "\"");
                    using (var dataAdapter = new FbDataAdapter(query, (FbConnection)connection))
                    {
                        dataAdapter.Fill(dataSet, 0, 0, table.Name);
                    }
                }

                for (var i = 0; i < Tables.Count; i++)
                {
                    var tableRow = dataSet.Tables[i];
                    var table = Tables[i];
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

            #region Columns
            using (var columnSchema = connection.GetSchema("Columns"))
            {
                foreach (DataRow columnRow in columnSchema.Rows)
                {
                    var tableName = (string)columnRow["TABLE_NAME"];
                    var table = Tables[tableName];
                    if (table != null)
                    {
                        var columnName = (string)columnRow["COLUMN_NAME"];
                        var column = table.Columns.Find(x => x.Name == columnName);

                        column.NotNull = (bool)columnRow["IS_NULLABLE"] == false;
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
            #endregion

            #region PrimaryKeys
            using (var primaryKeySchema = connection.GetSchema("PrimaryKeys"))
            {
                foreach (DataRow primaryKeyRow in primaryKeySchema.Rows)
                {
                    var tableName = (string)primaryKeyRow["TABLE_NAME"];
                    var table = Tables[tableName];

                    var columnName = (string)primaryKeyRow["COLUMN_NAME"];
                    var column = table.Columns.Find(x => x.Name == columnName);
                    column.IsPrimary = true;
                    table.PrimaryKeyColumn = column;
                }
            }
            #endregion

            #region Indexes
            using (var indexesSchema = connection.GetSchema("Indexes"))
            {
                foreach (DataRow indexRow in indexesSchema.Rows)
                {
                    var tableName = (string)indexRow["TABLE_NAME"];
                    var table = Tables[tableName];
                    if (table != null)
                    {
                        table.Indexes.Add(new DBIndex(table)
                        {
                            Name = (string)indexRow["INDEX_NAME"],
                            IsActive = (short)indexRow["IS_INACTIVE"] == 0,
                            IsUnique = (short)indexRow["IS_UNIQUE"] == 1,
                            IsPrimary = (bool)indexRow["IS_PRIMARY"],
                        });
                    }
                }
            }
            #endregion

            #region ForeignKeys
            using (var foreignKeysSchema = connection.GetSchema("ForeignKeys"))
            {
                foreach (DataRow foreignKeysRow in foreignKeysSchema.Rows)
                {
                    var tableName = (string)foreignKeysRow["TABLE_NAME"];
                    var table = Tables[tableName];
                    if (table != null)
                    {
                        var indexName = (string)foreignKeysRow["INDEX_NAME"];
                        var index = table.Indexes.Find(x => x.Name == indexName);
                        if (index != null)
                        {
                            index.IsForeign = true;
                        }
                    }
                }
            }
            #endregion

            // Код для отображения DataTable в Excel
            //var schema = connection.GetSchema();
            //var str = new StringBuilder();
            //foreach (DataColumn column in schema.Columns)
            //{
            //    str.Append(column.ColumnName + "\t");
            //}
            //str.AppendLine();
            //foreach (DataColumn column in schema.Columns)
            //{
            //    str.Append(column.DataType.Name + "\t");
            //}
            //str.AppendLine();
            //foreach (DataRow row in schema.Rows)
            //{
            //    foreach (var value in row.ItemArray)
            //    {
            //        str.Append(value + "\t");
            //    }
            //    str.AppendLine();
            //}
            //var text = str.ToString();
        }
        protected override string GetInsertCommandText(DBTable table)
        {
            return string.Concat(base.GetInsertCommandText(table), " RETURNING ", GetName(table.PrimaryKeyColumn.Name));
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((FbCommand)command).Parameters.AddWithValue(name, value);
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

                var block = query.Structure.Find(DBQueryStructureType.Distinct);
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
                PrepareReturningBlock(sql, query);
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
            else
            {
                PrepareBatchingCommand(sql, query, cQuery);
            }

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }

        public string GetUpdateSelectivityIndexCommandText(DBIndex index)
        {
            return string.Concat("SET STATISTICS INDEX ", GetName(index.Name));
        }
        public string GetActivateIndexCommandText(DBIndex index)
        {
            return string.Concat("ALTER INDEX ", GetName(index.Name), " ACTIVE");
        }
        public string GetDeactivateIndexCommandText(DBIndex index)
        {
            return string.Concat("ALTER INDEX ", GetName(index.Name), " INACTIVE");
        }

        private void PrepareBatchingCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var block = query.Structure.Find(DBQueryStructureType.UpdateOrInsert);
            if (block != null)
            {
                #region UPDATE OR INSERT

                sql.Concat("UPDATE OR INSERT INTO ", GetName(query.Table.Name), '(');

                var blockList = query.Structure.FindAll(DBQueryStructureType.Set);
                if (blockList.Count == 0)
                {
                    throw DBInternal.WrongUpdateCommandException();
                }

                for (var i = 0; i < blockList.Count; i++)
                {
                    var block1 = blockList[i];
                    if (i > 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetColumnName(block1[0]));
                }

                sql.Concat(")VALUES(");
                for (var i = 0; i < blockList.Count; i++)
                {
                    var block1 = blockList[i];
                    if (i > 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetParameter(block1[1], cQuery));
                }

                sql.Concat(")MATCHING(");
                for (var i = 0; i < block.Args.Length; i++)
                {
                    if (i > 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetColumnName(block[i]));
                }
                sql.Concat(')');

                PrepareWhereBlock(sql, query, cQuery);

                #endregion
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        private void PrepareReturningBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.Structure.FindAll(DBQueryStructureType.Returning);
            if (blockList.Count > 0)
            {
                sql.Concat(" RETURNING ");
                for (var i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    for (var j = 0; j < block.Length; j++)
                    {
                        if (j > 0)
                        {
                            sql.Concat(',');
                        }
                        sql.Concat(GetColumnName(block[j]));
                    }
                }
            }
        }
    }
}
