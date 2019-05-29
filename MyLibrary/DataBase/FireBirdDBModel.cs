using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
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
        protected override string GetInsertCommandText(DBTable table)
        {
            return string.Concat(base.GetInsertCommandText(table), " RETURNING ", GetName(table.Columns[table.PrimaryKeyColumn.Index].Name));
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
            else if (query.Type == DBQueryType.UpdateOrInsert)
            {
                #region UPDATE OR INSERT

                AddText(sql, "UPDATE OR INSERT INTO ", GetName(query.Table.Name));

                blockList = FindBlockList(query, DBQueryStructureType.Set);
                if (blockList.Count == 0)
                {
                    throw DBInternal.InadequateUpdateCommandException();
                }

                AddText(sql, '(');
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        AddText(sql, ',');
                    }
                    AddText(sql, GetColumnName(block[0]));
                }

                AddText(sql, ")VALUES(");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        AddText(sql, ',');
                    }
                    AddText(sql, AddParameter(block[1], cQuery));
                }

                AddText(sql, ')');

                blockList = FindBlockList(query, DBQueryStructureType.Matching);
                if (blockList.Count > 0)
                {
                    AddText(sql, " MATCHING(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        block = blockList[i];
                        for (int j = 0; j < block.Length; j++)
                        {
                            if (j > 0)
                            {
                                AddText(sql, ',');
                            }
                            AddText(sql, GetColumnName(block[j]));
                        }
                    }
                    AddText(sql, ')');
                }

                #endregion
            }

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryType.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            #region RETURNING ...

            blockList = FindBlockList(query, DBQueryStructureType.Returning);
            if (blockList.Count > 0)
            {
                AddText(sql, " RETURNING ");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    for (int j = 0; j < block.Length; j++)
                    {
                        if (j > 0)
                        {
                            AddText(sql, ',');
                        }
                        AddText(sql, GetColumnName(block[j]));
                    }
                }
            }

            #endregion

            PrepareUnionCommand(sql, query, cQuery);

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }
    }
}
