using MyLibrary.Data;
using MyLibrary.DataBase.Orm;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MyLibrary.DataBase
{
    public abstract class DBModelBase
    {
        public DBTable[] Tables { get; private set; }
        public bool Initialized { get; private set; }
        public char OpenBlock { get; protected set; }
        public char CloseBlock { get; protected set; }
        public char ParameterPrefix { get; protected set; }

        protected event EventHandler<InitializeFromDbConnectionEventArgs> InitializeFromDbConnection;
        protected event EventHandler<InitializeDefaultInsertCommandEventArgs> InitializeDefaultInsertCommand;

        public abstract DbCommand CreateCommand(DbConnection connection);
        public abstract void AddCommandParameter(DbCommand command, string name, object value);
        public abstract object ExecuteInsertCommand(DbCommand command);
        public abstract DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0);

        public void Initialize(DbConnection connection)
        {
            if (InitializeFromDbConnection == null)
            {
                throw new ArgumentNullException(nameof(InitializeFromDbConnection));
            }

            var args = new InitializeFromDbConnectionEventArgs()
            {
                DbConnection = connection,
            };
            InitializeFromDbConnection(this, args);

            Tables = args.Tables;

            InitializeDictionaries();
            Initialized = true;
        }
        public void Initialize(Type[] ormTableTypes)
        {
            foreach (var tableType in ormTableTypes)
            {
                var tableName = DBInternal.GetTableNameFromAttribute(tableType);
                var table = new DBTable(this, tableName);

                foreach (var columnProperty in tableType.GetProperties())
                {
                    var attrList = columnProperty.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    if (attrList.Length == 0)
                    {
                        continue;
                    }

                    var attr = (DBOrmColumnAttribute)attrList[0];
                    var column = new DBColumn(table);
                    column.Name = attr.ColumnName;
                    column.IsPrimary = attr.PrimaryKey;
                    column.AllowDBNull = attr.AllowDbNull;

                    var columnType = columnProperty.PropertyType;
                    if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        columnType = Nullable.GetUnderlyingType(columnType);
                    }
                    column.DataType = columnType;
                }
            }
            InitializeDictionaries();
            Initialized = true;
        }
        public string GetDefaultSqlQuery(DBTable table, DBQueryTypeEnum queryType)
        {
            switch (queryType)
            {
                case DBQueryTypeEnum.Select:
                    return _selectCommandsDict[table];

                case DBQueryTypeEnum.Insert:
                    return _insertCommandsDict[table];

                case DBQueryTypeEnum.Update:
                    return _updateCommandsDict[table];

                case DBQueryTypeEnum.Delete:
                    return _deleteCommandsDict[table];

            }
            throw new NotImplementedException();
        }
        public DbCommand CompileCommand(DbConnection connection, DBQueryBase query)
        {
            var compiledQuery = CompileQuery(query);
            var command = CreateCommand(connection);
            command.CommandText = compiledQuery.CommandText;
            foreach (var parameter in compiledQuery.Parameters)
            {
                AddCommandParameter(command, parameter.Name, parameter.Value);
            }
            return command;
        }
        public DBContext CreateDBContext(DbConnection connection)
        {
            var context = new DBContext(this, connection);
            return context;
        }
        public DBTable GetTable(string tableName)
        {
            if (!_tablesDict.TryGetValue(tableName, out var table))
            {
                throw DBInternal.UnknownTableException(tableName);
            }

            return table;
        }
        public DBColumn GetColumn(string columnName)
        {
            if (!_columnsDict.TryGetValue(columnName, out var column))
            {
                throw DBInternal.UnknownColumnException(null, columnName);
            }

            return column;
        }
        public bool TryGetColumn(string columnName, out DBColumn column)
        {
            return _columnsDict.TryGetValue(columnName, out column);
        }

        #region [protected] Вспомогательные сущности для получения SQL-команд

        protected virtual string GetSelectCommand(DBTable table)
        {
            var sql = new StringBuilder();
            Add(sql, "SELECT ", GetName(table.Name), ".* FROM ", GetName(table.Name));
            return sql.ToString();
        }
        protected virtual string GetInsertCommand(DBTable table)
        {
            var sql = new StringBuilder();
            Add(sql, "INSERT INTO ", GetName(table.Name), " VALUES(");

            int index = 0;
            int paramIndex = 0;
            foreach (var column in table.Columns)
            {
                if (index++ > 0)
                {
                    Add(sql, ',');
                }
                if (column.IsPrimary)
                {
                    Add(sql, "NULL");
                }
                else
                {
                    Add(sql, ParameterPrefix, 'p', paramIndex++);
                }
            }
            Add(sql, ')');
            return sql.ToString();
        }
        protected virtual string GetUpdateCommand(DBTable table)
        {
            var sql = new StringBuilder();

            Add(sql, "UPDATE ", GetName(table.Name), " SET ");
            int index = 0;
            foreach (var column in table.Columns)
            {
                if (column.IsPrimary)
                {
                    continue;
                }
                if (index != 0)
                {
                    Add(sql, ',');
                }
                Add(sql, GetName(column.Name), '=', ParameterPrefix, 'p', index++);
            }
            Add(sql, " WHERE ", GetName(table.PrimaryKeyColumn.Name), '=', ParameterPrefix, "id");
            return sql.ToString();
        }
        protected virtual string GetDeleteCommand(DBTable table)
        {
            var sql = new StringBuilder();
            Add(sql, "DELETE FROM ", GetName(table.Name), " WHERE ", GetName(table.PrimaryKeyColumn.Name), '=', ParameterPrefix, "id");
            return sql.ToString();
        }

        protected string GetFullName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[0], CloseBlock, '.', OpenBlock, split[1], CloseBlock);
        }
        protected string GetName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[0], CloseBlock);
        }
        protected string GetColumnName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[1], CloseBlock);
        }

        protected void PrepareSelectCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                Add(sql, _selectCommandsDict[query.Table]);
            }
            else
            {
                Add(sql, "SELECT ");

                var index = 0;
                foreach (var block in blockList)
                {
                    switch (block.Type)
                    {
                        case DBQueryStructureTypeEnum.Select:
                            #region
                            if (block.Length == 0)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, '*');
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Add(sql, ',');
                                    }

                                    var paramCol = (string)block[j];
                                    if (paramCol.Contains("."))
                                    {
                                        // Столбец
                                        Add(sql, GetFullName(paramCol));
                                    }
                                    else
                                    {
                                        // Таблица
                                        Add(sql, GetName(paramCol), ".*");
                                    }
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectAs:
                            #region
                            if (index > 0)
                            {
                                Add(sql, ',');
                            }
                            Add(sql, GetFullName(block[1]), " AS ", GetName(block[0]));
                            index++;
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectSum:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "SUM(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectSumAs:
                            #region
                            for (int i = 0; i < block.Length; i += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "SUM(", GetFullName(block[i]), ") AS ", GetName(block[i + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectMax:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectMaxAs:
                            #region
                            for (int j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectMin:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectMinAs:
                            #region
                            for (int j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.SelectCount:
                            #region
                            if (block.Length == 0)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "COUNT(*)");
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Add(sql, ',');
                                    }
                                    Add(sql, "COUNT(", GetFullName(block[j]), ')');
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.Select_expression:
                            #region
                            Add(sql, ParseExpressionList((Expression)block[0], cQuery).Sql);
                            break;
                            #endregion
                    }
                }
                Add(sql, " FROM ", GetName(query.Table.Name));
            }
        }
        protected void PrepareInsertCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            Add(sql, "INSERT INTO ", GetName(query.Table.Name));

            var blockList = FindBlockList(query, DBQueryStructureTypeEnum.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.InadequateInsertCommandException();
            }

            Add(sql, '(');
            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, GetColumnName(block[0]));
            }
            Add(sql, ")VALUES(");
            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, AddParameter(block[1], cQuery));
            }
            Add(sql, ')');
        }
        protected void PrepareUpdateCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            Add(sql, "UPDATE ", GetName(query.Table.Name), " SET ");

            var blockList = FindBlockList(query, DBQueryStructureTypeEnum.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.InadequateUpdateCommandException();
            }

            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, GetFullName(block[0]), '=', AddParameter(block[1], cQuery));
            }
        }
        protected void PrepareDeleteCommand(StringBuilder sql, DBQueryBase query)
        {
            Add(sql, "DELETE FROM ", GetName(query.Table.Name));
        }
        protected void PrepareJoinCommand(StringBuilder sql, DBQueryBase query)
        {
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureTypeEnum.InnerJoin:
                        Add(sql, " INNER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureTypeEnum.LeftOuterJoin:
                        Add(sql, " LEFT OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureTypeEnum.RightOuterJoin:
                        Add(sql, " RIGHT OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureTypeEnum.FullOuterJoin:
                        Add(sql, " FULL OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureTypeEnum.InnerJoinAs:
                        Add(sql, " INNER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureTypeEnum.LeftOuterJoinAs:
                        Add(sql, " LEFT OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureTypeEnum.RightOuterJoinAs:
                        Add(sql, " RIGHT OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureTypeEnum.FullOuterJoinAs:
                        Add(sql, " FULL OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureTypeEnum.InnerJoin_type:
                    case DBQueryStructureTypeEnum.LeftOuterJoin_type:
                    case DBQueryStructureTypeEnum.RightOuterJoin_type:
                    case DBQueryStructureTypeEnum.FullOuterJoin_type:

                    case DBQueryStructureTypeEnum.InnerJoinAs_type:
                    case DBQueryStructureTypeEnum.LeftOuterJoinAs_type:
                    case DBQueryStructureTypeEnum.RightOuterJoinAs_type:
                    case DBQueryStructureTypeEnum.FullOuterJoinAs_type:
                        #region
                        var foreignKey = DBInternal.GetForeignKey((Type)block[0], (Type)block[1]);
                        if (foreignKey == null)
                        {
                            throw DBInternal.ForeignKeyException();
                        }
                        var split = foreignKey[1].Split('.');
                        switch (block.Type)
                        {
                            case DBQueryStructureTypeEnum.InnerJoin_type:
                                Add(sql, " INNER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.LeftOuterJoin_type:
                                Add(sql, " LEFT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.RightOuterJoin_type:
                                Add(sql, " RIGHT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.FullOuterJoin_type:
                                Add(sql, " FULL OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.InnerJoinAs_type:
                                Add(sql, " INNER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.LeftOuterJoinAs_type:
                                Add(sql, " LEFT OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.RightOuterJoinAs_type:
                                Add(sql, " RIGHT OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureTypeEnum.FullOuterJoinAs_type:
                                Add(sql, " FULL OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;
                        }
                        break;
                        #endregion


                }
            }
        }
        protected void PrepareWhereCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("Where"));

            if (blockList.Count > 0)
            {
                Add(sql, " WHERE ");

                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    if (i > 0)
                    {
                        Add(sql, " AND ");
                    }

                    switch (block.Type)
                    {
                        case DBQueryStructureTypeEnum.Where_expression:
                            #region
                            Add(sql, ParseExpression((Expression)block[0], null, false, cQuery).Sql);
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.Where:
                            #region
                            block[2] = block[2] ?? DBNull.Value;

                            Add(sql, GetFullName(block[0]));

                            if ((block[2] is DBNull) && ((string)block[1]) == "=")
                            {
                                Add(sql, " IS NULL");
                            }
                            else if (block[2] is DBNull && ((string)block[1]) == "<>")
                            {
                                Add(sql, " IS NOT NULL");
                            }
                            else
                            {
                                Add(sql, block[1], AddParameter(block[2], cQuery));
                            }
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereBetween:
                            #region
                            Add(sql, GetFullName(block[0]), "BETWEEN ", AddParameter(block[1], cQuery), " AND ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereUpper:
                            #region
                            Add(sql, "UPPER(", GetFullName(block[0]), ")=", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereContaining:
                            #region
                            Add(sql, GetFullName(block[0]), " CONTAINING ", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereContainingUpper:
                            #region
                            Add(sql, " UPPER(", GetFullName(block[0]), ") CONTAINING ", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereLike:
                            #region
                            Add(sql, GetFullName(block[0]), " LIKE \'", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereLikeUpper:
                            #region
                            Add(sql, " UPPER(", GetFullName(block[0]), ") LIKE '", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereIn_command:
                            #region
                            Add(sql, GetFullName(block[0]), " IN (");

                            var innerQuery = CompileQuery((DBQuery)block[1], cQuery.Parameters.Count);
                            Add(sql, innerQuery.CommandText);
                            cQuery.Parameters.AddRange(innerQuery.Parameters);

                            Add(sql, ')');
                            break;
                        #endregion
                        case DBQueryStructureTypeEnum.WhereIn_values:
                            #region
                            Add(sql, GetFullName(block[0]), " IN (");
                            #region Добавление списка значений

                            var values = (object[])block[1];
                            for (int j = 0; j < values.Length; j++)
                            {
                                if (j > 0)
                                {
                                    Add(sql, ',');
                                }

                                var value = values[j];
                                if (value.GetType().IsPrimitive)
                                {
                                    Add(sql, value);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            #endregion
                            Add(sql, ')');
                            break;
                            #endregion
                    }
                }
            }
        }
        protected void PrepareGroupByCommand(StringBuilder sql, DBQueryBase query)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("GroupBy"));
            if (blockList.Count > 0)
            {
                Add(sql, " GROUP BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureTypeEnum.GroupBy:
                            #region
                            var args = (string[])block[0];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, GetFullName(args[j]));
                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureTypeEnum.GroupBy_expression:
                            Add(sql, ParseExpressionList((Expression)block[0], null).Sql);
                            break;
                    }
                }
            }
        }
        protected void PrepareOrderByCommand(StringBuilder sql, DBQueryBase query)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("OrderBy"));
            if (blockList.Count > 0)
            {
                Add(sql, " ORDER BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureTypeEnum.OrderBy:
                        case DBQueryStructureTypeEnum.OrderByDesc:
                        case DBQueryStructureTypeEnum.OrderByUpper:
                        case DBQueryStructureTypeEnum.OrderByUpperDesc:
                            #region
                            var args = (string[])block[0];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                switch (block.Type)
                                {
                                    case DBQueryStructureTypeEnum.OrderBy:
                                        Add(sql, GetFullName(args[j])); break;
                                    case DBQueryStructureTypeEnum.OrderByDesc:
                                        Add(sql, GetFullName(args[j]), " DESC"); break;
                                    case DBQueryStructureTypeEnum.OrderByUpper:
                                        Add(sql, "UPPER(", GetFullName(args[j]), ")"); break;
                                    case DBQueryStructureTypeEnum.OrderByUpperDesc:
                                        Add(sql, "UPPER(", GetFullName(block[1]), ") DESC"); break;
                                }

                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureTypeEnum.OrderBy_expression:
                            Add(sql, ParseExpressionList((Expression)block[0], null).Sql);
                            break;
                    }
                }
            }
        }







        //!!! реализовать Union команду

        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, Predicate<DBQueryStructureTypeEnum> predicate)
        {
            return query.Structure.FindAll(x => predicate(x.Type));
        }
        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, DBQueryStructureTypeEnum type)
        {
            return FindBlockList(query, x => x == type);
        }
        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, Predicate<string> predicate)
        {
            return query.Structure.FindAll(x => predicate(x.Type.ToString()));
        }
        protected DBQueryStructureBlock FindBlock(DBQueryBase query, Predicate<DBQueryStructureTypeEnum> type)
        {
            return query.Structure.Find(x => type(x.Type));
        }
        protected DBQueryStructureBlock FindBlock(DBQueryBase query, DBQueryStructureTypeEnum type)
        {
            return FindBlock(query, x => x == type);
        }
        protected string AddParameter(object value, DBCompiledQuery cQuery)
        {
            value = value ?? DBNull.Value;

            if (value is string && _columnsDict.ContainsKey((string)value))
            {
                return GetFullName((string)value);
            }

            var type = value.GetType();
            if (type.BaseType == typeof(Enum))
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(type));
            }

            var paramNumber = cQuery.NextParameterNumber++;
            var parameter = new DBCompiledQueryParameter()
            {
                Name = string.Concat(ParameterPrefix, 'p', paramNumber),
                Value = value,
            };
            cQuery.Parameters.Add(parameter);

            return parameter.Name;
        }
        protected void Add(StringBuilder str, params object[] values)
        {
            foreach (var value in values)
            {
                str.Append(value);
            }
        }

        #endregion

        private ParseExpressionResult ParseExpression(Expression expression, Expression parentExpression, bool parseValue, DBCompiledQuery cQuery)
        {
            var result = new ParseExpressionResult();

            if (expression is BinaryExpression binaryExpression)
            {
                #region

                Add(result.Sql, '(', ParseExpression(binaryExpression.Left, expression, false, cQuery).Sql);

                var rightBlock = ParseExpression(binaryExpression.Right, expression, false, cQuery).Sql;
                if (rightBlock.Length > 0)
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

                    Add(result.Sql, @operator, rightBlock);
                }
                else
                {
                    // IS [NOT] NULL
                    Add(result.Sql, " IS");
                    if (binaryExpression.NodeType == ExpressionType.NotEqual)
                    {
                        Add(result.Sql, " NOT");
                    }
                    Add(result.Sql, " NULL");
                }

                Add(result.Sql, ')');

                #endregion
            }
            else if (expression is MemberExpression memberExpression)
            {
                #region

                if (memberExpression.Expression is ParameterExpression)
                {
                    var custAttr = memberExpression.Member.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    var attr = (DBOrmColumnAttribute)custAttr[0];
                    Add(result.Sql, GetFullName(attr.ColumnName));
                }
                else if (memberExpression.Member is PropertyInfo)
                {
                    var propertyInfo = memberExpression.Member as PropertyInfo;

                    object value;
                    if (memberExpression.Expression != null)
                    {
                        var innerInfo = ParseExpression(memberExpression.Expression, expression, true, cQuery);
                        value = propertyInfo.GetValue(innerInfo.Value, null);
                    }
                    else
                    {
                        value = propertyInfo.GetValue(null, null);
                    }

                    if (parseValue)
                    {
                        result.Value = value;
                        return result;
                    }
                    else
                    {
                        Add(result.Sql, AddParameter(value, cQuery));
                    }
                }
                else if (memberExpression.Member is FieldInfo)
                {
                    var fieldInfo = memberExpression.Member as FieldInfo;
                    var constantExpression = memberExpression.Expression as ConstantExpression;
                    var value = fieldInfo.GetValue(constantExpression.Value);

                    if (parseValue)
                    {
                        result.Value = value;
                        return result;
                    }
                    else
                    {
                        Add(result.Sql, AddParameter(value, cQuery));
                    }
                }
                else
                {
                    throw DBInternal.UnsupportedCommandContextException();
                }

                #endregion
            }
            else if (expression is ConstantExpression constantExpression)
            {
                #region

                var value = constantExpression.Value;
                if (parseValue)
                {
                    result.Value = value;
                    return result;
                }
                else
                {
                    if (value != null)
                    {
                        Add(result.Sql, AddParameter(value, cQuery));
                    }
                    return result;
                }

                #endregion
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                #region

                Add(result.Sql, ParseExpression(unaryExpression.Operand, expression, false, cQuery).Sql);

                #endregion
            }
            else if (expression is ParameterExpression parameterExpression)
            {
                #region

                var custAttr = parameterExpression.Type.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                if (custAttr.Length > 0)
                {
                    var attr = (DBOrmColumnAttribute)custAttr[0];
                    Add(result.Sql, GetFullName(attr.ColumnName));
                }
                else
                {
                    throw DBInternal.UnsupportedCommandContextException();
                }

                #endregion
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                #region

                var method = methodCallExpression.Method;
                if (method.DeclaringType == typeof(DBFunction))
                {
                    Add(result.Sql, ParseFunctionExpression(methodCallExpression, parentExpression, cQuery).Sql);
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                    {
                        obj = ParseExpression(methodCallExpression.Object, expression, true, cQuery).Value;
                    }

                    var arguments = new object[methodCallExpression.Arguments.Count];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        arguments[i] = ParseExpression(methodCallExpression.Arguments[i], expression, true, cQuery).Value;
                    }

                    var value = methodCallExpression.Method.Invoke(obj, arguments);

                    if (parseValue)
                    {
                        result.Value = value;
                        return result;
                    }
                    else
                    {
                        Add(result.Sql, AddParameter(value, cQuery));
                    }
                }

                #endregion
            }
            else
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            return result;
        }
        private ParseExpressionResult ParseExpressionList(Expression expression, DBCompiledQuery cQuery)
        {
            var result = new ParseExpressionResult();

            if (expression is NewArrayExpression newArrayExpression)
            {
                foreach (var exprArg in newArrayExpression.Expressions)
                {
                    if (result.Sql.Length > 0)
                    {
                        Add(result.Sql, ',');
                    }
                    Add(result.Sql, ParseExpression(exprArg, expression, false, cQuery).Sql);
                }
            }
            else
            {
                Add(result.Sql, ParseExpression(expression, null, false, cQuery).Sql);
            }

            return result;
        }
        private ParseExpressionResult ParseFunctionExpression(MethodCallExpression expression, Expression parentExpression, DBCompiledQuery cQuery)
        {
            var result = new ParseExpressionResult();

            var args = new object[expression.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = ParseExpression(expression.Arguments[i], expression, false, cQuery).Sql;
            }

            string notBlock = (parentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not) ? "NOT " : string.Empty;

            switch (expression.Method.Name)
            {
                #region Функции для работы со строками

                case nameof(DBFunction.CharLength):
                    Add(result.Sql, "CHAR_LENGTH(", args[0], ")"); break;


                case nameof(DBFunction.Hash):
                    Add(result.Sql, "HASH(", args[0], ")"); break;


                case nameof(DBFunction.Left):
                    Add(result.Sql, "LEFT(", args[0], ",", args[1], ")"); break;


                case nameof(DBFunction.Lower):
                    Add(result.Sql, "LOWER(", args[0], ")"); break;


                case nameof(DBFunction.LPad):
                    Add(result.Sql, "LPAD(", args[0], ",", args[1]);
                    if (args.Length > 2 && !Format.IsEmpty(args[2]))
                    {
                        Add(result.Sql, ",", args[2]);
                    }
                    Add(result.Sql, ")"); break;


                case nameof(DBFunction.Overlay):
                    Add(result.Sql, "OVERLAY(", args[0], " PLACING ", args[1], " FROM ", args[2]);
                    if (args.Length > 3 && !Format.IsEmpty(args[3]))
                    {
                        Add(result.Sql, " FOR ", args[3]);
                    }
                    Add(result.Sql, ")"); break;


                case nameof(DBFunction.Replace):
                    Add(result.Sql, "REPLACE(", args[0], ",", args[1], ",", args[2], ")"); break;


                case nameof(DBFunction.Reverse):
                    Add(result.Sql, "REVERSE(", args[0], ")"); break;


                case nameof(DBFunction.Right):
                    Add(result.Sql, "RIGHT(", args[0], ",", args[1], ")"); break;


                case nameof(DBFunction.RPad):
                    Add(result.Sql, "RPAD(", args[0], ",", args[1]);
                    if (args.Length > 2 && !Format.IsEmpty(args[2]))
                    {
                        Add(result.Sql, ",", args[2]);
                    }
                    Add(result.Sql, ")"); break;


                case nameof(DBFunction.SubString):
                    Add(result.Sql, "SUBSTRING (", args[0], " FROM ", args[1]);
                    if (args.Length > 2 && !Format.IsEmpty(args[2]))
                    {
                        Add(result.Sql, " FOR ", args[2]);
                    }
                    Add(result.Sql, ")"); break;


                case nameof(DBFunction.Upper):
                    Add(result.Sql, "UPPER(", args[0], ")"); break;

                #endregion

                #region Предикаты сравнения

                case nameof(DBFunction.Between):
                    Add(result.Sql, args[0], " ", notBlock, "BETWEEN ", args[1], " AND ", args[2]); break;


                case nameof(DBFunction.Like):
                    Add(result.Sql, args[0], " ", notBlock, "LIKE ", args[1]);
                    if (args.Length > 2 && !Format.IsEmpty(args[2]))
                    {
                        Add(result.Sql, " ESCAPE ", args[2]);
                    }
                    break;


                case nameof(DBFunction.StartingWith):
                    Add(result.Sql, args[0], " ", notBlock, "STARTING WITH ", args[1]); break;


                case nameof(DBFunction.Containing):
                    Add(result.Sql, args[0], " ", notBlock, "CONTAINING ", args[1]); break;


                case nameof(DBFunction.SimilarTo):
                    Add(result.Sql, args[0], " ", notBlock, "SIMILAR TO ", args[1]);
                    if (args.Length > 2 && !Format.IsEmpty(args[2]))
                    {
                        Add(result.Sql, " ESCAPE ", args[2]);
                    }
                    break;

                #endregion

                #region Агрегатные функции

                case nameof(DBFunction.Avg):
                    string option = string.Empty;
                    if (args.Length > 1)
                    {
                        option = ParseAggregateExpression(expression.Arguments[1]);
                    }
                    Add(result.Sql, "AVG(", option, args[0], ")"); break;


                case nameof(DBFunction.Count):
                    if (args.Length > 0)
                    {
                        Add(result.Sql, "COUNT(", args[0], ")");
                    }
                    else
                    {
                        Add(result.Sql, "COUNT(*)");
                    }
                    break;


                case nameof(DBFunction.List):
                    option = string.Empty;
                    if (args.Length > 2)
                    {
                        option = ParseAggregateExpression(expression.Arguments[2]);
                    }
                    Add(result.Sql, "LIST(", option, args[0]);
                    if (args.Length > 1)
                    {
                        Add(result.Sql, ",", args[1]);
                    }
                    Add(result.Sql, ")"); break;


                case nameof(DBFunction.Max):
                    option = string.Empty;
                    if (args.Length > 1)
                    {
                        option = ParseAggregateExpression(expression.Arguments[1]);
                    }
                    Add(result.Sql, "MAX(", option, args[0], ")"); break;


                case nameof(DBFunction.Min):
                    option = string.Empty;
                    if (args.Length > 1)
                    {
                        option = ParseAggregateExpression(expression.Arguments[1]);
                    }
                    Add(result.Sql, "MIN(", option, args[0], ")"); break;


                case nameof(DBFunction.Sum):
                    option = string.Empty;
                    if (args.Length > 1)
                    {
                        option = ParseAggregateExpression(expression.Arguments[1]);
                    }
                    Add(result.Sql, "MIN(", option, args[0], ")"); break;

                #endregion

                case nameof(DBFunction.As):
                    Add(result.Sql, args[0], " AS ", OpenBlock, (expression.Arguments[1] as ConstantExpression).Value, CloseBlock); break;

                case nameof(DBFunction.Desc):
                    Add(result.Sql, args[0], " DESC"); break;
            }

            return result;
        }
        private string ParseAggregateExpression(Expression expression)
        {
            var constantExpression = (ConstantExpression)expression;
            switch ((DBFunction.OptionEnum)constantExpression.Value)
            {
                case DBFunction.OptionEnum.All:
                    return "ALL ";
                case DBFunction.OptionEnum.Distinct:
                    return "DISTINCT ";
            }
            throw new Exception();
        }
        private void InitializeDictionaries()
        {
            foreach (var table in Tables)
            {
                _selectCommandsDict.Add(table, GetSelectCommand(table));
                _updateCommandsDict.Add(table, GetUpdateCommand(table));
                _deleteCommandsDict.Add(table, GetDeleteCommand(table));
                if (InitializeDefaultInsertCommand == null)
                {
                    _insertCommandsDict.Add(table, GetInsertCommand(table));
                }
                else
                {
                    var args = new InitializeDefaultInsertCommandEventArgs()
                    {
                        Table = table,
                    };
                    InitializeDefaultInsertCommand(this, args);
                    _insertCommandsDict.Add(table, args.DefaultInsertCommand);
                }

                _tablesDict.Add(table.Name, table);
                foreach (var column in table.Columns)
                {
                    string longName = string.Concat(table.Name, '.', column.Name);
                    _columnsDict.Add(longName, column);
                }
            }
        }

        private Dictionary<DBTable, string> _selectCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _insertCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _updateCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _deleteCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<string, DBTable> _tablesDict = new Dictionary<string, DBTable>();
        private Dictionary<string, DBColumn> _columnsDict = new Dictionary<string, DBColumn>();

        private class ParseExpressionResult
        {
            public StringBuilder Sql = new StringBuilder();
            public object Value;
        }
    }

    public class InitializeFromDbConnectionEventArgs : EventArgs
    {
        public DbConnection DbConnection { get; internal set; }
        public DBTable[] Tables { get; set; }
    }
    public class InitializeDefaultInsertCommandEventArgs : EventArgs
    {
        public DBTable Table { get; internal set; }
        public string DefaultInsertCommand { get; set; }
    }
}