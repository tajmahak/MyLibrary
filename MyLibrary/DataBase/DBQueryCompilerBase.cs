using MyLibrary.DataBase.Orm;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MyLibrary.DataBase
{
    public abstract class DBQueryCompilerBase
    {
        public DBQueryCompilerBase(DBModelBase model)
        {
            Model = model;
        }

        public abstract DBCompiledQuery CompileQuery(DBQuery query, int nextParameterNumber = 0);

        public virtual string GetSelectCommand(DBTable table)
        {
            var sql = new StringBuilder();
            Add(sql, "SELECT ", GetName(table.Name), ".* FROM ", GetName(table.Name));
            return sql.ToString();
        }
        public virtual string GetInsertCommand(DBTable table)
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
                    Add(sql, Model.ParameterPrefix, 'p', paramIndex++);
                }
            }
            Add(sql, ')');
            return sql.ToString();
        }
        public virtual string GetUpdateCommand(DBTable table)
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
                Add(sql, GetName(column.Name), '=', Model.ParameterPrefix, 'p', index++);
            }
            Add(sql, " WHERE ", GetName(table.PrimaryKeyColumn.Name), '=', Model.ParameterPrefix, "id");
            return sql.ToString();
        }
        public virtual string GetDeleteCommand(DBTable table)
        {
            var sql = new StringBuilder();
            Add(sql, "DELETE FROM ", GetName(table.Name), " WHERE ", GetName(table.PrimaryKeyColumn.Name), '=', Model.ParameterPrefix, "id");
            return sql.ToString();
        }

        public string GetFullName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(Model.OpenBlock, split[0], Model.CloseBlock, '.', Model.OpenBlock, split[1], Model.CloseBlock);
        }
        public string GetName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(Model.OpenBlock, split[0], Model.CloseBlock);
        }
        public string GetColumnName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(Model.OpenBlock, split[1], Model.CloseBlock);
        }

        protected List<object[]> FindBlockList(DBQuery query, Predicate<string> predicate)
        {
            return query.Structure.FindAll(block => predicate((string)block[0]));
        }
        protected List<object[]> FindBlockList(DBQuery query, string name)
        {
            return FindBlockList(query, x => x == name);
        }
        protected object[] FindBlock(DBQuery query, Predicate<string> predicate)
        {
            return query.Structure.Find(block =>
                predicate((string)block[0]));
        }
        protected object[] FindBlock(DBQuery query, string name)
        {
            return FindBlock(query, x => x == name);
        }
        protected string AddParameter(object value, DBCompiledQuery cQuery)
        {
            value = value ?? DBNull.Value;

            if (value is string && Model.ColumnsDict.ContainsKey((string)value))
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
                Name = string.Concat(Model.ParameterPrefix, 'p', paramNumber),
                Value = value,
            };
            cQuery.Parameters.Add(parameter);

            return parameter.Name;
        }
        protected DBModelBase Model { get; private set; }
        protected void Add(StringBuilder str, params object[] values)
        {
            foreach (var value in values)
            {
                str.Append(value);
            }
        }

        protected void PrepareSelectCommand(StringBuilder sql, DBQuery query)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                Add(sql, Model.DefaultSelectCommandsDict[query.Table]);
            }
            else
            {
                Add(sql, "SELECT ");
                #region

                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch ((string)block[0])
                    {
                        case "Select":
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
                        case "SelectAs":
                            #region
                            if (index > 0)
                            {
                                Add(sql, ',');
                            }
                            Add(sql, GetName(block[1]), ".", GetColumnName(block[2]));
                            index++;
                            break;
                        #endregion
                        case "SelectSum":
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
                        case "SelectSumAs":
                            #region
                            args = (string[])block[1];
                            for (int j = 0; j < args.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "SUM(", GetFullName(args[j]), ") AS ", GetName(args[j + 1]));
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
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(args[j]), ')');
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
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MAX(", GetFullName(args[j]), ") AS ", GetName(args[j + 1]));
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
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(args[j]), ')');
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
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "MIN(", GetFullName(args[j]), ") AS ", GetName(args[j + 1]));
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
                    }
                }

                #endregion
                Add(sql, " FROM ", GetName(query.Table.Name));
            }
        }
        protected void PrepareInsertCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            Add(sql, "INSERT INTO ", GetName(query.Table.Name));

            var blockList = FindBlockList(query, "Set");
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

            var blockList = FindBlockList(query, "Set");
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
            foreach (var item in query.Structure)
            {
                switch ((string)item[0])
                {
                    case "InnerJoin":
                        Add(sql, " INNER JOIN ", GetName(item[1]), " ON ", GetFullName(item[1]), '=', GetFullName(item[2]));
                        break;

                    case "LeftOuterJoin":
                        Add(sql, " LEFT OUTER JOIN ", GetName(item[1]), " ON ", GetFullName(item[1]), '=', GetFullName(item[2]));
                        break;

                    case "RightOuterJoin":
                        Add(sql, " RIGHT OUTER JOIN ", GetName(item[1]), " ON ", GetFullName(item[1]), '=', GetFullName(item[2]));
                        break;

                    case "FullOuterJoin":
                        Add(sql, " FULL OUTER JOIN ", GetName(item[1]), " ON ", GetFullName(item[1]), '=', GetFullName(item[2]));
                        break;

                    case "InnerJoinAs":
                        Add(sql, " INNER JOIN ", GetName(item[2]), " AS ", GetName(item[1]), " ON ", GetName(item[1]), ".", GetColumnName(item[2]), '=', GetFullName(item[3]));
                        break;

                    case "LeftOuterJoinAs":
                        Add(sql, " LEFT OUTER JOIN ", GetName(item[2]), " AS ", GetName(item[1]), " ON ", GetName(item[1]), ".", GetColumnName(item[2]), '=', GetFullName(item[3]));
                        break;

                    case "RightOuterJoinAs":
                        Add(sql, " RIGHT OUTER JOIN ", GetName(item[2]), " AS ", GetName(item[1]), " ON ", GetName(item[1]), ".", GetColumnName(item[2]), '=', GetFullName(item[3]));
                        break;

                    case "FullOuterJoinAs":
                        Add(sql, " FULL OUTER JOIN ", GetName(item[2]), " AS ", GetName(item[1]), " ON ", GetName(item[1]), ".", GetColumnName(item[2]), '=', GetFullName(item[3]));
                        break;
                }
            }
        }
        protected void PrepareSqlCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            var block = FindBlock(query, "Sql");
            Add(sql, block[1]);
            var index = 0;
            foreach (var param in (object[])block[2])
            {
                cQuery.Parameters.Add(new DBCompiledQueryParameter()
                {
                    Name = string.Concat(Model.ParameterPrefix, 'p', index++),
                    Value = param,
                });
            }
        }
        protected void PrepareWhereCommand(StringBuilder sql, DBQuery query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, x => x.Contains("Where") || x == "Or" || x == "Not" || x == "(" || x == ")");
            if (blockList.Count > 0)
            {
                Add(sql, " WHERE");

                bool needPastePredicate = false;
                string prevBlockName = null;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    var blockName = (string)block[0];
                    if (i > 0)
                    {
                        prevBlockName = (string)blockList[i - 1][0];
                    }

                    #region AND,OR,NOT,(,)

                    if (needPastePredicate)
                    {
                        needPastePredicate = false;
                        if (blockName == "Or")
                        {
                            Add(sql, " OR");
                            continue;
                        }
                        else if (blockName != ")")
                        {
                            Add(sql, " AND");
                        }
                    }
                    if (blockName == "(")
                    {
                        Add(sql, " (");
                        continue;
                    }
                    else if (blockName == ")")
                    {
                        Add(sql, " )");
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
                            Add(sql, ' ', ParseExpression((Expression)block[1], false, cQuery).Text);
                            break;
                        #endregion
                        case "Where":
                            #region
                            block[3] = block[3] ?? DBNull.Value;

                            Add(sql, ' ', GetFullName(block[1]));

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
                        case "WhereBetween":
                            #region
                            Add(sql, ' ', GetFullName(block[1]));
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " BETWEEN ", AddParameter(block[2], cQuery), " AND ", AddParameter(block[3], cQuery));
                            break;
                        #endregion
                        case "WhereUpper":
                            #region
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " UPPER(", GetFullName(block[1]), ")=", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case "WhereContaining":
                            #region
                            Add(sql, ' ', GetFullName(block[1]));
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " CONTAINING ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case "WhereContainingUpper":
                            #region
                            Add(sql, " UPPER(", GetFullName(block[1]));
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, ") CONTAINING ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case "WhereLike":
                            #region
                            Add(sql, ' ', GetFullName(block[1]));
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " LIKE \'", block[2], '\'');
                            break;
                        #endregion
                        case "WhereLikeUpper":
                            #region
                            Add(sql, " UPPER(", GetFullName(block[1]), ')');
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " LIKE '", block[2], '\'');
                            break;
                        #endregion
                        case "WhereIn_command":
                            #region
                            Add(sql, ' ', GetFullName(block[1]), " IN (");

                            var innerQuery = CompileQuery((DBQuery)block[2], cQuery.Parameters.Count);
                            Add(sql, innerQuery.CommandText);
                            cQuery.Parameters.AddRange(innerQuery.Parameters);

                            Add(sql, ')');
                            break;
                        #endregion
                        case "WhereIn_values":
                            #region
                            Add(sql, ' ', GetFullName(block[1]));
                            if (prevBlockName == "Not")
                            {
                                Add(sql, " NOT");
                            }
                            Add(sql, " IN (");
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
                    needPastePredicate = true;
                }
            }
        }
        protected void PrepareGroupByCommand(StringBuilder sql, DBQuery query)
        {
            var blockList = FindBlockList(query, "GroupBy");
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
                    var args = (string[])block[1];
                    switch ((string)block[0])
                    {
                        case "OrderBy":
                            #region
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
                        case "OrderByDesc":
                            #region
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, GetFullName(args[j]), " DESC");
                                index++;
                            }
                            break;
                        #endregion
                        case "OrderByUpper":
                            #region
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "UPPER(", GetFullName(args[j]), ")");
                                index++;
                            }
                            break;
                        #endregion
                        case "OrderByUpperDesc":
                            #region
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Add(sql, ',');
                                }
                                Add(sql, "UPPER(", GetFullName(block[1]), ") DESC");
                                index++;
                            }
                            break;
                            #endregion
                    }
                }
            }
        }

        private ExpressionInfo ParseExpression(Expression exp, bool parseValue, DBCompiledQuery cQuery)
        {
            var info = new ExpressionInfo();
            var sql = new StringBuilder();

            if (exp is BinaryExpression)
            {
                #region

                var binaryExpression = exp as BinaryExpression;
                Add(sql, '(', ParseExpression(binaryExpression.Left, false, cQuery).Text);

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
                    Add(sql, @operator, result);
                }
                else
                {
                    #region IS [NOT] NULL

                    Add(sql, " IS");
                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.Equal:
                            break;
                        case ExpressionType.NotEqual:
                            Add(sql, " NOT"); break;
                        default: throw DBInternal.UnsupportedCommandContextException();
                    }
                    Add(sql, " NULL");

                    #endregion
                }
                Add(sql, ')');

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
                    Add(sql, GetFullName(attr.ColumnName));
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
                    else
                    {
                        value = propertyInfo.GetValue(null, null);
                    }

                    if (parseValue)
                    {
                        info.Value = value;
                        return info;
                    }
                    else
                    {
                        Add(sql, AddParameter(value, cQuery));
                    }
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
                    else
                    {
                        Add(sql, AddParameter(value, cQuery));
                    }
                }
                else
                {
                    throw DBInternal.UnsupportedCommandContextException();
                }

                #endregion
            }
            else if (exp is ConstantExpression)
            {
                #region

                var constantExpression = exp as ConstantExpression;

                var value = constantExpression.Value;
                if (value == null)
                {
                    return info;
                }
                Add(sql, AddParameter(value, cQuery));

                #endregion
            }
            else if (exp is UnaryExpression)
            {
                #region

                var unaryExpression = exp as UnaryExpression;
                Add(sql, ParseExpression(unaryExpression.Operand, false, cQuery).Text);

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
                    Add(sql, GetFullName(attr.ColumnName));
                }
                else
                {
                    throw DBInternal.UnsupportedCommandContextException();
                }

                #endregion
            }
            else if (exp is MethodCallExpression)
            {
                #region

                var methodCallExpression = exp as MethodCallExpression;
                var method = methodCallExpression.Method;
                if (method.DeclaringType == typeof(string) && method.Name.Contains("ToUpper"))
                {
                    Add(sql, "UPPER(", ParseExpression(methodCallExpression.Object, false, cQuery), ")");
                }
                else if (method.DeclaringType == typeof(string) && method.Name.Contains("ToLower"))
                {
                    Add(sql, "LOWER(", ParseExpression(methodCallExpression.Object, false, cQuery), ")");
                }
                else if (method.DeclaringType == typeof(string) && method.Name == "Contains")
                {
                    Add(sql, ParseExpression(methodCallExpression.Object, false, cQuery).Text, " CONTAINING ", ParseExpression(methodCallExpression.Arguments[0], false, cQuery).Text);
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                    {
                        obj = ParseExpression(methodCallExpression.Object, true, cQuery);
                    }

                    var values = new object[methodCallExpression.Arguments.Count];
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = ParseExpression(methodCallExpression.Arguments[i], true, cQuery).Value;
                    }

                    var value = methodCallExpression.Method.Invoke(obj, values);
                    if (parseValue)
                    {
                        info.Value = value;
                        return info;
                    }
                    else
                    {
                        Add(sql, AddParameter(value, cQuery));
                    }
                }

                #endregion
            }
            else
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            info.Text = sql;
            return info;
        }
        private class ExpressionInfo
        {
            public StringBuilder Text;
            public object Value;
        }
    }
}
