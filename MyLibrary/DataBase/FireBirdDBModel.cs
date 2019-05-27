using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace MyLibrary.DataBase
{
    public class FireBirdDBModel : DBModelBase
    {
        public FireBirdDBModel()
        {
            OpenBlock = CloseBlock = '\"';
            ParameterPrefix = '@';
            InitializeFromDbConnection += FireBirdDBModel_InitializeFromDbConnection1;
            InitializeDefaultInsertCommand += FireBirdDBModel_InitializeDefaultInsertCommand;
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            return ((FbConnection)connection).CreateCommand();
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
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
            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareSelectCommand(sql, query, cQuery);

                block = FindBlock(query, DBQueryStructureTypeEnum.Distinct);
                if (block != null)
                {
                    sql.Insert(6, " DISTINCT");
                }

                block = FindBlock(query, DBQueryStructureTypeEnum.Skip);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" SKIP ", block[0]));
                }

                block = FindBlock(query, DBQueryStructureTypeEnum.First);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" FIRST ", block[0]));
                }

                PrepareJoinCommand(sql, query);

                PrepareUnionCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Insert)
            {
                PrepareInsertCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Update)
            {
                PrepareUpdateCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Delete)
            {
                PrepareDeleteCommand(sql, query);
            }
            else if (query.Type == DBQueryTypeEnum.UpdateOrInsert)
            {
                #region UPDATE OR INSERT

                Add(sql, "UPDATE OR INSERT INTO ", GetName(query.Table.Name));

                blockList = FindBlockList(query, DBQueryStructureTypeEnum.Set);
                if (blockList.Count == 0)
                {
                    throw DBInternal.InadequateUpdateCommandException();
                }

                Add(sql, '(');
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        Add(sql, ',');
                    }
                    Add(sql, GetColumnName(block[0]));
                }

                Add(sql, ")VALUES(");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        Add(sql, ',');
                    }
                    Add(sql, AddParameter(block[1], cQuery));
                }

                Add(sql, ')');

                blockList = FindBlockList(query, DBQueryStructureTypeEnum.Matching);
                if (blockList.Count > 0)
                {
                    Add(sql, " MATCHING(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        block = blockList[i];
                        for (int j = 0; j < block.Length; j++)
                        {
                            if (j > 0)
                            {
                                Add(sql, ',');
                            }
                            Add(sql, GetColumnName(block[j]));
                        }
                    }
                    Add(sql, ')');
                }

                #endregion
            }

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            #region RETURNING ...

            blockList = FindBlockList(query, DBQueryStructureTypeEnum.Returning);
            if (blockList.Count > 0)
            {
                Add(sql, " RETURNING ");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    for (int j = 0; j < block.Length; j++)
                    {
                        if (i > 0)
                        {
                            Add(sql, ',');
                        }
                        Add(sql, GetColumnName(block[j]));
                    }
                }
            }

            #endregion

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }

        private void FireBirdDBModel_InitializeFromDbConnection1(object sender, InitializeFromDbConnectionEventArgs e)
        {
            //!!! думаю можно упростить

            var connection = (FbConnection)e.DbConnection;

            var tableNames = new List<string>();
            #region Получение названий таблиц

            using (var dataTables = connection.GetSchema("Tables"))
            {
                foreach (DataRow table in dataTables.Rows)
                {
                    if ((short)table["IS_SYSTEM_TABLE"] == 0)
                    {
                        tableNames.Add((string)table["TABLE_Name"]);
                    }
                }

                dataTables.Clear();
            }

            #endregion

            var tables = new DBTable[tableNames.Count];

            using (var ds = new DataSet())
            using (var columnsInfo = connection.GetSchema("Columns"))
            using (var primaryKeysInfo = connection.GetSchema("PrimaryKeys"))
            {
                #region Подготовка ДатаСета

                foreach (var tableName in tableNames)
                {
                    using (var dataAdapter = new FbDataAdapter(string.Format("SELECT FIRST 0 * FROM \"{0}\"", tableName), connection))
                    {
                        dataAdapter.Fill(ds, 0, 0, tableName);
                    }
                }

                #endregion
                #region Добавление информации для таблиц

                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    DataTable dataTable = ds.Tables[i];
                    DBTable table = new DBTable(this, dataTable.TableName);
                    DBColumn[] columns = new DBColumn[dataTable.Columns.Count];
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        DataColumn dataColumn = dataTable.Columns[j];
                        DBColumn column = new DBColumn(table);
                        #region Добавление информации для столбцов

                        var columnInfo = columnsInfo.Select("TABLE_Name = '" + dataTable.TableName + "' AND COLUMN_Name = '" + dataColumn.ColumnName + "'")[0];
                        var primaryKeyInfo = primaryKeysInfo.Select("TABLE_Name = '" + dataTable.TableName + "' AND COLUMN_Name = '" + dataColumn.ColumnName + "'");

                        column.Name = dataColumn.ColumnName;
                        column.DataType = dataColumn.DataType;
                        column.AllowDBNull = (bool)columnInfo["IS_NULLABLE"];

                        var columnDescription = columnInfo["DESCRIPTION"];
                        if (columnDescription != DBNull.Value)
                        {
                            column.Comment = (string)columnDescription;
                        }

                        if (primaryKeyInfo.Length > 0)
                        {
                            column.IsPrimary = true;
                        }

                        var defaultValue = columnInfo["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = defaultValue.Remove(0, 8);
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        else
                        {
                            column.DefaultValue = DBNull.Value;
                        }

                        if (column.DataType == typeof(string))
                        {
                            column.MaxTextLength = (int)columnInfo["COLUMN_SIZE"];
                        }

                        #endregion
                        columns[j] = column;
                    }
                    table.AddColumns(columns);
                    tables[i] = table;
                }

                #endregion
            }

            e.Tables = tables;
        }
        private void FireBirdDBModel_InitializeDefaultInsertCommand(object sender, InitializeDefaultInsertCommandEventArgs e)
        {
            e.DefaultInsertCommand = string.Concat(GetInsertCommand(e.Table), " RETURNING ", GetName(e.Table.Columns[e.Table.PrimaryKeyIndex].Name));
        }
    }
}
