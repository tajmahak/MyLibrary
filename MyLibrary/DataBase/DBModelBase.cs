using MyLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public DBTable[] Tables { get; private set; }
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
            Tables = GetTableSchema(connection);
            InitializeDictionaries();
            IsInitialized = true;
        }
        public void Initialize(Type[] ormTableTypes)
        {
            Tables = new DBTable[ormTableTypes.Length];
            for (int i = 0; i < Tables.Length; i++)
            {
                var tableType = ormTableTypes[i];
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
                    column.Name = attr.ColumnName.Split('.')[1];
                    column.AllowDBNull = attr.AllowDbNull;

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
        public string GetDefaultSqlQuery(DBTable table, DBQueryType queryType)
        {
            switch (queryType)
            {
                case DBQueryType.Select:
                    return _selectCommandsDict[table];

                case DBQueryType.Insert:
                    return _insertCommandsDict[table];

                case DBQueryType.Update:
                    return _updateCommandsDict[table];

                case DBQueryType.Delete:
                    return _deleteCommandsDict[table];

            }
            throw new NotImplementedException();
        }
        public DbCommand CompileCommand(DbConnection connection, DBQueryBase query)
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
        public bool TryGetTable(string tableName, out DBTable table)
        {
            return _tablesDict.TryGetValue(tableName, out table);
        }
        public bool TryGetColumn(string columnName, out DBColumn column)
        {
            return _columnsDict.TryGetValue(columnName, out column);
        }

        #region [protected] Вспомогательные сущности для получения SQL-команд

        protected virtual string GetInsertCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            AddText(sql, "INSERT INTO ", GetName(table.Name), " VALUES(");

            int index = 0;
            int paramIndex = 0;
            foreach (var column in table.Columns)
            {
                if (index++ > 0)
                {
                    AddText(sql, ',');
                }
                if (column.IsPrimary)
                {
                    AddText(sql, "NULL");
                }
                else
                {
                    AddText(sql, "@p", paramIndex++);
                }
            }
            AddText(sql, ')');
            return sql.ToString();
        }
        protected string GetSelectCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            AddText(sql, "SELECT ", GetName(table.Name), ".* FROM ", GetName(table.Name));
            return sql.ToString();
        }
        protected string GetUpdateCommandText(DBTable table)
        {
            var sql = new StringBuilder();

            AddText(sql, "UPDATE ", GetName(table.Name), " SET ");
            int index = 0;
            foreach (var column in table.Columns)
            {
                if (column.IsPrimary)
                {
                    continue;
                }
                if (index != 0)
                {
                    AddText(sql, ',');
                }
                AddText(sql, GetName(column.Name), "=@p", index++);
            }
            AddText(sql, " WHERE ", GetName(table.PrimaryKeyColumn.Name), "=@id");
            return sql.ToString();
        }
        protected string GetDeleteCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            AddText(sql, "DELETE FROM ", GetName(table.Name), " WHERE ", GetName(table.PrimaryKeyColumn.Name), "=@id");
            return sql.ToString();
        }

        protected void PrepareSelectCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                AddText(sql, _selectCommandsDict[query.Table]);
            }
            else
            {
                AddText(sql, "SELECT ");

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
                                    AddText(sql, ',');
                                }
                                AddText(sql, '*');
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        AddText(sql, ',');
                                    }

                                    var paramCol = (string)block[j];
                                    if (paramCol.Contains("."))
                                    {
                                        // Столбец
                                        AddText(sql, GetFullName(paramCol));
                                    }
                                    else
                                    {
                                        // Таблица
                                        AddText(sql, GetName(paramCol), ".*");
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
                                AddText(sql, ',');
                            }
                            AddText(sql, GetFullName(block[1]), " AS ", GetName(block[0]));
                            index++;
                            break;
                            #endregion
                        case DBQueryStructureType.SelectSum:
                            #region
                            for (int j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    AddText(sql, ',');
                                }
                                AddText(sql, "SUM(", GetFullName(block[j]), ')');
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "SUM(", GetFullName(block[i]), ") AS ", GetName(block[i + 1]));
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "MAX(", GetFullName(block[j]), ')');
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "MAX(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "MIN(", GetFullName(block[j]), ')');
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "MIN(", GetFullName(block[j]), ") AS ", GetName(block[j + 1]));
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, "COUNT(*)");
                                index++;
                            }
                            else
                            {
                                for (int j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        AddText(sql, ',');
                                    }
                                    AddText(sql, "COUNT(", GetFullName(block[j]), ')');
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.Select_expression:
                            #region
                            AddText(sql, ParseExpressionList((Expression)block[0], cQuery).Sql);
                            break;
                            #endregion
                    }
                }
                AddText(sql, " FROM ", GetName(query.Table.Name));
            }
        }
        protected void PrepareInsertCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            AddText(sql, "INSERT INTO ", GetName(query.Table.Name));

            var blockList = FindBlockList(query, DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.InadequateInsertCommandException();
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
                AddText(sql, AddParameter(block[1], cQuery));
            }
            AddText(sql, ')');
        }
        protected void PrepareUpdateCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            AddText(sql, "UPDATE ", GetName(query.Table.Name), " SET ");

            var blockList = FindBlockList(query, DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongUpdateCommandException();
            }

            for (int i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    AddText(sql, ',');
                }
                AddText(sql, GetFullName(block[0]), '=', AddParameter(block[1], cQuery));
            }
        }
        protected void PrepareDeleteCommand(StringBuilder sql, DBQueryBase query)
        {
            AddText(sql, "DELETE FROM ", GetName(query.Table.Name));
        }
        protected void PrepareJoinCommand(StringBuilder sql, DBQueryBase query)
        {
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.InnerJoin:
                        AddText(sql, " INNER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.LeftOuterJoin:
                        AddText(sql, " LEFT OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.RightOuterJoin:
                        AddText(sql, " RIGHT OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.FullOuterJoin:
                        AddText(sql, " FULL OUTER JOIN ", GetName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;


                    case DBQueryStructureType.InnerJoinAs:
                        AddText(sql, " INNER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.LeftOuterJoinAs:
                        AddText(sql, " LEFT OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.RightOuterJoinAs:
                        AddText(sql, " RIGHT OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.FullOuterJoinAs:
                        AddText(sql, " FULL OUTER JOIN ", GetName(block[1]), " AS ", GetName(block[0]), " ON ", GetName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;


                    case DBQueryStructureType.InnerJoin_type:
                    case DBQueryStructureType.LeftOuterJoin_type:
                    case DBQueryStructureType.RightOuterJoin_type:
                    case DBQueryStructureType.FullOuterJoin_type:
                    case DBQueryStructureType.InnerJoinAs_type:
                    case DBQueryStructureType.LeftOuterJoinAs_type:
                    case DBQueryStructureType.RightOuterJoinAs_type:
                    case DBQueryStructureType.FullOuterJoinAs_type:
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
                                AddText(sql, " INNER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftOuterJoin_type:
                                AddText(sql, " LEFT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightOuterJoin_type:
                                AddText(sql, " RIGHT OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullOuterJoin_type:
                                AddText(sql, " FULL OUTER JOIN ", GetName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;


                            case DBQueryStructureType.InnerJoinAs_type:
                                AddText(sql, " INNER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftOuterJoinAs_type:
                                AddText(sql, " LEFT OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightOuterJoinAs_type:
                                AddText(sql, " RIGHT OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullOuterJoinAs_type:
                                AddText(sql, " FULL OUTER JOIN ", GetName(split[0]), " AS ", GetName(block[2]), " ON ", GetName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
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
                AddText(sql, " WHERE ");

                for (int i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    if (i > 0)
                    {
                        AddText(sql, " AND ");
                    }

                    switch (block.Type)
                    {
                        case DBQueryStructureType.Where_expression:
                            #region
                            AddText(sql, ParseExpression(false, (Expression)block[0], null, cQuery).Sql);
                            break;
                        #endregion
                        case DBQueryStructureType.Where:
                            #region
                            block[2] = block[2] ?? DBNull.Value;

                            AddText(sql, GetFullName(block[0]));

                            if ((block[2] is DBNull) && ((string)block[1]) == "=")
                            {
                                AddText(sql, " IS NULL");
                            }
                            else if (block[2] is DBNull && ((string)block[1]) == "<>")
                            {
                                AddText(sql, " IS NOT NULL");
                            }
                            else
                            {
                                AddText(sql, block[1], AddParameter(block[2], cQuery));
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.WhereBetween:
                            #region
                            AddText(sql, GetFullName(block[0]), "BETWEEN ", AddParameter(block[1], cQuery), " AND ", AddParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereUpper:
                            #region
                            AddText(sql, "UPPER(", GetFullName(block[0]), ")=", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContaining:
                            #region
                            AddText(sql, GetFullName(block[0]), " CONTAINING ", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContainingUpper:
                            #region
                            AddText(sql, " UPPER(", GetFullName(block[0]), ") CONTAINING ", AddParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLike:
                            #region
                            AddText(sql, GetFullName(block[0]), " LIKE \'", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLikeUpper:
                            #region
                            AddText(sql, " UPPER(", GetFullName(block[0]), ") LIKE '", block[1], '\'');
                            break;
                        #endregion
                        case DBQueryStructureType.WhereIn_command:
                            #region
                            AddText(sql, GetFullName(block[0]), " IN ");
                            AddText(sql, AddSubQuery((DBQueryBase)block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereIn_values:
                            #region
                            AddText(sql, GetFullName(block[0]), " IN (");
                            #region Добавление списка значений

                            var values = (object[])block[1];
                            for (int j = 0; j < values.Length; j++)
                            {
                                if (j > 0)
                                {
                                    AddText(sql, ',');
                                }

                                var value = values[j];
                                if (value.GetType().IsPrimitive)
                                {
                                    AddText(sql, value);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            #endregion
                            AddText(sql, ')');
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
                AddText(sql, " GROUP BY ");
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
                                    AddText(sql, ',');
                                }
                                AddText(sql, GetFullName(args[j]));
                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.GroupBy_expression:
                            AddText(sql, ParseExpressionList((Expression)block[0], null).Sql);
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
                AddText(sql, " ORDER BY ");
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
                                    AddText(sql, ',');
                                }
                                switch (block.Type)
                                {
                                    case DBQueryStructureType.OrderBy:
                                        AddText(sql, GetFullName(args[j])); break;
                                    case DBQueryStructureType.OrderByDesc:
                                        AddText(sql, GetFullName(args[j]), " DESC"); break;
                                    case DBQueryStructureType.OrderByUpper:
                                        AddText(sql, "UPPER(", GetFullName(args[j]), ")"); break;
                                    case DBQueryStructureType.OrderByUpperDesc:
                                        AddText(sql, "UPPER(", GetFullName(block[1]), ") DESC"); break;
                                }

                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.OrderBy_expression:
                            AddText(sql, ParseExpressionList((Expression)block[0], null).Sql);
                            break;
                    }
                }
            }
        }
        protected void PrepareUnionCommand(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = FindBlockList(query, DBQueryStructureType.UnionAll);
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.UnionAll:
                        AddText(sql, " UNION ALL ", AddSubQuery((DBQueryBase)block[0], cQuery));
                        break;

                    case DBQueryStructureType.UnionDistinct:
                        AddText(sql, " UNION DISTINCT ", AddSubQuery((DBQueryBase)block[0], cQuery));
                        break;
                }
            }
        }

        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, Predicate<DBQueryStructureType> predicate)
        {
            return query.Structure.FindAll(x => predicate(x.Type));
        }
        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, DBQueryStructureType type)
        {
            return FindBlockList(query, x => x == type);
        }
        protected List<DBQueryStructureBlock> FindBlockList(DBQueryBase query, Predicate<string> predicate)
        {
            return query.Structure.FindAll(x => predicate(x.Type.ToString()));
        }
        protected DBQueryStructureBlock FindBlock(DBQueryBase query, Predicate<DBQueryStructureType> type)
        {
            return query.Structure.Find(x => type(x.Type));
        }
        protected DBQueryStructureBlock FindBlock(DBQueryBase query, DBQueryStructureType type)
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
            var parameter = new DBParameter()
            {
                Name = string.Concat("@p", paramNumber),
                Value = value,
            };
            cQuery.Parameters.Add(parameter);

            return parameter.Name;
        }
        protected string AddSubQuery(DBQueryBase subQuery, DBCompiledQuery cQuery)
        {
            var subCQuery = CompileQuery(subQuery, cQuery.Parameters.Count);
            cQuery.Parameters.AddRange(subCQuery.Parameters);
            return string.Concat('(', subCQuery.CommandText, ')');
        }
        protected void AddText(StringBuilder str, params object[] values)
        {
            foreach (var value in values)
            {
                str.Append(value);
            }
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

        #endregion


        private StringBuilder GetSqlFromExpression(Expression expression, Expression parentExpression, DBCompiledQuery cQuery)
        {
            return null;
        }
        private object GetValueFromExpression(Expression expression, Expression parentExpression)
        {
            return null;
        }




        private ParseExpressionResult ParseExpression(bool parseValue, Expression expression, Expression parentExpression, DBCompiledQuery cQuery)
        {
            var result = new ParseExpressionResult();

            if (expression is BinaryExpression binaryExpression)
            {
                #region

                AddText(result.Sql, '(', ParseExpression(false, binaryExpression.Left, expression, cQuery).Sql);

                var rightBlock = ParseExpression(false, binaryExpression.Right, expression, cQuery).Sql;
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

                    AddText(result.Sql, @operator, rightBlock);
                }
                else
                {
                    if (binaryExpression.NodeType == ExpressionType.NotEqual)
                    {
                        AddText(result.Sql, " IS NOT NULL");
                    }
                    else
                    {
                        AddText(result.Sql, " IS NULL");
                    }
                }

                AddText(result.Sql, ')');

                #endregion
            }
            else if (expression is MemberExpression memberExpression)
            {
                #region

                if (memberExpression.Expression is ParameterExpression)
                {
                    var custAttr = memberExpression.Member.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    var attr = (DBOrmColumnAttribute)custAttr[0];
                    AddText(result.Sql, GetFullName(attr.ColumnName));
                }
                else if (memberExpression.Member is PropertyInfo)
                {
                    var propertyInfo = memberExpression.Member as PropertyInfo;

                    object value;
                    if (memberExpression.Expression != null)
                    {
                        var innerInfo = ParseExpression(true, memberExpression.Expression, expression, cQuery);
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
                        AddText(result.Sql, AddParameter(value, cQuery));
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
                        if (value is DBQueryBase subQuery)
                        {
                            AddText(result.Sql, AddSubQuery(subQuery, cQuery));
                        }
                        else
                        {
                            AddText(result.Sql, AddParameter(value, cQuery));
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
                    result.Value = value;
                    return result;
                }
                else
                {
                    if (value != null)
                    {
                        AddText(result.Sql, AddParameter(value, cQuery));
                    }
                    return result;
                }

                #endregion
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                #region

                if (parseValue)
                {
                    return ParseExpression(true, unaryExpression.Operand, expression, cQuery);
                }
                else
                {
                    AddText(result.Sql, ParseExpression(false, unaryExpression.Operand, expression, cQuery).Sql);
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
                    AddText(result.Sql, GetFullName(attr.ColumnName));
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
                    AddText(result.Sql, ParseExpressionFunction(methodCallExpression, parentExpression, cQuery).Sql);
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                    {
                        // Извлечение объекта из дерева выражений
                        obj = ParseExpression(true, methodCallExpression.Object, expression, cQuery).Value;
                    }

                    var arguments = new object[methodCallExpression.Arguments.Count];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        arguments[i] = ParseExpression(true, methodCallExpression.Arguments[i], expression, cQuery).Value;
                    }

                    var value = methodCallExpression.Method.Invoke(obj, arguments);

                    if (parseValue)
                    {
                        result.Value = value;
                        return result;
                    }
                    else
                    {
                        AddText(result.Sql, AddParameter(value, cQuery));
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
                        var value = ParseExpression(true, newArrayExpression.Expressions[i], expression, cQuery).Value;
                        array.SetValue(value, i);
                    }
                    result.Value = array;
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
                        AddText(result.Sql, ',');
                    }
                    AddText(result.Sql, ParseExpression(false, exprArg, expression, cQuery).Sql);
                }
            }
            else
            {
                AddText(result.Sql, ParseExpression(false, expression, null, cQuery).Sql);
            }

            return result;
        }
        private ParseExpressionResult ParseExpressionFunction(MethodCallExpression expression, Expression parentExpression, DBCompiledQuery cQuery)
        {
            var result = new ParseExpressionResult();

            string notBlock = (parentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not) ?
               "NOT " : string.Empty;

            var argumentsCount = expression.Arguments.Count;

            // для сокращения объёма кода
            Func<int, string> GetArgument = (f_index) =>
                ParseExpression(false, expression.Arguments[f_index], expression, cQuery).Sql.ToString();
            Func<int, object> GetValueArgument = (f_index) =>
                ParseExpression(true, expression.Arguments[f_index], expression, cQuery).Value;
            Func<int, ReadOnlyCollection<Expression>> GetParamsArgument = (f_index) =>
                ((NewArrayExpression)expression.Arguments[f_index]).Expressions;

            switch (expression.Method.Name)
            {
                case nameof(DBFunction.As):
                    AddText(result.Sql, GetArgument(0), " AS ", OpenBlock, GetValueArgument(1), CloseBlock);
                    break;

                case nameof(DBFunction.Desc):
                    AddText(result.Sql, GetArgument(0), " DESC");
                    break;

                case nameof(DBFunction.Distinct):
                    AddText(result.Sql, "DISTINCT ", GetArgument(0));
                    break;

                #region Предикаты сравнения

                case nameof(DBFunction.Between):
                    AddText(result.Sql, GetArgument(0), " ", notBlock, "BETWEEN ", GetArgument(1), " AND ", GetArgument(2));
                    break;

                case nameof(DBFunction.Like):
                    AddText(result.Sql, GetArgument(0), " ", notBlock, "LIKE ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, " ESCAPE ", arg);
                        }
                    }
                    break;

                case nameof(DBFunction.StartingWith):
                    AddText(result.Sql, GetArgument(0), ' ', notBlock, "STARTING WITH ", GetArgument(1));
                    break;

                case nameof(DBFunction.Containing):
                    AddText(result.Sql, GetArgument(0), ' ', notBlock, "CONTAINING ", GetArgument(1));
                    break;

                case nameof(DBFunction.SimilarTo):
                    AddText(result.Sql, GetArgument(0), ' ', notBlock, "SIMILAR TO ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, " ESCAPE ", arg);
                        }
                    }
                    break;

                #endregion

                #region Агрегатные функции

                case nameof(DBFunction.Avg):
                    AddText(result.Sql, "AVG(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Count):
                    if (argumentsCount > 0)
                    {
                        AddText(result.Sql, "COUNT(", GetArgument(0), ")");
                    }
                    else
                    {
                        AddText(result.Sql, "COUNT(*)");
                    }
                    break;

                case nameof(DBFunction.List):
                    AddText(result.Sql, "LIST(", GetArgument(0));
                    if (argumentsCount > 1)
                    {
                        AddText(result.Sql, ",", GetArgument(1));
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.Max):
                    AddText(result.Sql, "MAX(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Min):
                    AddText(result.Sql, "MIN(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Sum):
                    AddText(result.Sql, "SUM(", GetArgument(0), ")");
                    break;

                #endregion

                #region Функции для работы со строками

                case nameof(DBFunction.CharLength):
                    AddText(result.Sql, "CHAR_LENGTH(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Hash):
                    AddText(result.Sql, "HASH(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Left):
                    AddText(result.Sql, "LEFT(", GetArgument(0), ",", GetArgument(1), ")");
                    break;

                case nameof(DBFunction.Lower):
                    AddText(result.Sql, "LOWER(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.LPad):
                    AddText(result.Sql, "LPAD(", GetArgument(0), ",", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, ",", arg);
                        }
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.Overlay):
                    AddText(result.Sql, "OVERLAY(", GetArgument(0), " PLACING ", GetArgument(1), " FROM ", GetArgument(2));
                    if (argumentsCount > 3)
                    {
                        var arg = GetArgument(3);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, " FOR ", arg);
                        }
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.Replace):
                    AddText(result.Sql, "REPLACE(", GetArgument(0), ",", GetArgument(1), ",", GetArgument(2), ")");
                    break;

                case nameof(DBFunction.Reverse):
                    AddText(result.Sql, "REVERSE(", GetArgument(0), ")");
                    break;

                case nameof(DBFunction.Right):
                    AddText(result.Sql, "RIGHT(", GetArgument(0), ",", GetArgument(1), ")");
                    break;

                case nameof(DBFunction.RPad):
                    AddText(result.Sql, "RPAD(", GetArgument(0), ",", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, ",", arg);
                        }
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.SubString):
                    AddText(result.Sql, "SUBSTRING (", GetArgument(0), " FROM ", GetArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = GetArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            AddText(result.Sql, " FOR ", arg);
                        }
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.Upper):
                    AddText(result.Sql, "UPPER(", GetArgument(0), ")");
                    break;

                #endregion

                #region Предикаты существования

                case nameof(DBFunction.Exists):
                    AddText(result.Sql, notBlock, "EXISTS", GetArgument(0));
                    break;

                case nameof(DBFunction.In):
                    var value = GetValueArgument(1);
                    if (value is DBQueryBase subQuery)
                    {
                        AddText(result.Sql, GetArgument(0), ' ', notBlock, "IN", AddSubQuery(subQuery, cQuery));
                    }
                    else if (value is object[] arrayValue)
                    {
                        AddText(result.Sql, GetArgument(0), ' ', notBlock, "IN(");
                        for (int i = 0; i < arrayValue.Length; i++)
                        {
                            if (i > 0)
                            {
                                AddText(result.Sql, ',');
                            }
                            AddText(result.Sql, AddParameter(arrayValue[i], cQuery));
                        }
                        AddText(result.Sql, ")");
                    }
                    break;

                case nameof(DBFunction.Singular):
                    AddText(result.Sql, notBlock, "SINGULAR", GetArgument(0));
                    break;

                #endregion

                #region Количественные предикаты подзапросов

                case nameof(DBFunction.All):
                    AddText(result.Sql, notBlock, "ALL", GetArgument(0));
                    break;

                case nameof(DBFunction.Any):
                    AddText(result.Sql, notBlock, "ANY", GetArgument(0));
                    break;

                case nameof(DBFunction.Some):
                    AddText(result.Sql, notBlock, "SOME", GetArgument(0));
                    break;

                #endregion

                #region Условные функции

                case nameof(DBFunction.Coalesce):
                    AddText(result.Sql, "COALESCE(", GetArgument(0), ',', GetArgument(1));
                    var array = GetParamsArgument(2);
                    for (int i = 0; i < array.Count; i++)
                    {
                        AddText(result.Sql, ',');
                        AddText(result.Sql, ParseExpression(false, array[i], expression, cQuery).Sql);
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.Decode):
                    AddText(result.Sql, "DECODE(", GetArgument(0));
                    array = GetParamsArgument(1);
                    for (int i = 0; i < array.Count; i++)
                    {
                        AddText(result.Sql, ',');
                        AddText(result.Sql, ParseExpression(false, array[i], expression, cQuery).Sql);
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.MaxValue):
                    AddText(result.Sql, "MAXVALUE(", GetArgument(0));
                    array = GetParamsArgument(1);
                    for (int i = 0; i < array.Count; i++)
                    {
                        AddText(result.Sql, ',');
                        AddText(result.Sql, ParseExpression(false, array[i], expression, cQuery).Sql);
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.MinValue):
                    AddText(result.Sql, "MINVALUE(", GetArgument(0));
                    array = GetParamsArgument(1);
                    for (int i = 0; i < array.Count; i++)
                    {
                        AddText(result.Sql, ',');
                        AddText(result.Sql, ParseExpression(false, array[i], expression, cQuery).Sql);
                    }
                    AddText(result.Sql, ")");
                    break;

                case nameof(DBFunction.NullIf):
                    AddText(result.Sql, "NULLIF(", GetArgument(0), ',', GetArgument(0), ')');
                    break;

                    #endregion
            }

            return result;
        }
        private void InitializeDictionaries()
        {
            foreach (var table in Tables)
            {
                _selectCommandsDict.Add(table, GetSelectCommandText(table));
                _updateCommandsDict.Add(table, GetUpdateCommandText(table));
                _deleteCommandsDict.Add(table, GetDeleteCommandText(table));
                _insertCommandsDict.Add(table, GetInsertCommandText(table));

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

        private class ParseExpressionResult
        {
            public StringBuilder Sql = new StringBuilder();
            public object Value;
        }
    }
}