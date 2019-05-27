using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace MyLibrary.DataBase
{
    public class SQLiteDBModel : DBModelBase
    {
        public SQLiteDBModel()
        {
            OpenBlock = '[';
            CloseBlock = ']';
            ParameterPrefix = '@';

            InitializeFromDbConnection += SQLiteDBModel_InitializeFromDbConnection;
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            return ((SQLiteConnection)connection).CreateCommand();
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
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((SQLiteCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0)
        {
            var cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            var sql = new StringBuilder();
            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareSelectCommand(sql, query, cQuery);
                PrepareJoinCommand(sql, query);
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

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }

        private void SQLiteDBModel_InitializeFromDbConnection(object sender, InitializeFromDbConnectionEventArgs e)
        {
            var connection = (SQLiteConnection)e.DbConnection;

            var tableNames = new List<string>();
            #region Получение названий таблиц

            using (var dataTables = connection.GetSchema("Tables"))
            {
                foreach (DataRow table in dataTables.Rows)
                {
                    if ((string)table["TABLE_TYPE"] != "SYSTEM_TABLE")
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
            {
                #region Подготовка ДатаСета

                foreach (var tableName in tableNames)
                {
                    using (var dataAdapter = new SQLiteDataAdapter(string.Format("SELECT * FROM \"{0}\" LIMIT 0", tableName), connection))
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

                        column.Name = dataColumn.ColumnName;
                        column.DataType = dataColumn.DataType;
                        column.AllowDBNull = (bool)columnInfo["IS_NULLABLE"];

                        var columnDescription = columnInfo["DESCRIPTION"];
                        if (columnDescription != DBNull.Value)
                        {
                            column.Comment = (string)columnDescription;
                        }

                        column.IsPrimary = (bool)columnInfo["PRIMARY_KEY"];

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
                            column.MaxTextLength = (int)columnInfo["CHARACTER_MAXIMUM_LENGTH"];
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
    }
}
