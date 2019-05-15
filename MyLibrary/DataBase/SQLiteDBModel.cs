using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using MyLibrary.DataBase.Orm;

namespace MyLibrary.DataBase
{
    public class SQLiteDBModel : DBModelBase
    {
        public override void Initialize(DbConnection connection)
        {
            InitializeDBModel((SQLiteConnection)connection);
            InitializeDefaultCommands();
            IsInitialized = true;
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            command.ExecuteScalar();
            var connection = (SQLiteConnection)command.Connection;
            return connection.LastInsertRowId;
        }
        public override void AddParameter(DbCommand command, string name, object value)
        {
            ((SQLiteCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DbCommand BuildCommand(DbConnection connection, DBQuery query)
        {
            var command = (SQLiteCommand)connection.CreateCommand();
            var builder = new SqlBuilder(this, command);
            command.CommandText = builder.Build(query);
            return command;
        }

        private void InitializeDBModel(SQLiteConnection connection)
        {
            var tableNames = new List<string>();
            #region Получение названий таблиц

            using (var dataTables = connection.GetSchema("Tables"))
            {
                foreach (DataRow table in dataTables.Rows)
                    if ((string)table["TABLE_TYPE"] != "SYSTEM_TABLE")
                        tableNames.Add((string)table["TABLE_Name"]);
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
                            column.Comment = (string)columnDescription;

                        column.IsPrimary = (bool)columnInfo["PRIMARY_KEY"];

                        var defaultValue = columnInfo["COLUMN_DEFAULT"].ToString();
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = defaultValue.Remove(0, 8);
                            column.DefaultValue = Convert.ChangeType(defaultValue, column.DataType);
                        }
                        else column.DefaultValue = DBNull.Value;

                        if (column.DataType == typeof(string))
                            column.MaxTextLength = (int)columnInfo["CHARACTER_MAXIMUM_LENGTH"];

                        #endregion
                        columns[j] = column;
                    }
                    table.AddColumns(columns);
                    tables[i] = table;
                }

                #endregion
            }

            #region Подготовка значений

            base.Tables = tables;
            for (int i = 0; i < tables.Length; i++)
            {
                var table = tables[i];
                base.TablesDict.Add(table.Name, table);

                for (int j = 0; j < table.Columns.Length; j++)
                {
                    var column = table.Columns[j];
                    string longName = string.Concat(table.Name, '.', column.Name);
                    base.ColumnsDict.Add(longName, column);
                }
            }

            #endregion
        }
        private void InitializeDefaultCommands()
        {
            #region SELECT
            for (int i = 0; i < Tables.Length; i++)
            {
                var table = Tables[i];
                var str = new StringBuilder();
                str.Append("SELECT ");
                str.Append(GetName(table.Name));
                str.Append(".* FROM ");
                str.Append(GetName(table.Name));

                DefaultSelectCommandsDict.Add(table, str.ToString());
            }
            #endregion
            #region INSERT
            for (int i = 0; i < Tables.Length; i++)
            {
                var table = Tables[i];
                var str = new StringBuilder();
                str.Append("INSERT INTO ");
                str.Append(GetName(table.Name));
                str.Append(" VALUES(");

                int index = 0;
                for (int j = 0; j < table.Columns.Length; j++)
                {
                    if (j > 0)
                        str.Append(',');

                    if (table.Columns[j].IsPrimary)
                        str.Append("NULL");
                    else
                    {
                        str.Append(String.Concat("@p", index));
                        index++;
                    }
                }

                str.Append(")");
                DefaultInsertCommandsDict.Add(table, str.ToString());
            }
            #endregion
            #region UPDATE
            for (int i = 0; i < Tables.Length; i++)
            {
                var table = Tables[i];
                var str = new StringBuilder();

                str.Append("UPDATE ");
                str.Append(GetName(table.Name));
                str.Append(" SET ");
                int index = 0;
                for (int j = 0; j < table.Columns.Length; j++)
                {
                    var column = table.Columns[j];
                    if (column.IsPrimary)
                        continue;

                    if (index != 0)
                        str.Append(",");

                    str.Append(GetName(column.Name));
                    str.Append("=@p");
                    str.Append(index++);
                }
                str.Append(" WHERE ");
                str.Append(GetName(table.Columns[table.PrimaryKeyIndex].Name));
                str.Append("=@id");
                DefaultUpdateCommandsDict.Add(table, str.ToString());
            }
            #endregion
            #region DELETE
            for (int i = 0; i < Tables.Length; i++)
            {
                var table = Tables[i];
                var str = new StringBuilder();

                str.Append("DELETE FROM ");
                str.Append(GetName(table.Name));
                str.Append(" WHERE ");
                for (int j = 0; j < table.Columns.Length; j++)
                {
                    var column = table.Columns[j];
                    if (column.IsPrimary)
                    {
                        str.Append(GetName(column.Name));
                        break;
                    }
                }
                str.Append("=@id");

                DefaultDeleteCommandsDict.Add(table, str.ToString());
            }
            #endregion
        }

