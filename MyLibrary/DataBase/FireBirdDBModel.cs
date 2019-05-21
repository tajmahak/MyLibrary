using FirebirdSql.Data.FirebirdClient;
using MyLibrary.DataBase.Orm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MyLibrary.DataBase
{
    public class FireBirdDBModel : DBModelBase
    {
        public FireBirdDBModel()
        {
            _queryCompiler = new FireBirdQueryCompiler(this);
        }

        public override void Initialize(DbConnection connection)
        {
            InitializeDBModel((FbConnection)connection);
            InitializeDefaultCommands();
            IsInitialized = true;
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
        }
        public override void AddParameter(DbCommand command, string name, object value)
        {
            ((FbCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DbCommand CreateCommand(DbConnection connection, DBQuery query)
        {
            var compiledQuery = _queryCompiler.CompileQuery(query);
            var command = (FbCommand)connection.CreateCommand();
            command.CommandText = compiledQuery.CommandText;
            foreach (var parameter in compiledQuery.Parameters)
            {
                AddParameter(command, parameter.Name, parameter.Value);
            }
            return command;
        }

        private void InitializeDBModel(FbConnection connection)
        {
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

            #region Подготовка значений

            Tables = tables;
            for (int i = 0; i < tables.Length; i++)
            {
                var table = tables[i];
                TablesDict.Add(table.Name, table);

                for (int j = 0; j < table.Columns.Length; j++)
                {
                    var column = table.Columns[j];
                    string longName = string.Concat(table.Name, '.', column.Name);
                    ColumnsDict.Add(longName, column);
                }
            }

            #endregion
        }
        private void InitializeDefaultCommands()
        {
            for (int i = 0; i < Tables.Length; i++)
            {
                var table = Tables[i];

                var selectCommand = _queryCompiler.GetSelectCommand(table);
                var insertCommand = string.Concat(_queryCompiler.GetInsertCommand(table), " RETURNING ", _queryCompiler.GetName(table.Columns[table.PrimaryKeyIndex].Name));
                var updateCommand = _queryCompiler.GetUpdateCommand(table);
                var deleteCommand = _queryCompiler.GetDeleteCommand(table);

                DefaultSelectCommandsDict.Add(table, selectCommand);
                DefaultInsertCommandsDict.Add(table, insertCommand);
                DefaultUpdateCommandsDict.Add(table, updateCommand);
                DefaultDeleteCommandsDict.Add(table, deleteCommand);
            }
        }

        private FireBirdQueryCompiler _queryCompiler;
    }

    internal class FireBirdQueryCompiler : DBQueryCompilerBase
    {
        public FireBirdQueryCompiler(FireBirdDBModel model) : base(model)
        {
            BeginBlock = EndBlock = '\"';
            ParameterPrefix = '@';
        }

        public override DBCompiledQuery CompileQuery(DBQuery query, int nextParameterNumber = 0)
        {
            var cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            object[] block;
            List<object[]> blockList;
            string[] args;
            int index = 0;
            var sql = new StringBuilder();

            if (query.QueryType == DBQueryTypeEnum.Select)
            {
                #region SELECT [...] ... FROM ...

                blockList = FindBlockList(query, x => x.StartsWith("Select"));
                if (blockList.Count == 0)
                {
                    sql.Append(Model.DefaultSelectCommandsDict[query.Table]);
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

                                        var paramCol = args[j];
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
                    sql.Append(AddParameter(blockList[i][2], cQuery));
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
                    sql.Append(AddParameter(blockList[i][2], cQuery));
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
                    sql.Append(AddParameter(blockList[i][2], cQuery));
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
                    cQuery.Parameters.Add(new DBCompiledQueryParameter()
                    {
                        Name = "@p" + index++,
                        Value = param,
                    });
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

                    if (needPastePredicate)
                    {
                        needPastePredicate = false;
                        if (blockName == "Or")
                        {
                            sql.Append(" OR");
                            continue;
                        }
                        else if (blockName != ")")
                        {
                            sql.Append(" AND");
                        }
                    }
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

                    #endregion

                    switch (blockName)
                    {
                        case "Where_expression":
                            #region
                            sql.Append(' ');
                            sql.Append(ParseExpression((Expression)block[1], false, cQuery).Text);
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
                                sql.Append(AddParameter(block[3], cQuery));
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
                            sql.Append(AddParameter(block[2], cQuery));
                            sql.Append(" AND ");
                            sql.Append(AddParameter(block[3], cQuery));
                            break;
                        #endregion
                        case "WhereUpper":
                            #region
                            if (prevBlockName == "Not")
                                sql.Append(" NOT");
                            sql.Append(" UPPER(");
                            sql.Append(GetFullName(block[1]));
                            sql.Append(")=");
                            sql.Append(AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case "WhereContaining":
                            #region
                            sql.Append(' ');
                            sql.Append(GetFullName(block[1]));
                            if (prevBlockName == "Not")
                                sql.Append(" NOT");
                            sql.Append(" CONTAINING ");
                            sql.Append(AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case "WhereContainingUpper":
                            #region
                            sql.Append(" UPPER(");
                            sql.Append(GetFullName(block[1]));
                            if (prevBlockName == "Not")
                                sql.Append(" NOT");
                            sql.Append(") CONTAINING ");
                            sql.Append(AddParameter(block[2], cQuery));
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

                            var innerQuery = CompileQuery((DBQuery)block[2], cQuery.Parameters.Count);
                            sql.Append(innerQuery.CommandText);
                            cQuery.Parameters.AddRange(innerQuery.Parameters);

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

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }

        private ExpressionInfo ParseExpression(Expression exp, bool parseValue, DBCompiledQuery cQuery)
        {
            var info = new ExpressionInfo();
            var str = new StringBuilder();

            if (exp is BinaryExpression)
            {
                #region

                var binaryExpression = exp as BinaryExpression;
                str.Append('(');
                str.Append(ParseExpression(binaryExpression.Left, false, cQuery).Text);

                var result = ParseExpression(binaryExpression.Right, false, cQuery).Text;
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
                        var innerInfo = ParseExpression(memberExpression.Expression, true, cQuery);
                        value = propertyInfo.GetValue(innerInfo.Value, null);
                    }
                    else value = propertyInfo.GetValue(null, null);

                    if (parseValue)
                    {
                        info.Value = value;
                        return info;
                    }
                    else str.Append(AddParameter(value, cQuery));
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
                    else str.Append(AddParameter(value, cQuery));
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
                str.Append(AddParameter(value, cQuery));

                #endregion
            }
            else if (exp is UnaryExpression)
            {
                #region

                var unaryExpression = exp as UnaryExpression;
                str.Append(ParseExpression(unaryExpression.Operand, false, cQuery).Text);

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
                    str.Append(ParseExpression(methodCallExpression.Object, false, cQuery));
                    str.Append(")");
                }
                else if (method.DeclaringType == typeof(string) && method.Name.Contains("ToLower"))
                {
                    str.Append("LOWER(");
                    str.Append(ParseExpression(methodCallExpression.Object, false, cQuery));
                    str.Append(")");
                }
                else if (method.DeclaringType == typeof(string) && method.Name == "Contains")
                {
                    str.Append(ParseExpression(methodCallExpression.Object, false, cQuery).Text);
                    str.Append(" CONTAINING ");
                    str.Append(ParseExpression(methodCallExpression.Arguments[0], false, cQuery).Text);
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                        obj = ParseExpression(methodCallExpression.Object, true, cQuery);

                    var values = new object[methodCallExpression.Arguments.Count];
                    for (int i = 0; i < values.Length; i++)
                        values[i] = ParseExpression(methodCallExpression.Arguments[i], true, cQuery).Value;

                    var value = methodCallExpression.Method.Invoke(obj, values);
                    if (parseValue)
                    {
                        info.Value = value;
                        return info;
                    }
                    else str.Append(AddParameter(value, cQuery));
                }

                #endregion
            }
            else throw DBInternal.UnsupportedCommandContextException();

            info.Text = str;
            return info;
        }
        private class ExpressionInfo
        {
            public StringBuilder Text;
            public object Value;
        }
    }
}
