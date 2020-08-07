using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data;
using System.Data.Common;
using System.Text;

namespace MyLibrary.DataBase.Firebird
{
    /// <summary>
    /// Модель БД "FireBird".
    /// </summary>
    public sealed class FireBirdProvider : DBProvider
    {
        public FireBirdProvider()
        {
            OpenBlock = CloseBlock = "\"";
        }

        public override void FillTableSchema(DbConnection dbConnection)
        {
            #region Tables
            using (DataTable tableSchema = dbConnection.GetSchema("Tables"))
            {
                foreach (DataRow tableRow in tableSchema.Rows)
                {
                    if ((short)tableRow["IS_SYSTEM_TABLE"] == 0)
                    {
                        DBTable table = new DBTable()
                        {
                            Name = (string)tableRow["TABLE_NAME"]
                        };
                        Tables.Add(table);
                    }
                }
            }
            #endregion

            using (DataSet dataSet = new DataSet())
            {
                foreach (DBTable table in Tables)
                {
                    string query = string.Concat("SELECT FIRST 0 * FROM \"", table.Name, "\"");
                    using (FbDataAdapter dataAdapter = new FbDataAdapter(query, (FbConnection)dbConnection))
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

            #region Columns
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
                            defaultValue = defaultValue.Remove(0, 8);
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        column.Size = (int)columnRow["COLUMN_SIZE"];
                        object description = columnRow["DESCRIPTION"];
                        if (description != DBNull.Value)
                        {
                            column.Description = (string)description;
                        }
                    }
                }
            }
            #endregion

            #region PrimaryKeys
            using (DataTable primaryKeySchema = dbConnection.GetSchema("PrimaryKeys"))
            {
                foreach (DataRow primaryKeyRow in primaryKeySchema.Rows)
                {
                    string tableName = (string)primaryKeyRow["TABLE_NAME"];
                    DBTable table = Tables[tableName];

                    string columnName = (string)primaryKeyRow["COLUMN_NAME"];
                    DBColumn column = table.Columns.Find(x => x.Name == columnName);
                    column.IsPrimary = true;
                    table.PrimaryKeyColumn = column;
                }
            }
            #endregion

            #region Indexes
            using (DataTable indexesSchema = dbConnection.GetSchema("Indexes"))
            {
                foreach (DataRow indexRow in indexesSchema.Rows)
                {
                    string tableName = (string)indexRow["TABLE_NAME"];
                    if (Tables.Contains(tableName))
                    {
                        DBTable table = Tables[tableName];
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
            using (DataTable foreignKeysSchema = dbConnection.GetSchema("ForeignKeys"))
            {
                foreach (DataRow foreignKeysRow in foreignKeysSchema.Rows)
                {
                    string tableName = (string)foreignKeysRow["TABLE_NAME"];
                    if (Tables.Contains(tableName))
                    {
                        DBTable table = Tables[tableName];
                        string indexName = (string)foreignKeysRow["INDEX_NAME"];
                        DBIndex index = table.Indexes.Find(x => x.Name == indexName);
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
            return string.Concat(base.GetInsertCommandText(table), " RETURNING ", GetShortName(table.PrimaryKeyColumn.Name));
        }
        public override DbParameter CreateParameter(string name, object value)
        {
            return new FbParameter(name, value);
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
            return string.Concat("SET STATISTICS INDEX ", GetShortName(index.Name));
        }
        public string GetActivateIndexCommandText(DBIndex index)
        {
            return string.Concat("ALTER INDEX ", GetShortName(index.Name), " ACTIVE");
        }
        public string GetDeactivateIndexCommandText(DBIndex index)
        {
            return string.Concat("ALTER INDEX ", GetShortName(index.Name), " INACTIVE");
        }

        private void PrepareBatchingCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            DBQueryStructureBlock block = query.Structure.Find(DBQueryStructureType.UpdateOrInsert);
            if (block != null)
            {
                #region UPDATE OR INSERT

                sql.Concat("UPDATE OR INSERT INTO ", GetShortName(query.Table.Name), '(');

                System.Collections.Generic.List<DBQueryStructureBlock> blockList = query.Structure.FindAll(DBQueryStructureType.Set);
                if (blockList.Count == 0)
                {
                    throw DBInternal.WrongUpdateCommandException();
                }

                for (int i = 0; i < blockList.Count; i++)
                {
                    DBQueryStructureBlock block1 = blockList[i];
                    if (i > 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetColumnName(block1[0]));
                }

                sql.Concat(")VALUES(");
                for (int i = 0; i < blockList.Count; i++)
                {
                    DBQueryStructureBlock block1 = blockList[i];
                    if (i > 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetParameter(block1[1], cQuery));
                }

                sql.Concat(")MATCHING(");
                for (int i = 0; i < block.Args.Length; i++)
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
            System.Collections.Generic.List<DBQueryStructureBlock> blockList = query.Structure.FindAll(DBQueryStructureType.Returning);
            if (blockList.Count > 0)
            {
                sql.Concat(" RETURNING ");
                for (int i = 0; i < blockList.Count; i++)
                {
                    DBQueryStructureBlock block = blockList[i];
                    for (int j = 0; j < block.Length; j++)
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