        private static string GetFullName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat('[', split[0], "].[", split[1], ']');
        }
        private static string GetName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat('[', split[0], ']');
        }
        private static string GetColumnName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat('[', split[1], ']');
        }

        #region [class] Конструктор SQL-команд

        private class SqlBuilder
        {
            private SQLiteDBModel _model;
            private SQLiteCommand _command;
            private int _parameterCounter;

            public SqlBuilder(SQLiteDBModel model, SQLiteCommand command)
            {
                _model = model;
                _command = command;
            }

            public string Build(DBQuery query)
            {
                var sql = new StringBuilder();

                List<object[]> blockList;
                object[] block;
                string[] args;
                int index = 0;

                if (query.QueryType == DBQueryTypeEnum.Select)
                {
                    #region SELECT [...] ... FROM ...

                    blockList = FindBlockList(query, x => x.StartsWith("Select"));
                    if (blockList.Count == 0)
                    {
                        sql.Append(_model.DefaultSelectCommandsDict[query.Table]);
                    }
                    else
                    {
                        sql.Append("SELECT ");
                        #region

                        index = 0;
                        for (int i = 0; i < blockList.Count; i++)
                        {
                            block = blockList[i];
                            switch ((string)block[0])
                            {
                                case "Select":
                                    #region
                                    args = (string[])block[1];
                                    if (args.Length == 0)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append('*');
                                        index++;
                                    }
                                    else
                                    {
                                        for (int j = 0; j < args.Length; j++)
                                        {
                                            if (index > 0)
                                                sql.Append(',');

                                            var paramCol = (string)args[j];
                                            if (paramCol.Contains("."))
                                            {
                                                // Столбец
                                                sql.Append(GetFullName(paramCol));
                                            }
                                            else
                                            {
                                                // Таблица
                                                sql.Append(GetName(paramCol));
                                                sql.Append(".*");
                                            }
                                            index++;
                                        }
                                    }
                                    break;
                                #endregion
                                case "SelectAs":
                                    #region
                                    if (index > 0)
                                        sql.Append(',');
                                    sql.Append(GetName(block[1]));
                                    sql.Append(".");
                                    sql.Append(GetColumnName(block[2]));
                                    index++;
                                    break;
                                #endregion
                                case "SelectSum":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("SUM(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(')');
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectSumAs":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j += 2)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("SUM(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(") AS ");
                                        sql.Append(GetName(args[j + 1]));
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectMax":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("MAX(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(')');
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectMaxAs":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j += 2)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("MAX(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(") AS ");
                                        sql.Append(GetName(args[j + 1]));
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectMin":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("MIN(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(')');
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectMinAs":
                                    #region
                                    args = (string[])block[1];
                                    for (int j = 0; j < args.Length; j += 2)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("MIN(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(") AS ");
                                        sql.Append(GetName(args[j + 1]));
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "SelectCount":
                                    #region
                                    args = (string[])block[1];
                                    if (args.Length == 0)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("COUNT(*)");
                                        index++;
                                    }
                                    else
                                    {
                                        for (int j = 0; j < args.Length; j++)
                                        {
                                            if (index > 0)
                                                sql.Append(',');
                                            sql.Append("COUNT(");
                                            sql.Append(GetFullName(args[j]));
                                            sql.Append(')');
                                            index++;
                                        }
                                    }
                                    break;
                                    #endregion
                            }
                        }

                        #endregion
                        sql.Append(" FROM ");
                        sql.Append(GetName(query.Table.Name));
                    }

                    block = FindBlock(query, x => x == "Distinct");
                    if (block != null)
                        sql.Insert(6, " DISTINCT");

                    block = FindBlock(query, x => x == "Skip");
                    if (block != null)
                        sql.Insert(6, string.Concat(" SKIP ", block[1]));

                    block = FindBlock(query, x => x == "First");
                    if (block != null)
                        sql.Insert(6, string.Concat(" FIRST ", block[1]));

                    #endregion
                    #region JOIN

                    foreach (var item in query.Structure)
                    {
                        switch ((string)item[0])
                        {
                            case "InnerJoin":
                                #region
                                sql.Append(" INNER JOIN ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetFullName(item[1]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[2]));
                                break;
                            #endregion
                            case "LeftOuterJoin":
                                #region
                                sql.Append(" LEFT OUTER JOIN ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetFullName(item[1]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[2]));
                                break;
                            #endregion
                            case "RightOuterJoin":
                                #region
                                sql.Append(" RIGHT OUTER JOIN ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetFullName(item[1]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[2]));
                                break;
                            #endregion
                            case "FullOuterJoin":
                                #region
                                sql.Append(" FULL OUTER JOIN ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetFullName(item[1]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[2]));
                                break;
                            #endregion
                            case "InnerJoinAs":
                                #region
                                sql.Append(" INNER JOIN ");
                                sql.Append(GetName(item[2]));
                                sql.Append(" AS ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetName(item[1]));
                                sql.Append(".");
                                sql.Append(GetColumnName(item[2]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[3]));
                                break;
                            #endregion
                            case "LeftOuterJoinAs":
                                #region
                                sql.Append(" LEFT OUTER JOIN ");
                                sql.Append(GetName(item[2]));
                                sql.Append(" AS ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetName(item[1]));
                                sql.Append(".");
                                sql.Append(GetColumnName(item[2]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[3]));
                                break;
                            #endregion
                            case "RightOuterJoinAs":
                                #region
                                sql.Append(" RIGHT OUTER JOIN ");
                                sql.Append(GetName(item[2]));
                                sql.Append(" AS ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetName(item[1]));
                                sql.Append(".");
                                sql.Append(GetColumnName(item[2]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[3]));
                                break;
                            #endregion
                            case "FullOuterJoinAs":
                                #region
                                sql.Append(" FULL OUTER JOIN ");
                                sql.Append(GetName(item[2]));
                                sql.Append(" AS ");
                                sql.Append(GetName(item[1]));
                                sql.Append(" ON ");
                                sql.Append(GetName(item[1]));
                                sql.Append(".");
                                sql.Append(GetColumnName(item[2]));
                                sql.Append('=');
                                sql.Append(GetFullName(item[3]));
                                break;
                                #endregion
                        }
                    }

                    #endregion
                }
                else if (query.QueryType == DBQueryTypeEnum.Insert)
                {
                    #region INSERT INTO ...

                    sql.Append("INSERT INTO ");
                    sql.Append(GetName(query.Table.Name));

                    blockList = query.Structure.FindAll(x => ((string)x[0]) == "Set");
                    if (blockList.Count == 0)
                        throw DBInternal.InadequateInsertCommandException();

                    sql.Append("(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(',');
                        sql.Append(GetColumnName(blockList[i][1]));
                    }
                    sql.Append(")VALUES(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(',');
                        sql.Append(AddParameter(blockList[i][2]));
                    }
                    sql.Append(")");

                    #endregion
                }
                else if (query.QueryType == DBQueryTypeEnum.Update)
                {
                    #region UPDATE ... SET ...

                    sql.Append("UPDATE ");
                    sql.Append(GetName(query.Table.Name));
                    sql.Append(" SET ");

                    blockList = query.Structure.FindAll(x => ((string)x[0]) == "Set");
                    if (blockList.Count == 0)
                        throw DBInternal.InadequateUpdateCommandException();
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(',');
                        sql.Append(GetFullName(blockList[i][1]));
                        sql.Append('=');
                        sql.Append(AddParameter(blockList[i][2]));
                    }

                    #endregion
                }
                else if (query.QueryType == DBQueryTypeEnum.Delete)
                {
                    #region DELETE FROM ...

                    sql.Append("DELETE FROM ");
                    sql.Append(GetName(query.Table.Name));

                    #endregion
                }
                else if (query.QueryType == DBQueryTypeEnum.UpdateOrInsert)
                {
                    #region UPDATE OR INSERT

                    sql.Append("UPDATE OR INSERT INTO ");
                    sql.Append(GetName(query.Table.Name));

                    blockList = query.Structure.FindAll(x => ((string)x[0]) == "Set");
                    if (blockList.Count == 0)
                        throw DBInternal.InadequateUpdateCommandException();

                    sql.Append("(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(',');
                        sql.Append(GetColumnName(blockList[i][1]));
                    }

                    sql.Append(")VALUES(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(',');
                        sql.Append(AddParameter(blockList[i][2]));
                    }

                    sql.Append(")");

                    blockList = FindBlockList(query, x => x == "Matching");
                    if (blockList.Count > 0)
                    {
                        sql.Append(" MATCHING(");
                        index = 0;
                        for (int i = 0; i < blockList.Count; i++)
                        {
                            block = blockList[i];
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                    sql.Append(',');
                                sql.Append(GetColumnName(args[j]));
                                index++;
                            }
                        }
                        sql.Append(")");
                    }

                    #endregion
                }
                else if (query.QueryType == DBQueryTypeEnum.Sql)
                {
                    #region SQL-команда

                    block = FindBlock(query, x => x == "Sql");
                    sql.Append(block[1]);
                    index = 0;
                    foreach (var param in (object[])block[2])
                    {
                        var paramName = "@p" + index++;
                        _model.AddParameter(_command, paramName, param);
                    }

                    #endregion
                }

                #region WHERE ...

                blockList = FindBlockList(query, x => x.Contains("Where") || x == "Or" || x == "Not" || x == "(" || x == ")");
                if (blockList.Count > 0)
                {
                    sql.Append(" WHERE");
                    bool needPastePredicate = false;
                    string prevBlockName = null;
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        block = blockList[i];
                        var blockName = (string)block[0];
                        if (i > 0)
                            prevBlockName = (string)blockList[i - 1][0];

                        #region AND,OR,NOT,(,)

                        if (blockName == "(")
                        {
                            sql.Append(" (");
                            continue;
                        }
                        else if (blockName == ")")
                        {
                            sql.Append(" )");
                            continue;
                        }
                        else if (blockName == "Not")
                        {
                            // пропуск, поскольку эта команда является частью следующей команды
                            continue;
                        }
                        else if (needPastePredicate)
                        {
                            needPastePredicate = false;
                            if (blockName == "Or")
                            {
                                sql.Append(" OR");
                                continue;
                            }
                            else
                            {
                                sql.Append(" AND");
                            }
                        }
                        #endregion

                        switch (blockName)
                        {
                            case "Where_expression":
                                #region
                                sql.Append(' ');
                                sql.Append(ParseExpression((Expression)block[1], false).Text);
                                break;
                            #endregion
                            case "Where":
                                #region
                                block[3] = block[3] ?? DBNull.Value;

                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));

                                if ((block[3] is DBNull) && ((string)block[2]) == "=")
                                    sql.Append(" IS NULL");
                                else if (block[3] is DBNull && ((string)block[2]) == "<>")
                                    sql.Append(" IS NOT NULL");
                                else
                                {
                                    sql.Append(block[2]); // оператор сравнения
                                    sql.Append(AddParameter(block[3]));
                                }
                                break;
                            #endregion
                            case "WhereBetween":
                                #region
                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" BETWEEN ");
                                sql.Append(AddParameter(block[2]));
                                sql.Append(" AND ");
                                sql.Append(AddParameter(block[3]));
                                break;
                            #endregion
                            case "WhereUpper":
                                #region
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" UPPER(");
                                sql.Append(GetFullName(block[1]));
                                sql.Append(")=");
                                sql.Append(AddParameter(block[2]));
                                break;
                            #endregion
                            case "WhereContaining":
                                #region
                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" CONTAINING ");
                                sql.Append(AddParameter(block[2]));
                                break;
                            #endregion
                            case "WhereContainingUpper":
                                #region
                                sql.Append(" UPPER(");
                                sql.Append(GetFullName(block[1]));
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(") CONTAINING ");
                                sql.Append(AddParameter(block[2]));
                                break;
                            #endregion
                            case "WhereLike":
                                #region
                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" LIKE \'");
                                sql.Append(block[2]);
                                sql.Append('\'');
                                break;
                            #endregion
                            case "WhereLikeUpper":
                                #region
                                sql.Append(" UPPER(");
                                sql.Append(GetFullName(block[1]));
                                sql.Append(')');
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" LIKE '");
                                sql.Append(block[2]);
                                sql.Append('\'');
                                break;
                            #endregion
                            case "WhereIn_command":
                                #region
                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));
                                sql.Append(" IN (");
                                Build((DBQuery)block[2]);
                                sql.Append(')');
                                break;
                            #endregion
                            case "WhereIn_values":
                                #region
                                sql.Append(' ');
                                sql.Append(GetFullName(block[1]));
                                if (prevBlockName == "Not")
                                    sql.Append(" NOT");
                                sql.Append(" IN (");
                                #region Добавление списка значений

                                var values = (object[])block[2];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    if (j > 0)
                                        sql.Append(',');

                                    var value = values[j];
                                    if (value.GetType().IsPrimitive)
                                    {
                                        sql.Append(value);
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                }

                                #endregion
                                sql.Append(')');
                                break;
                                #endregion
                        }
                        needPastePredicate = true;
                    }
                }

                #endregion
                if (query.QueryType == DBQueryTypeEnum.Select)
                {
                    #region GROUP BY ...

                    blockList = FindBlockList(query, x => x == "GroupBy");
                    if (blockList.Count > 0)
                    {
                        sql.Append(" GROUP BY ");
                        index = 0;
                        for (int i = 0; i < blockList.Count; i++)
                        {
                            block = blockList[i];
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                    sql.Append(',');
                                sql.Append(GetFullName(args[j]));
                                index++;
                            }
                        }
                    }

                    #endregion
                    #region ORDER BY ...

                    blockList = FindBlockList(query, x => x.StartsWith("OrderBy"));
                    if (blockList.Count > 0)
                    {
                        sql.Append(" ORDER BY ");
                        index = 0;
                        for (int i = 0; i < blockList.Count; i++)
                        {
                            block = blockList[i];
                            args = (string[])block[1];
                            switch ((string)block[0])
                            {
                                case "OrderBy":
                                    #region
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append(GetFullName(args[j]));
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "OrderByDesc":
                                    #region
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(" DESC");
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "OrderByUpper":
                                    #region
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("UPPER(");
                                        sql.Append(GetFullName(args[j]));
                                        sql.Append(")");
                                        index++;
                                    }
                                    break;
                                #endregion
                                case "OrderByUpperDesc":
                                    #region
                                    for (int j = 0; j < args.Length; j++)
                                    {
                                        if (index > 0)
                                            sql.Append(',');
                                        sql.Append("UPPER(");
                                        sql.Append(GetFullName(block[1]));
                                        sql.Append(") DESC");
                                        index++;
                                    }
                                    break;
                                    #endregion
                            }
                        }
                    }

                    #endregion
                }

                #region RETURNING ...

                blockList = FindBlockList(query, x => x == "Returning");
                if (blockList.Count > 0)
                {
                    sql.Append(" RETURNING ");
                    index = 0;
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        block = blockList[i];
                        args = (string[])block[1];
                        for (int j = 0; j < args.Length; j++)
                        {
                            if (index > 0)
                                sql.Append(',');
                            sql.Append(GetColumnName(args[j]));
                            index++;
                        }
                    }
                }

                #endregion

                return sql.ToString();
            }
            private ExpressionInfo ParseExpression(Expression exp, bool parseValue)
            {
                var info = new ExpressionInfo();
                var str = new StringBuilder();

                if (exp is BinaryExpression)
                {
                    #region

                    var binaryExpression = exp as BinaryExpression;
                    str.Append('(');
                    str.Append(ParseExpression(binaryExpression.Left, false).Text);

                    var result = ParseExpression(binaryExpression.Right, false).Text;
                    if (result != null)
                    {
                        string @operator;
                        #region Выбор оператора

                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.Or:
                            case ExpressionType.OrElse:
                                @operator = " OR "; break;
                            case ExpressionType.And:
                            case ExpressionType.AndAlso:
                                @operator = " AND "; break;
                            case ExpressionType.Equal:
                                @operator = "="; break;
                            case ExpressionType.NotEqual:
                                @operator = "<>"; break;
                            case ExpressionType.LessThan:
                                @operator = "<"; break;
                            case ExpressionType.LessThanOrEqual:
                                @operator = "<="; break;
                            case ExpressionType.GreaterThan:
                                @operator = ">"; break;
                            case ExpressionType.GreaterThanOrEqual:
                                @operator = ">="; break;

                            default: throw DBInternal.UnsupportedCommandContextException();
                        }

                        #endregion
                        str.Append(@operator);
                        str.Append(result);
                    }
                    else
                    {
                        #region IS [NOT] NULL

                        str.Append(" IS");
                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.Equal:
                                break;
                            case ExpressionType.NotEqual:
                                str.Append(" NOT"); break;
                            default: throw DBInternal.UnsupportedCommandContextException();
                        }
                        str.Append(" NULL");

                        #endregion
                    }
                    str.Append(')');

                    #endregion
                }
                else if (exp is MemberExpression)
                {
                    #region

                    var memberExpression = exp as MemberExpression;

                    if (memberExpression.Expression is ParameterExpression)
                    {
                        var custAttr = memberExpression.Member.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                        var attr = (DBOrmColumnAttribute)custAttr[0];
                        str.Append(GetFullName(attr.ColumnName));
                    }
                    else if (memberExpression.Member is PropertyInfo)
                    {
                        var propertyInfo = memberExpression.Member as PropertyInfo;

                        object value;
                        if (memberExpression.Expression != null)
                        {
                            var innerInfo = ParseExpression(memberExpression.Expression, true);
                            value = propertyInfo.GetValue(innerInfo.Value, null);
                        }
                        else value = propertyInfo.GetValue(null, null);

                        if (parseValue)
                        {
                            info.Value = value;
                            return info;
                        }
                        else str.Append(AddParameter(value));
                    }
                    else if (memberExpression.Member is FieldInfo)
                    {
                        var fieldInfo = memberExpression.Member as FieldInfo;
                        var constantExpression = memberExpression.Expression as ConstantExpression;
                        var value = fieldInfo.GetValue(constantExpression.Value);

                        if (parseValue)
                        {
                            info.Value = value;
                            return info;
                        }
                        else str.Append(AddParameter(value));
                    }
                    else throw DBInternal.UnsupportedCommandContextException();

                    #endregion
                }
                else if (exp is ConstantExpression)
                {
                    #region

                    var constantExpression = exp as ConstantExpression;

                    var value = constantExpression.Value;
                    if (value == null)
                        return info;
                    str.Append(AddParameter(value));

                    #endregion
                }
                else if (exp is UnaryExpression)
                {
                    #region

                    var unaryExpression = exp as UnaryExpression;
                    str.Append(ParseExpression(unaryExpression.Operand, false).Text);

                    #endregion
                }
                else if (exp is ParameterExpression)
                {
                    #region

                    var parameterExpression = exp as ParameterExpression;
                    var custAttr = parameterExpression.Type.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    if (custAttr.Length > 0)
                    {
                        var attr = (DBOrmColumnAttribute)custAttr[0];
                        str.Append(GetFullName(attr.ColumnName));
                    }
                    else throw DBInternal.UnsupportedCommandContextException();

                    #endregion
                }
                else if (exp is MethodCallExpression)
                {
                    #region

                    var methodCallExpression = exp as MethodCallExpression;
                    var method = methodCallExpression.Method;
                    if (method.DeclaringType == typeof(string) && method.Name.Contains("ToUpper"))
                    {
                        str.Append("UPPER(");
                        str.Append(ParseExpression(methodCallExpression.Object, false));
                        str.Append(")");
                    }
                    else if (method.DeclaringType == typeof(string) && method.Name.Contains("ToLower"))
                    {
                        str.Append("LOWER(");
                        str.Append(ParseExpression(methodCallExpression.Object, false));
                        str.Append(")");
                    }
                    else if (method.DeclaringType == typeof(string) && method.Name == "Contains")
                    {
                        str.Append(ParseExpression(methodCallExpression.Object, false).Text);
                        str.Append(" CONTAINING ");
                        str.Append(ParseExpression(methodCallExpression.Arguments[0], false).Text);
                    }
                    else
                    {
                        object obj = methodCallExpression.Object;
                        if (obj != null)
                            obj = ParseExpression(methodCallExpression.Object, true);

                        var values = new object[methodCallExpression.Arguments.Count];
                        for (int i = 0; i < values.Length; i++)
                            values[i] = ParseExpression(methodCallExpression.Arguments[i], true).Value;

                        var value = methodCallExpression.Method.Invoke(obj, values);
                        if (parseValue)
                        {
                            info.Value = value;
                            return info;
                        }
                        else str.Append(AddParameter(value));
                    }

                    #endregion
                }
                else throw DBInternal.UnsupportedCommandContextException();

                info.Text = str;
                return info;
            }
            private string AddParameter(object value)
            {
                value = value ?? DBNull.Value;

                if (value is string && _model.ColumnsDict.ContainsKey((string)value))
                    return GetFullName((string)value);

                var type = value.GetType();
                if (type.BaseType == typeof(Enum))
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type));

                var paramName = string.Concat("@p", _parameterCounter++);
                _model.AddParameter(_command, paramName, value);
                return paramName;
            }
            private List<object[]> FindBlockList(DBQuery query, Predicate<string> predicate)
            {
                return query.Structure.FindAll(block =>
                    predicate((string)block[0]));
            }
            private object[] FindBlock(DBQuery query, Predicate<string> predicate)
            {
                return query.Structure.Find(block =>
                    predicate((string)block[0]));
            }
            private class ExpressionInfo
            {
                public StringBuilder Text;
                public object Value;
            }
        }

        #endregion
    }
}
