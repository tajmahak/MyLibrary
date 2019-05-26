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
        public DBModelBase()
        {
            DefaultSelectCommandsDict = new Dictionary<DBTable, string>();
            DefaultInsertCommandsDict = new Dictionary<DBTable, string>();
            DefaultUpdateCommandsDict = new Dictionary<DBTable, string>();
            DefaultDeleteCommandsDict = new Dictionary<DBTable, string>();
            TablesDict = new Dictionary<string, DBTable>();
            ColumnsDict = new Dictionary<string, DBColumn>();
        }

        public DBTable[] Tables { get; protected set; }
        public bool Initialized { get; protected set; }
        public char OpenBlock { get; protected set; }
        public char CloseBlock { get; protected set; }
        public char ParameterPrefix { get; protected set; }

        protected internal Dictionary<DBTable, string> DefaultSelectCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultInsertCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultUpdateCommandsDict { get; private set; }
        protected internal Dictionary<DBTable, string> DefaultDeleteCommandsDict { get; private set; }
        protected internal Dictionary<string, DBTable> TablesDict { get; private set; }
        protected internal Dictionary<string, DBColumn> ColumnsDict { get; private set; }

        public abstract void Initialize(DbConnection connection);
        public abstract DbCommand CreateCommand(DbConnection connection);
        public abstract void AddCommandParameter(DbCommand command, string name, object value);
        public abstract object ExecuteInsertCommand(DbCommand command);
        public abstract DBCompiledQuery CompileQuery(DBQuery query, int nextParameterNumber = 0);

        public DbCommand CompileCommand(DbConnection connection, DBQuery query)
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
            if (!TablesDict.TryGetValue(tableName, out var table))
            {
                throw DBInternal.UnknownTableException(tableName);
            }

            return table;
        }
        public DBColumn GetColumn(string columnName)
        {
            if (!ColumnsDict.TryGetValue(columnName, out var column))
            {
                throw DBInternal.UnknownColumnException(null, columnName);
            }

            return column;
        }
        public bool TryGetColumn(string columnName, out DBColumn column)
        {
            return ColumnsDict.TryGetValue(columnName, out column);
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

        protected void PrepareSelectCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                Add(sql, DefaultSelectCommandsDict[query.Table]);
            }
            else
            {
                Add(sql, "SELECT ");

                var index = 0;
                foreach (var block in blockList)
                {
                    switch ((DBQueryTypeEnum)block[0])
                    {
                        case DBQueryTypeEnum.Select:
                            #region
                            var args = (string[])block[1];
                            if (args.Length == 0)
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
                                for (int j = 0; j < args.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Add(sql, ',');
                                    }

                                    var paramCol = args[j];
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
                        case DBQueryTypeEnum.SelectAs:
                            #region
                            if (index > 0)
                            {
                                Add(sql, ',');
                            }
                            Add(sql, GetFullName(block[2]), " AS ", GetName(block[1]));
                            index++;
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectSum:
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "SUM(", GetFullName(args[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectSumAs:
                            #region
                            args = (string[])block[1];
                            for (int i = 0; i < args.Length; i += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "SUM(", GetFullName(args[i]), ") AS ", GetName(args[i + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectMax:
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(args[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectMaxAs:
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(args[j]), ") AS ", GetName(args[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectMin:
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(args[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectMinAs:
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(args[j]), ") AS ", GetName(args[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.SelectCount:
                            #region
                            args = (string[])block[1];
                            if (args.Length == 0)
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
                                for (int j = 0; j < args.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Add(sql, ',');
                                    }
                                    Add(sql, "COUNT(", GetFullName(args[j]), ')');
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.Select_expression:
                            #region
                            Add(sql, ParseExpressionList((Expression)block[1], cQuery).Sql);
                            break;
                            #endregion
                    }
                }
                Add(sql, " FROM ", GetName(query.Table.Name));
            }
        }
        protected void PrepareInsertCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            Add(sql, "INSERT INTO ", GetName(query.Table.Name));

            var blockList = FindBlockList(query, DBQueryTypeEnum.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.InadequateInsertCommandException();
            }

            Add(sql, '(');
            for (int i = 0; i < blockList.Count; i++)
            {
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, GetColumnName(blockList[i][1]));
            }
            Add(sql, ")VALUES(");
            for (int i = 0; i < blockList.Count; i++)
            {
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, AddParameter(blockList[i][2], cQuery));
            }
            Add(sql, ')');
        }
        protected void PrepareUpdateCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            Add(sql, "UPDATE ", GetName(query.Table.Name), " SET ");

            var blockList = FindBlockList(query, DBQueryTypeEnum.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.InadequateUpdateCommandException();
            }

            for (int i = 0; i < blockList.Count; i++)
            {
                if (i > 0)
                {
                    Add(sql, ',');
                }
                Add(sql, GetFullName(blockList[i][1]), '=', AddParameter(blockList[i][2], cQuery));
            }
        }
        protected void PrepareDeleteCommand(StringBuilder sql, DBQuery query)
        {
            Add(sql, "DELETE FROM ", GetName(query.Table.Name));
        }
        protected void PrepareJoinCommand(StringBuilder sql, DBQuery query)
        {
            foreach (var block in query.Structure)
            {
                var queryType = (DBQueryTypeEnum)block[0];
                switch (queryType)
                {
                    case DBQueryTypeEnum.InnerJoin:
                        Add(sql, " INNER JOIN ", GetName(block[1]), " ON ", GetFullName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryTypeEnum.LeftOuterJoin:
                        Add(sql, " LEFT OUTER JOIN ", GetName(block[1]), " ON ", GetFullName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryTypeEnum.RightOuterJoin:
                        Add(sql, " RIGHT OUTER JOIN ", GetName(block[1]), " ON ", GetFullName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryTypeEnum.FullOuterJoin:
                        Add(sql, " FULL OUTER JOIN ", GetName(block[1]), " ON ", GetFullName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryTypeEnum.InnerJoin_type:
                    case DBQueryTypeEnum.LeftOuterJoin_type:
                    case DBQueryTypeEnum.RightOuterJoin_type:
                    case DBQueryTypeEnum.FullOuterJoin_type:
                        var foreignKey = DBInternal.GetForeignKey((Type)block[1], (Type)block[2]);
                        if (foreignKey == null)
                        {
                            throw DBInternal.ForeignKeyException();
                        }
                        var split = foreignKey[1].Split('.');
                        switch (queryType)
                        {
                            case DBQueryTypeEnum.InnerJoin_type:
                                Add(sql, " INNER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0])); break;
                            case DBQueryTypeEnum.LeftOuterJoin_type:
                                Add(sql, " LEFT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0])); break;
                            case DBQueryTypeEnum.RightOuterJoin_type:
                                Add(sql, " RIGHT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0])); break;
                            case DBQueryTypeEnum.FullOuterJoin_type:
                                Add(sql, " FULL OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0])); break;
                        }
                        break;




                }
            }
        }
        protected void PrepareSqlCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            var block = FindBlock(query, DBQueryTypeEnum.Sql);
            Add(sql, block[1]);
            var index = 0;
            foreach (var param in (object[])block[2])
            {
                cQuery.Parameters.Add(new DBCompiledQueryParameter()
                {
                    Name = string.Concat(ParameterPrefix, 'p', index++),
                    Value = param,
                });
            }
        }
        protected void PrepareWhereCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
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

                    switch ((DBQueryTypeEnum)block[0])
                    {
                        case DBQueryTypeEnum.Where_expression:
                            #region
                            Add(sql, ParseExpression((Expression)block[1], null, false, cQuery).Sql);
                            break;
                        #endregion
                        case DBQueryTypeEnum.Where:
                            #region
                            block[3] = block[3] ?? DBNull.Value;

                            Add(sql, GetFullName(block[1]));

                            if ((block[3] is DBNull) && ((string)block[2]) == "=")
                            {
                                Add(sql, " IS NULL");
                            }
                            else if (block[3] is DBNull && ((string)block[2]) == "<>")
                            {
                                Add(sql, " IS NOT NULL");
                            }
                            else
                            {
                                Add(sql, block[2], AddParameter(block[3], cQuery));
                            }
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereBetween:
                            #region
                            Add(sql, GetFullName(block[1]), "BETWEEN ", AddParameter(block[2], cQuery), " AND ", AddParameter(block[3], cQuery));
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereUpper:
                            #region
                            Add(sql, "UPPER(", GetFullName(block[1]), ")=", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereContaining:
                            #region
                            Add(sql, GetFullName(block[1]), " CONTAINING ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereContainingUpper:
                            #region
                            Add(sql, " UPPER(", GetFullName(block[1]), ") CONTAINING ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereLike:
                            #region
                            Add(sql, GetFullName(block[1]), " LIKE \'", block[2], '\'');
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereLikeUpper:
                            #region
                            Add(sql, " UPPER(", GetFullName(block[1]), ") LIKE '", block[2], '\'');
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereIn_command:
                            #region
                            Add(sql, GetFullName(block[1]), " IN (");

                            var innerQuery = CompileQuery((DBQuery)block[2], cQuery.Parameters.Count);
                            Add(sql, innerQuery.CommandText);
                            cQuery.Parameters.AddRange(innerQuery.Parameters);

                            Add(sql, ')');
                            break;
                        #endregion
                        case DBQueryTypeEnum.WhereIn_values:
                            #region
                            Add(sql, GetFullName(block[1]), " IN (");
                            #region Добавление списка значений

                            var values = (object[])block[2];
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
        protected void PrepareGroupByCommand(StringBuilder sql, DBQuery query)
        {
            var blockList = FindBlockList(query, DBQueryTypeEnum.GroupBy);
            if (blockList.Count > 0)
            {
                Add(sql, " GROUP BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    var args = (string[])block[1];
                    for (int j = 0; j < args.Length; j++)
                    {
                        if (index > 0)
                        {
                            Add(sql, ',');
                        }
                        Add(sql, GetFullName(args[j]));
                        index++;
                    }
                }
            }
        }
        protected void PrepareOrderByCommand(StringBuilder sql, DBQuery query)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("OrderBy"));
            if (blockList.Count > 0)
            {
                Add(sql, " ORDER BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    var queryType = (DBQueryTypeEnum)block[0];
                    switch (queryType)
                    {
                        case DBQueryTypeEnum.OrderBy:
                        case DBQueryTypeEnum.OrderByDesc:
                        case DBQueryTypeEnum.OrderByUpper:
                        case DBQueryTypeEnum.OrderByUpperDesc:
                            #region
                            var args = (string[])block[1];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                switch (queryType)
                                {
                                    case DBQueryTypeEnum.OrderBy:
                                        Add(sql, GetFullName(args[j])); break;
                                    case DBQueryTypeEnum.OrderByDesc:
                                        Add(sql, GetFullName(args[j]), " DESC"); break;
                                    case DBQueryTypeEnum.OrderByUpper:
                                        Add(sql, "UPPER(", GetFullName(args[j]), ")"); break;
                                    case DBQueryTypeEnum.OrderByUpperDesc:
                                        Add(sql, "UPPER(", GetFullName(block[1]), ") DESC"); break;
                                }

                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryTypeEnum.OrderBy_expression:
                            Add(sql, ParseExpressionList((Expression)block[1], null).Sql); break;
                    }
                }
            }
        }
        //!!! реализовать Union команду

        protected List<object[]> FindBlockList(DBQuery query, Predicate<DBQueryTypeEnum> predicate)
        {
            return query.Structure.FindAll(block => predicate((DBQueryTypeEnum)block[0]));
        }
        protected List<object[]> FindBlockList(DBQuery query, DBQueryTypeEnum type)
        {
            return FindBlockList(query, x => x == type);
        }
        protected List<object[]> FindBlockList(DBQuery query, Predicate<string> predicate)
        {
            return query.Structure.FindAll(block => predicate(block[0].ToString()));
        }
        protected object[] FindBlock(DBQuery query, Predicate<DBQueryTypeEnum> type)
        {
            return query.Structure.Find(block =>
                type((DBQueryTypeEnum)block[0]));
        }
        protected object[] FindBlock(DBQuery query, DBQueryTypeEnum type)
        {
            return FindBlock(query, x => x == type);
        }
        protected string AddParameter(object value, DBCompiledQuery cQuery)
        {
            value = value ?? DBNull.Value;

            if (value is string && ColumnsDict.ContainsKey((string)value))
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

        private class ParseExpressionResult
        {
            public StringBuilder Sql = new StringBuilder();
            public object Value;
        }
    }
}
