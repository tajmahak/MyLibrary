using MyLibrary.Collections;
using MyLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс модели БД, включающего функции взаимодействия с СУБД.
    /// </summary>
    public abstract class DBModelBase
    {
        public ReadOnlyArray<DBTable> Tables { get; private set; }
        public bool IsInitialized { get; private set; }

        public string OpenBlock { get; protected set; } = string.Empty;
        public string CloseBlock { get; protected set; } = string.Empty;

        public abstract DBTable[] GetTableSchema(DbConnection connection);
        public abstract void AddCommandParameter(DbCommand command, string name, object value);
        public abstract DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0);
        public virtual object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
        }

        public void Initialize(DbConnection connection)
        {
            if (IsInitialized)
            {
                throw DBInternal.ContextInitializeException();
            }
            Tables = GetTableSchema(connection);
            InitializeDictionaries();
            IsInitialized = true;
        }
        public void Initialize(Type[] ormTableTypes)
        {
            Tables = new DBTable[ormTableTypes.Length];
            for (int i = 0; i < Tables.Count; i++)
            {
                var tableType = ormTableTypes[i];
                var table = new DBTable(this);
                table.Name = DBInternal.GetTableNameFromAttribute(tableType);

                foreach (var columnProperty in tableType.GetProperties())
                {
                    var attrList = columnProperty.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    if (attrList.Length == 0)
                    {
                        continue;
                    }

                    var attr = (DBOrmColumnAttribute)attrList[0];
                    var column = new DBColumn(table);
                    column.Name = attr.ColumnName.Split('.')[1];
                    column.NotNull = attr.NotNull;

                    var columnType = columnProperty.PropertyType;
                    if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        columnType = Nullable.GetUnderlyingType(columnType);
                    }
                    column.DataType = columnType;

                    if (attr.PrimaryKey)
                    {
                        column.IsPrimary = true;
                        table.PrimaryKeyColumn = column;
                    }

                    table.AddColumn(column);
                }
                Tables[i] = table;
            }
            InitializeDictionaries();
            IsInitialized = true;
        }
        public string GetDefaultSqlQuery(DBTable table, StatementType statementType)
        {
            if (table.PrimaryKeyColumn == null && statementType != StatementType.Select)
            {
                throw DBInternal.GetDefaultSqlQueryException(table);
            }

            switch (statementType)
            {
                case StatementType.Select:
                    return _selectCommandsDict[table];

                case StatementType.Insert:
                    return _insertCommandsDict[table];

                case StatementType.Update:
                    return _updateCommandsDict[table];

                case StatementType.Delete:
                    return _deleteCommandsDict[table];
            }
            throw new NotImplementedException();
        }
        public DbCommand CreateCommand(DbConnection connection, DBQueryBase query)
        {
            var compiledQuery = CompileQuery(query);
            var command = connection.CreateCommand();
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
            var table = TryGetTable(tableName);
            if (table == null)
            {
                throw DBInternal.UnknownTableException(tableName);
            }
            return table;
        }
        public DBColumn GetColumn(string columnName)
        {
            var column = TryGetColumn(columnName);
            if (column == null)
            {
                throw DBInternal.UnknownColumnException(null, columnName);
            }
            return column;
        }
        public DBTable TryGetTable(string tableName)
        {
            if (_tablesDict.TryGetValue(tableName, out var table))
            {
                return table;
            }
            return null;
        }
        public DBColumn TryGetColumn(string columnName)
        {
            if (_columnsDict.TryGetValue(columnName, out var column))
            {
                return column;
            }
            return null;
        }

        #region [protected] Вспомогательные сущности для получения SQL-команд

        protected void PrepareSelectBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = query.FindBlocks(x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                Concat(sql, _selectCommandsDict[query.Table]);
            }
            else
            {
                Concat(sql, "SELECT ");

                var index = 0;
                foreach (var block in blockList)
                {
                    switch (block.Type)
                    {
                        case DBQueryStructureType.Select:
                            #region
                            if (block.Length == 0)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, '*');
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Concat(sql, ',');
                                    }

                                    var paramCol = (string)block[j];
                                    if (paramCol.Contains("."))
                                    {
                                        // Столбец
                                        Concat(sql, GetFullName(paramCol));
                                    }
                                    else
                                    {
                                        // Таблица
                                        Concat(sql, GetName(paramCol), ".*");
                                    }
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectAs:
                            #region
                            if (index > 0)
                            {
                                Concat(sql, ',');
                            }
                            Concat(sql, GetFullName(block[1]), " AS ", GetName(block[0]));
                            index++;
                            break;
                        #endregion
                        case DBQueryStructureType.SelectSum:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "SUM(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectSumAs:
                            #region
                            for (int i = 0; i < block.Length; i += 2)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "SUM(", GetFullName(block[i]), ") AS ", GetName(block[i + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMax:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "MAX(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMaxAs:
                            #region
                            for (int j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "MAX(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMin:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "MIN(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMinAs:
                            #region
                            for (int j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "MIN(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectCount:
                            #region
                            if (block.Length == 0)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, "COUNT(*)");
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        Concat(sql, ',');
                                    }
                                    Concat(sql, "COUNT(", GetFullName(block[j]), ')');
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.Select_expression:
                            #region
                            Concat(sql, GetListFromExpression(block[0], cQuery));
                            break;
                            #endregion
                    }
                }
                Concat(sql, " FROM ", GetName(query.Table.Name));
            }
        }
        protected void PrepareInsertBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            Concat(sql, "INSERT INTO ", GetName(query.Table.Name), '(');

            var blockList = query.FindBlocks(DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongInsertCommandException();
            }

            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Concat(sql, ',');
                }
                Concat(sql, GetColumnName(block[0]));
            }
            Concat(sql, ")VALUES(");
            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Concat(sql, ',');
                }
                Concat(sql, GetParameter(block[1], cQuery));
            }
            Concat(sql, ')');
        }
        protected void PrepareUpdateBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            Concat(sql, "UPDATE ", GetName(query.Table.Name), " SET ");

            var blockList = query.FindBlocks(DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongUpdateCommandException();
            }

            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    Concat(sql, ',');
                }
                Concat(sql, GetFullName(block[0]), '=', GetParameter(block[1], cQuery));
            }
        }
        protected void PrepareDeleteBlock(StringBuilder sql, DBQueryBase query)
        {
            Concat(sql, "DELETE FROM ", GetName(query.Table.Name));
        }
        protected void PrepareJoinBlock(StringBuilder sql, DBQueryBase query)
        {
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.InnerJoin:
                        Concat(sql, " INNER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.LeftJoin:
                        Concat(sql, " LEFT JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.RightJoin:
                        Concat(sql, " RIGHT JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.FullJoin:
                        Concat(sql, " FULL JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;


                    case DBQueryStructureType.InnerJoinAs:
                        Concat(sql, " INNER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.LeftJoinAs:
                        Concat(sql, " LEFT JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.RightJoinAs:
                        Concat(sql, " RIGHT JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.FullJoinAs:
                        Concat(sql, " FULL JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.InnerJoin_type:
                    case DBQueryStructureType.LeftJoin_type:
                    case DBQueryStructureType.RightJoin_type:
                    case DBQueryStructureType.FullJoin_type:
                    case DBQueryStructureType.InnerJoinAs_type:
                    case DBQueryStructureType.LeftJoinAs_type:
                    case DBQueryStructureType.RightJoinAs_type:
                    case DBQueryStructureType.FullJoinAs_type:
                        #region
                        var foreignKey = DBInternal.GetForeignKey((Type)block[0], (Type)block[1]);
                        if (foreignKey == null)
                        {
                            throw DBInternal.ForeignKeyException();
                        }
                        var split = foreignKey[1].Split('.');
                        switch (block.Type)
                        {
                            case DBQueryStructureType.InnerJoin_type:
                                Concat(sql, " INNER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftJoin_type:
                                Concat(sql, " LEFT JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightJoin_type:
                                Concat(sql, " RIGHT JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullJoin_type:
                                Concat(sql, " FULL JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;


                            case DBQueryStructureType.InnerJoinAs_type:
                                Concat(sql, " INNER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftJoinAs_type:
                                Concat(sql, " LEFT JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightJoinAs_type:
                                Concat(sql, " RIGHT JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullJoinAs_type:
                                Concat(sql, " FULL JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;
                        }
                        break;
                        #endregion
                }
            }
        }
        protected void PrepareWhereBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = query.FindBlocks(x => x.StartsWith("Where"));

            if (blockList.Count > 0)
            {
                Concat(sql, " WHERE ");

                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    if (i > 0)
                    {
                        Concat(sql, " AND ");
                    }

                    switch (block.Type)
                    {
                        case DBQueryStructureType.Where_expression:
                            #region
                            Concat(sql, GetSqlFromExpression(block[0], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.Where:
                            #region
                            block[2] = block[2] ?? DBNull.Value;

                            Concat(sql, GetFullName(block[0]));

                            if ((block[2] is DBNull) && ((string)block[1]) == "=")
                            {
                                Concat(sql, " IS NULL");
                            }
                            else if (block[2] is DBNull && ((string)block[1]) == "<>")
                            {
                                Concat(sql, " IS NOT NULL");
                            }
                            else
                            {
                                Concat(sql, block[1], GetParameter(block[2], cQuery));
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.WhereBetween:
                            #region
                            Concat(sql, GetFullName(block[0]), "BETWEEN ", GetParameter(block[1], cQuery), " AND ", GetParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereUpper:
                            #region
                            Concat(sql, "UPPER(", GetFullName(block[0]), ")=", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContaining:
                            #region
                            Concat(sql, GetFullName(block[0]), " CONTAINING ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContainingUpper:
                            #region
                            Concat(sql, " UPPER(", GetFullName(block[0]), ") CONTAINING ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLike:
                            #region
                            Concat(sql, GetFullName(block[0]), " LIKE \'", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLikeUpper:
                            #region
                            Concat(sql, " UPPER(", GetFullName(block[0]), ") LIKE '", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureType.WhereIn_command:
                            #region
                            Concat(sql, GetFullName(block[0]), " IN ");
                            Concat(sql, GetSubQuery((DBQueryBase)block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereIn_values:
                            #region
                            Concat(sql, GetFullName(block[0]), " IN (");
                            #region Добавление списка значений

                            var values = (object[])block[1];
                            for (int j = 0; j < values.Length; j++)
                            {
                                if (j > 0)
                                {
                                    Concat(sql, ',');
                                }

                                var value = values[j];
                                if (value.GetType().IsPrimitive)
                                {
                                    Concat(sql, value);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            #endregion
                            Concat(sql, ')');
                            break;
                            #endregion
                    }
                }
            }
        }
        protected void PrepareOrderByBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.FindBlocks(x => x.StartsWith("OrderBy"));
            if (blockList.Count > 0)
            {
                Concat(sql, " ORDER BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureType.OrderBy:
                        case DBQueryStructureType.OrderByDesc:
                        case DBQueryStructureType.OrderByUpper:
                        case DBQueryStructureType.OrderByUpperDesc:
                            #region
                            var args = (string[])block.Args;
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                switch (block.Type)
                                {
                                    case DBQueryStructureType.OrderBy:
                                        Concat(sql, GetFullName(args[j])); break;
                                    case DBQueryStructureType.OrderByDesc:
                                        Concat(sql, GetFullName(args[j]), " DESC"); break;
                                    case DBQueryStructureType.OrderByUpper:
                                        Concat(sql, "UPPER(", GetFullName(args[j]), ")"); break;
                                    case DBQueryStructureType.OrderByUpperDesc:
                                        Concat(sql, "UPPER(", GetFullName(block[1]), ") DESC"); break;
                                }

                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.OrderBy_expression:
                            Concat(sql, GetListFromExpression(block[0], null));
                            break;
                    }
                }
            }
        }
        protected void PrepareGroupByBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.FindBlocks(x => x.StartsWith("GroupBy"));
            if (blockList.Count > 0)
            {
                Concat(sql, " GROUP BY ");
                var index = 0;
                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureType.GroupBy:
                            #region
                            var args = (string[])block.Args;
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    Concat(sql, ',');
                                }
                                Concat(sql, GetFullName(args[j]));
                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.GroupBy_expression:
                            Concat(sql, GetListFromExpression(block[0], null));
                            break;
                    }
                }
            }
        }
        protected void PrepareHavingBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var block = query.FindBlock(x => x == DBQueryStructureType.Having_expression);
            if (block != null)
            {
                Concat(sql, " HAVING ", GetSqlFromExpression(block[0], cQuery));
            }
        }
        protected void PrepareUnionBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = query.FindBlocks(DBQueryStructureType.UnionAll);
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.UnionAll:
                        Concat(sql, " UNION ALL ", GetSubQuery((DBQueryBase)block[0], cQuery));
                        break;

                    case DBQueryStructureType.UnionDistinct:
                        Concat(sql, " UNION DISTINCT ", GetSubQuery((DBQueryBase)block[0], cQuery));
                        break;
                }
            }
        }

        protected virtual string GetInsertCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            Concat(sql, "INSERT INTO ", GetName(table.Name), " VALUES(");

            int index = 0;
            int paramIndex = 0;
            foreach (var column in table.Columns)
            {
                if (index++ > 0)
                {
                    Concat(sql, ',');
                }
                if (column.IsPrimary)
                {
                    Concat(sql, "NULL");
                }
                else
                {
                    Concat(sql, "@p", paramIndex++);
                }
            }
            Concat(sql, ')');
            return sql.ToString();
        }
        protected string GetSelectCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            Concat(sql, "SELECT ", GetName(table.Name), ".* FROM ", GetName(table.Name));
            return sql.ToString();
        }
        protected string GetUpdateCommandText(DBTable table)
        {
            var sql = new StringBuilder();

            Concat(sql, "UPDATE ", GetName(table.Name), " SET ");
            int index = 0;
            foreach (var column in table.Columns)
            {
                if (column.IsPrimary)
                {
                    continue;
                }
                if (index != 0)
                {
                    Concat(sql, ',');
                }
                Concat(sql, GetName(column.Name), "=@p", index++);
            }
            Concat(sql, " WHERE ", GetName(table.PrimaryKeyColumn.Name), "=@id");
            return sql.ToString();
        }
        protected string GetDeleteCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            Concat(sql, "DELETE FROM ", GetName(table.Name), " WHERE ", GetName(table.PrimaryKeyColumn.Name), "=@id");
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
        protected string GetParameter(object value, DBCompiledQuery cQuery)
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
            var parameter = new DBParameter()
            {
                Name = string.Concat("@p", paramNumber),
                Value = value,
            };
            cQuery.Parameters.Add(parameter);

            return parameter.Name;
        }
        protected string GetSubQuery(DBQueryBase subQuery, DBCompiledQuery cQuery)
        {
            var subCQuery = CompileQuery(subQuery, cQuery.Parameters.Count);
            cQuery.Parameters.AddRange(subCQuery.Parameters);
            return string.Concat('(', subCQuery.CommandText, ')');
        }
        protected string GetSqlFromExpression(object expression, DBCompiledQuery cQuery, object parentExpression = null)
        {
            var value = ParseExpression(false, (Expression)expression, cQuery, (Expression)parentExpression);
            return value.ToString();
        }
        protected object GetValueFromExpression(object expression, object parentExpression = null)
        {
            var value = ParseExpression(true, (Expression)expression, null, (Expression)parentExpression);
            return value;
        }
        protected string GetListFromExpression(object expression, DBCompiledQuery cQuery)
        {
            var sql = new StringBuilder();
            if (expression is NewArrayExpression newArrayExpression)
            {
                foreach (var exprArg in newArrayExpression.Expressions)
                {
                    if (sql.Length > 0)
                    {
                        Concat(sql, ',');
                    }
                    Concat(sql, GetSqlFromExpression(exprArg, cQuery, expression));
                }
            }
            else
            {
                Concat(sql, GetSqlFromExpression(expression, cQuery));
            }

            return sql.ToString();
        }

        protected void Concat(StringBuilder str, params object[] values)
        {
            foreach (var value in values)
            {
                str.Append(value);
            }
        }

        #endregion

        private object ParseExpression(bool parseValue, Expression expression, DBCompiledQuery cQuery, Expression parentExpression)
        {
            var sql = new StringBuilder();

            if (expression is BinaryExpression binaryExpression)
            {
                #region

                Concat(sql, '(', GetSqlFromExpression(binaryExpression.Left, cQuery, expression));

                var rightBlock = GetSqlFromExpression(binaryExpression.Right, cQuery, expression);
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

                        case ExpressionType.Add:
                        case ExpressionType.AddChecked:
                            @operator = "+"; break;
                        case ExpressionType.Subtract:
                        case ExpressionType.SubtractChecked:
                            @operator = "-"; break;
                        case ExpressionType.Divide:
                            @operator = "/"; break;
                        case ExpressionType.Multiply:
                        case ExpressionType.MultiplyChecked:
                            @operator = "*"; break;

                        default: throw DBInternal.UnsupportedCommandContextException();
                    }

                    #endregion

                    Concat(sql, @operator, rightBlock);
                }
                else
                {
                    if (binaryExpression.NodeType == ExpressionType.NotEqual)
                    {
                        Concat(sql, " IS NOT NULL");
                    }
                    else
                    {
                        Concat(sql, " IS NULL");
                    }
                }

                Concat(sql, ')');

                #endregion
            }
            else if (expression is MemberExpression memberExpression)
            {
                #region

                if (memberExpression.Expression is ParameterExpression)
                {
                    var custAttr = memberExpression.Member.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    var attr = (DBOrmColumnAttribute)custAttr[0];
                    if (parentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Negate)
                    {
                        Concat(sql, '-');
                    }
                    Concat(sql, GetFullName(attr.ColumnName));
                }
                else if (memberExpression.Member is PropertyInfo)
                {
                    var propertyInfo = memberExpression.Member as PropertyInfo;

                    object value;
                    if (memberExpression.Expression != null)
                    {
                        value = GetValueFromExpression(memberExpression.Expression, expression);
                        value = propertyInfo.GetValue(value, null);
                    }
                    else
                    {
                        value = propertyInfo.GetValue(null, null);
                    }

                    if (parseValue)
                    {
                        return value;
                    }
                    else
                    {
                        Concat(sql, GetParameter(value, cQuery));
                    }
                }
                else if (memberExpression.Member is FieldInfo)
                {
                    var fieldInfo = memberExpression.Member as FieldInfo;
                    var constantExpression = memberExpression.Expression as ConstantExpression;
                    var value = fieldInfo.GetValue(constantExpression.Value);

                    if (parseValue)
                    {
                        return value;
                    }
                    else
                    {
                        if (value is DBQueryBase subQuery)
                        {
                            Concat(sql, GetSubQuery(subQuery, cQuery));
                        }
                        else
                        {
                            Concat(sql, GetParameter(value, cQuery));
                        }
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
                    return value;
                }
                else
                {
                    if (value != null)
                    {
                        Concat(sql, GetParameter(value, cQuery));
                    }
                }

                #endregion
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                #region

                if (parseValue)
                {
                    return GetValueFromExpression(unaryExpression.Operand, expression);
                }
                else
                {
                    Concat(sql, GetSqlFromExpression(unaryExpression.Operand, cQuery, expression));
                }

                #endregion
            }
            else if (expression is ParameterExpression parameterExpression)
            {
                #region

                var custAttr = parameterExpression.Type.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                if (custAttr.Length > 0)
                {
                    var attr = (DBOrmColumnAttribute)custAttr[0];
                    Concat(sql, GetFullName(attr.ColumnName));
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
                    Concat(sql, ParseExpressionFunction(methodCallExpression, cQuery, parentExpression));
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                    {
                        obj = GetValueFromExpression(methodCallExpression.Object, expression);
                    }

                    var arguments = new object[methodCallExpression.Arguments.Count];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        arguments[i] = GetValueFromExpression(methodCallExpression.Arguments[i], expression);
                    }

                    var value = methodCallExpression.Method.Invoke(obj, arguments);
                    if (parseValue)
                    {
                        return value;
                    }
                    else
                    {
                        Concat(sql, GetParameter(value, cQuery));
                    }
                }

                #endregion
            }
            else if (expression is NewArrayExpression newArrayExpression)
            {
                if (parseValue)
                {
                    var array = (Array)Activator.CreateInstance(newArrayExpression.Type, newArrayExpression.Expressions.Count);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var value = GetValueFromExpression(newArrayExpression.Expressions[i], expression);
                        array.SetValue(value, i);
                    }
                    return array;
                }
                else
                {
                    throw DBInternal.UnsupportedCommandContextException();
                }
            }
            else
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            return sql.ToString();
        }
        private string ParseExpressionFunction(MethodCallExpression expression, DBCompiledQuery cQuery, Expression parentExpression)
        {
            var sql = new StringBuilder();

            string notBlock = (parentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not) ?
               "NOT " : string.Empty;

            var argumentsCount = expression.Arguments.Count;

            // для сокращения объёма кода
            Func<int, string> GetArgument = (f_index) =>
                GetSqlFromExpression(expression.Arguments[f_index], cQuery, expression);
            Func<int, object> GetValueArgument = (f_index) =>
                GetValueFromExpression(expression.Arguments[f_index], expression);
            Func<int, ReadOnlyCollection<Expression>> GetParamsArgument = (f_index) =>
                ((NewArrayExpression)expression.Arguments[f_index]).Expressions;

            switch (expression.Method.Name)
            {
                case nameof(DBFunction.As):
                    Concat(sql, GetArgument(0), " AS ", OpenBlock, GetValueArgument(1), CloseBlock);
                    break;

                case nameof(DBFunction.Desc):
                    Concat(sql, GetArgument(0), " DESC");
                    break;

                case nameof(DBFunction.Distinct):
                    Concat(sql, "DISTINCT ", GetArgument(0));
                    break;

                case nameof(DBFunction.Alias):
                    Concat(sql, OpenBlock, GetValueArgument(0), CloseBlock);
                    break;

                #region Предикаты сравнения

                case nameof(DBFunction.Between):
                    Concat(sql, GetArgument(0), " ", notBlock, "BETWEEN ", GetArgument(1), " AND ", GetArgument(2));
                    break;

                case nameof(DBFunction.Like):
                    Concat(sql, GetArgument(0), " ", notBlock, "LIKE ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, " ESCAPE ", arg);
                        }
                    }
                    break;

                case nameof(DBFunction.StartingWith):
                    Concat(sql, GetArgument(0), ' ', notBlock, "STARTING WITH ", GetArgument(1));
                    break;

                case nameof(DBFunction.Containing):
                    Concat(sql, GetArgument(0), ' ', notBlock, "CONTAINING ", GetArgument(1));
                    break;

                case nameof(DBFunction.SimilarTo):
                    Concat(sql, GetArgument(0), ' ', notBlock, "SIMILAR TO ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, " ESCAPE ", arg);
                        }
                    }
                    break;

                #endregion

                #region Агрегатные функции

                case nameof(DBFunction.Avg):
                    Concat(sql, "AVG(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Count):
                    if (argumentsCount > 0)
                    {
                        Concat(sql, "COUNT(", GetArgument(0), ")");
                    }
                    else
                    {
                        Concat(sql, "COUNT(*)");
                    }
                    break;

                case nameof(DBFunction.List):
                    Concat(sql, "LIST(", GetArgument(0));
                    if (argumentsCount > 1)
                    {
                        Concat(sql, ",", GetArgument(1));
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.Max):
                    Concat(sql, "MAX(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Min):
                    Concat(sql, "MIN(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Sum):
                    Concat(sql, "SUM(", GetArgument(0), ")");
                    break;

                #endregion

                #region Функции для работы со строками

                case nameof(DBFunction.CharLength):
                    Concat(sql, "CHAR_LENGTH(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Hash):
                    Concat(sql, "HASH(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Left):
                    Concat(sql, "LEFT(", GetArgument(0), ",", GetArgument(1), ")");
                    break;

                case nameof(DBFunction.Lower):
                    Concat(sql, "LOWER(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.LPad):
                    Concat(sql, "LPAD(", GetArgument(0), ",", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, ",", arg);
                        }
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.Overlay):
                    Concat(sql, "OVERLAY(", GetArgument(0), " PLACING ", GetArgument(1), " FROM ", GetArgument(2));
                    if (argumentsCount > 3)
                    {
                        var arg = GetArgument(3);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, " FOR ", arg);
                        }
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.Replace):
                    Concat(sql, "REPLACE(", GetArgument(0), ",", GetArgument(1), ",", GetArgument(2), ")");
                    break;

                case nameof(DBFunction.Reverse):
                    Concat(sql, "REVERSE(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Right):
                    Concat(sql, "RIGHT(", GetArgument(0), ",", GetArgument(1), ")");
                    break;

                case nameof(DBFunction.RPad):
                    Concat(sql, "RPAD(", GetArgument(0), ",", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, ",", arg);
                        }
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.SubString):
                    Concat(sql, "SUBSTRING (", GetArgument(0), " FROM ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            Concat(sql, " FOR ", arg);
                        }
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.Upper):
                    Concat(sql, "UPPER(", GetArgument(0), ")");
                    break;

                #endregion

                #region Предикаты существования

                case nameof(DBFunction.Exists):
                    Concat(sql, notBlock, "EXISTS", GetArgument(0));
                    break;

                case nameof(DBFunction.In):
                    var value = GetValueArgument(1);
                    if (value is DBQueryBase subQuery)
                    {
                        Concat(sql, GetArgument(0), ' ', notBlock, "IN", GetSubQuery(subQuery, cQuery));
                    }
                    else if (value is object[] array)
                    {
                        Concat(sql, GetArgument(0), ' ', notBlock, "IN(");
                        for (int i = 0; i < array.Length; i++)
                        {
                            if (i > 0)
                            {
                                Concat(sql, ',');
                            }
                            Concat(sql, GetParameter(array[i], cQuery));
                        }
                        Concat(sql, ")");
                    }
                    break;

                case nameof(DBFunction.Singular):
                    Concat(sql, notBlock, "SINGULAR", GetArgument(0));
                    break;

                #endregion

                #region Количественные предикаты подзапросов

                case nameof(DBFunction.All):
                    Concat(sql, notBlock, "ALL", GetArgument(0));
                    break;

                case nameof(DBFunction.Any):
                    Concat(sql, notBlock, "ANY", GetArgument(0));
                    break;

                case nameof(DBFunction.Some):
                    Concat(sql, notBlock, "SOME", GetArgument(0));
                    break;

                #endregion

                #region Условные функции

                case nameof(DBFunction.Coalesce):
                    Concat(sql, "COALESCE(", GetArgument(0), ',', GetArgument(1));
                    var expressionArray = GetParamsArgument(2);
                    for (int i = 0; i < expressionArray.Count; i++)
                    {
                        Concat(sql, ',');
                        Concat(sql, GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.Decode):
                    Concat(sql, "DECODE(", GetArgument(0));
                    expressionArray = GetParamsArgument(1);
                    for (int i = 0; i < expressionArray.Count; i++)
                    {
                        Concat(sql, ',');
                        Concat(sql, GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.MaxValue):
                    Concat(sql, "MAXVALUE(", GetArgument(0));
                    expressionArray = GetParamsArgument(1);
                    for (int i = 0; i < expressionArray.Count; i++)
                    {
                        Concat(sql, ',');
                        Concat(sql, GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.MinValue):
                    Concat(sql, "MINVALUE(", GetArgument(0));
                    expressionArray = GetParamsArgument(1);
                    for (int i = 0; i < expressionArray.Count; i++)
                    {
                        Concat(sql, ',');
                        Concat(sql, GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    Concat(sql, ")");
                    break;

                case nameof(DBFunction.NullIf):
                    Concat(sql, "NULLIF(", GetArgument(0), ',', GetArgument(0), ')');
                    break;

                    #endregion
            }

            return sql.ToString();
        }
        private void InitializeDictionaries()
        {
            foreach (var table in Tables)
            {
                _selectCommandsDict.Add(table, GetSelectCommandText(table));
                if (table.PrimaryKeyColumn != null)
                {
                    _insertCommandsDict.Add(table, GetInsertCommandText(table));
                    _updateCommandsDict.Add(table, GetUpdateCommandText(table));
                    _deleteCommandsDict.Add(table, GetDeleteCommandText(table));
                }

                _tablesDict.Add(table.Name, table);
                foreach (var column in table.Columns)
                {
                    string fullName = string.Concat(table.Name, '.', column.Name);
                    _columnsDict.Add(fullName, column);
                }
            }
        }

        private Dictionary<DBTable, string> _selectCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _insertCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _updateCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<DBTable, string> _deleteCommandsDict = new Dictionary<DBTable, string>();
        private Dictionary<string, DBTable> _tablesDict = new Dictionary<string, DBTable>();
        private Dictionary<string, DBColumn> _columnsDict = new Dictionary<string, DBColumn>();
    }
}