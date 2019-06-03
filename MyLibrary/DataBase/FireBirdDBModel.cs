﻿using FirebirdSql.Data.FirebirdClient;
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

            if (query.CommandType == DBCommandType.Select)
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
            else if (query.CommandType == DBCommandType.Insert)
            {
                PrepareInsertBlock(sql, query, cQuery);
                PrepareWhereBlock(sql, query, cQuery);
                PrepareReturningBlock(sql, query);
            }
            else if (query.CommandType == DBCommandType.Update)
            {
                PrepareUpdateBlock(sql, query, cQuery);
                PrepareWhereBlock(sql, query, cQuery);
            }
            else if (query.CommandType == DBCommandType.Delete)
            {
                PrepareDeleteBlock(sql, query);
                PrepareWhereBlock(sql, query, cQuery);
            }
            else if (query.CommandType == DBCommandType.UpdateOrInsert)
            {
                PrepareUpdateOrInsertCommand(sql, query, cQuery);
                PrepareMatchingCommand(sql, query);
                PrepareWhereBlock(sql, query, cQuery);
            }

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }

        private void PrepareUpdateOrInsertCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            AddText(sql, "UPDATE OR INSERT INTO ", GetName(query.Table.Name));

            var blockList = query.FindBlocks(DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongUpdateCommandException();
            }

            AddText(sql, '(');
            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    AddText(sql, ',');
                }
                AddText(sql, GetColumnName(block[0]));
            }

            AddText(sql, ")VALUES(");
            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    AddText(sql, ',');
                }
                AddText(sql, GetParameter(block[1], cQuery));
            }

            AddText(sql, ')');
        }
        private void PrepareMatchingCommand(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.FindBlocks(DBQueryStructureType.Matching);
            if (blockList.Count > 0)
            {
                AddText(sql, " MATCHING(");
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
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
        }
        private void PrepareReturningBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.FindBlocks(DBQueryStructureType.Returning);
            if (blockList.Count > 0)
            {
                AddText(sql, " RETURNING ");
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
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
        }
    }
}
