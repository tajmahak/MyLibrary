using MyLibrary.Data;
using System;
using System.Collections.Generic;
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
    public abstract class DBProvider
    {
        public DBTableCollection Tables { get; private set; } = new DBTableCollection();
        public DBColumnCollection Columns { get; private set; } = new DBColumnCollection();
        public string OpenBlock { get; protected set; } = string.Empty;
        public string CloseBlock { get; protected set; } = string.Empty;
        public bool Initialized { get; private set; }
        private readonly Dictionary<DBTable, string> _selectCommandsDict = new Dictionary<DBTable, string>();
        private readonly Dictionary<DBTable, string> _insertCommandsDict = new Dictionary<DBTable, string>();
        private readonly Dictionary<DBTable, string> _updateCommandsDict = new Dictionary<DBTable, string>();
        private readonly Dictionary<DBTable, string> _deleteCommandsDict = new Dictionary<DBTable, string>();

        public abstract void FillTableSchema(DbConnection connection);
        public abstract DbParameter CreateParameter(string name, object value);
        public abstract DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0);
        public virtual object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
        }

        public void Initialize(DbConnection connection)
        {
            Tables.Clear();
            FillTableSchema(connection);
            InitializeDictionaries();
            Initialized = true;
        }
        public void Initialize(Type dbType)
        {
            var ormTableTypes = new List<Type>();

            var members = dbType.GetMembers(BindingFlags.NonPublic);
            foreach (Type member in members)
            {
                var baseType = member.BaseType;
                while (baseType != null)
                {
                    if (baseType == typeof(DBOrmRow))
                    {
                        ormTableTypes.Add(member);
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            Tables.Clear();
            for (var i = 0; i < ormTableTypes.Count; i++)
            {
                var tableType = ormTableTypes[i];
                var table = new DBTable()
                {
                    Name = DBInternal.GetTableNameFromAttribute(tableType)
                };

                foreach (var columnProperty in tableType.GetProperties())
                {
                    var attrList = columnProperty.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    if (attrList.Length == 0)
                    {
                        continue;
                    }

                    var attr = (DBOrmColumnAttribute)attrList[0];
                    var column = new DBColumn(table)
                    {
                        Name = attr.ColumnName.Split('.')[1],
                        NotNull = attr.NotNull
                    };

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

                    table.Columns.Add(column);
                }
                Tables.Add(table);
            }

            InitializeDictionaries();
            Initialized = true;
        }
        public string GetDefaultSqlQuery(DBTable table, StatementType statementType)
        {
            if (statementType != StatementType.Select && table.PrimaryKeyColumn == null)
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
            var dbCommand = connection.CreateCommand();
            dbCommand.CommandText = compiledQuery.CommandText;
            foreach (var parameter in compiledQuery.Parameters)
            {
                var dbParameter = CreateParameter(parameter.Name, parameter.Value);
                dbCommand.Parameters.Add(dbParameter);
            }
            return dbCommand;
        }
        public DBContext CreateDBContext(DbConnection connection)
        {
            var context = new DBContext(this, connection);
            return context;
        }

        protected virtual string GetInsertCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            sql.Concat("INSERT INTO ", GetShortName(table.Name), " VALUES(");

            var index = 0;
            var paramIndex = 0;
            foreach (var column in table.Columns)
            {
                if (index++ > 0)
                {
                    sql.Concat(',');
                }
                if (column.IsPrimary)
                {
                    sql.Concat("NULL");
                }
                else
                {
                    sql.Concat("@p", paramIndex++);
                }
            }
            sql.Concat(')');
            return sql.ToString();
        }
        protected string GetSelectCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            sql.Concat("SELECT ", GetShortName(table.Name), ".* FROM ", GetShortName(table.Name));
            return sql.ToString();
        }
        protected string GetUpdateCommandText(DBTable table)
        {
            var sql = new StringBuilder();

            sql.Concat("UPDATE ", GetShortName(table.Name), " SET ");
            var index = 0;
            foreach (var column in table.Columns)
            {
                if (!column.IsPrimary)
                {
                    if (index != 0)
                    {
                        sql.Concat(',');
                    }
                    sql.Concat(GetShortName(column.Name), "=@p", index++);
                }
            }
            sql.Concat(" WHERE ", GetShortName(table.PrimaryKeyColumn.Name), "=@id");
            return sql.ToString();
        }
        protected string GetDeleteCommandText(DBTable table)
        {
            var sql = new StringBuilder();
            sql.Concat("DELETE FROM ", GetShortName(table.Name), " WHERE ", GetShortName(table.PrimaryKeyColumn.Name), "=@id");
            return sql.ToString();
        }
        private void InitializeDictionaries()
        {
            Columns.Clear();
            _selectCommandsDict.Clear();
            _insertCommandsDict.Clear();
            _updateCommandsDict.Clear();
            _deleteCommandsDict.Clear();
            foreach (var table in Tables)
            {
                _selectCommandsDict.Add(table, GetSelectCommandText(table));
                if (table.PrimaryKeyColumn != null)
                {
                    _insertCommandsDict.Add(table, GetInsertCommandText(table));
                    _updateCommandsDict.Add(table, GetUpdateCommandText(table));
                    _deleteCommandsDict.Add(table, GetDeleteCommandText(table));
                }

                foreach (var column in table.Columns)
                {
                    Columns.Add(column);
                }
            }
        }

        #region Вспомогательные сущности для получения SQL-команд

        protected void PrepareSelectBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = query.Structure.FindAll(x => x.StartsWith("Select"));
            if (blockList.Count == 0)
            {
                sql.Concat(_selectCommandsDict[query.Table]);
            }
            else
            {
                sql.Concat("SELECT ");

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
                                    sql.Concat(',');
                                }
                                sql.Concat('*');
                                index++;
                            }
                            else
                            {
                                for (var j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        sql.Concat(',');
                                    }

                                    var paramCol = (string)block[j];
                                    if (paramCol.Contains("."))
                                    {
                                        // Столбец
                                        sql.Concat(GetFullName(paramCol));
                                    }
                                    else
                                    {
                                        // Таблица
                                        sql.Concat(GetShortName(paramCol), ".*");
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
                                sql.Concat(',');
                            }
                            sql.Concat(GetFullName(block[1]), " AS ", GetShortName(block[0]));
                            index++;
                            break;
                        #endregion
                        case DBQueryStructureType.SelectSum:
                            #region
                            for (var j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("SUM(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectSumAs:
                            #region
                            for (var i = 0; i < block.Length; i += 2)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("SUM(", GetFullName(block[i]), ") AS ", GetShortName(block[i + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMax:
                            #region
                            for (var j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("MAX(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMaxAs:
                            #region
                            for (var j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("MAX(", GetFullName(block[j]), ") AS ", GetShortName(block[j + 1]));
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMin:
                            #region
                            for (var j = 0; j < block.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("MIN(", GetFullName(block[j]), ')');
                                index++;
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectMinAs:
                            #region
                            for (var j = 0; j < block.Length; j += 2)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat("MIN(", GetFullName(block[j]), ") AS ", GetShortName(block[j + 1]));
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
                                    sql.Concat(',');
                                }
                                sql.Concat("COUNT(*)");
                                index++;
                            }
                            else
                            {
                                for (var j = 0; j < block.Length; j++)
                                {
                                    if (index > 0)
                                    {
                                        sql.Concat(',');
                                    }
                                    sql.Concat("COUNT(", GetFullName(block[j]), ')');
                                    index++;
                                }
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.SelectExpression:
                            #region
                            sql.Concat(GetStringListFromExpression(block[0], cQuery));
                            break;
                            #endregion
                    }
                }
                sql.Concat(" FROM ", GetShortName(query.Table.Name));
            }
        }
        protected void PrepareInsertBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            sql.Concat("INSERT INTO ", GetShortName(query.Table.Name), '(');

            var blockList = query.Structure.FindAll(DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongInsertCommandException();
            }

            for (var i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    sql.Concat(',');
                }
                sql.Concat(GetColumnName(block[0]));
            }
            sql.Concat(")VALUES(");
            for (var i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    sql.Concat(',');
                }
                sql.Concat(GetParameter(block[1], cQuery));
            }
            sql.Concat(')');
        }
        protected void PrepareUpdateBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            sql.Concat("UPDATE ", GetShortName(query.Table.Name), " SET ");

            var blockList = query.Structure.FindAll(DBQueryStructureType.Set);
            if (blockList.Count == 0)
            {
                throw DBInternal.WrongUpdateCommandException();
            }

            for (var i = 0; i < blockList.Count; i++)
            {
                var block = blockList[i];
                if (i > 0)
                {
                    sql.Concat(',');
                }
                sql.Concat(GetFullName(block[0]), '=', GetParameter(block[1], cQuery));
            }
        }
        protected void PrepareDeleteBlock(StringBuilder sql, DBQueryBase query)
        {
            sql.Concat("DELETE FROM ", GetShortName(query.Table.Name));
        }
        protected void PrepareJoinBlock(StringBuilder sql, DBQueryBase query)
        {
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.InnerJoin:
                        sql.Concat(" INNER JOIN ", GetShortName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.LeftJoin:
                        sql.Concat(" LEFT JOIN ", GetShortName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.RightJoin:
                        sql.Concat(" RIGHT JOIN ", GetShortName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;

                    case DBQueryStructureType.FullJoin:
                        sql.Concat(" FULL JOIN ", GetShortName(block[0]), " ON ", GetFullName(block[0]), '=', GetFullName(block[1]));
                        break;


                    case DBQueryStructureType.InnerJoinAs:
                        sql.Concat(" INNER JOIN ", GetShortName(block[1]), " AS ", GetShortName(block[0]), " ON ", GetShortName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.LeftJoinAs:
                        sql.Concat(" LEFT JOIN ", GetShortName(block[1]), " AS ", GetShortName(block[0]), " ON ", GetShortName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.RightJoinAs:
                        sql.Concat(" RIGHT JOIN ", GetShortName(block[1]), " AS ", GetShortName(block[0]), " ON ", GetShortName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.FullJoinAs:
                        sql.Concat(" FULL JOIN ", GetShortName(block[1]), " AS ", GetShortName(block[0]), " ON ", GetShortName(block[0]), ".", GetColumnName(block[1]), '=', GetFullName(block[2]));
                        break;

                    case DBQueryStructureType.InnerJoinType:
                    case DBQueryStructureType.LeftJoinType:
                    case DBQueryStructureType.RightJoinType:
                    case DBQueryStructureType.FullJoinType:
                    case DBQueryStructureType.InnerJoinAsType:
                    case DBQueryStructureType.LeftJoinAsType:
                    case DBQueryStructureType.RightJoinAsType:
                    case DBQueryStructureType.FullJoinAsType:
                        #region
                        var foreignKey = DBInternal.GetForeignKey((Type)block[0], (Type)block[1]);
                        if (foreignKey == null)
                        {
                            throw DBInternal.ForeignKeyException();
                        }
                        var split = foreignKey[1].Split('.');
                        switch (block.Type)
                        {
                            case DBQueryStructureType.InnerJoinType:
                                sql.Concat(" INNER JOIN ", GetShortName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftJoinType:
                                sql.Concat(" LEFT JOIN ", GetShortName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightJoinType:
                                sql.Concat(" RIGHT JOIN ", GetShortName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullJoinType:
                                sql.Concat(" FULL JOIN ", GetShortName(split[0]), " ON ", GetFullName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;


                            case DBQueryStructureType.InnerJoinAsType:
                                sql.Concat(" INNER JOIN ", GetShortName(split[0]), " AS ", GetShortName(block[2]), " ON ", GetShortName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.LeftJoinAsType:
                                sql.Concat(" LEFT JOIN ", GetShortName(split[0]), " AS ", GetShortName(block[2]), " ON ", GetShortName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.RightJoinAsType:
                                sql.Concat(" RIGHT JOIN ", GetShortName(split[0]), " AS ", GetShortName(block[2]), " ON ", GetShortName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;

                            case DBQueryStructureType.FullJoinAsType:
                                sql.Concat(" FULL JOIN ", GetShortName(split[0]), " AS ", GetShortName(block[2]), " ON ", GetShortName(block[2]), ".", GetColumnName(foreignKey[1]), '=', GetFullName(foreignKey[0]));
                                break;
                        }
                        break;
                        #endregion
                }
            }
        }
        protected void PrepareWhereBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var blockList = query.Structure.FindAll(x => x.StartsWith("Where"));

            if (blockList.Count > 0)
            {
                sql.Concat(" WHERE ");

                for (var i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    if (i > 0)
                    {
                        sql.Concat(" AND ");
                    }

                    switch (block.Type)
                    {
                        case DBQueryStructureType.WhereExpression:
                            #region
                            sql.Concat(GetSqlFromExpression(block[0], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.Where:
                            #region
                            block[2] = block[2] ?? DBNull.Value;

                            sql.Concat(GetFullName(block[0]));

                            if ((block[2] is DBNull) && ((string)block[1]) == "=")
                            {
                                sql.Concat(" IS NULL");
                            }
                            else if (block[2] is DBNull && ((string)block[1]) == "<>")
                            {
                                sql.Concat(" IS NOT NULL");
                            }
                            else
                            {
                                sql.Concat(block[1], GetParameter(block[2], cQuery));
                            }
                            break;
                        #endregion
                        case DBQueryStructureType.WhereBetween:
                            #region
                            sql.Concat(GetFullName(block[0]), " BETWEEN ", GetParameter(block[1], cQuery), " AND ", GetParameter(block[2], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereUpper:
                            #region
                            sql.Concat("UPPER(", GetFullName(block[0]), ")=", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContaining:
                            #region
                            sql.Concat(GetFullName(block[0]), " CONTAINING ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereContainingUpper:
                            #region
                            sql.Concat("UPPER(", GetFullName(block[0]), ") CONTAINING ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLike:
                            #region
                            sql.Concat(GetFullName(block[0]), " LIKE ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereLikeUpper:
                            #region
                            sql.Concat("UPPER(", GetFullName(block[0]), ") LIKE ", GetParameter(block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereInQuery:
                            #region
                            sql.Concat(GetFullName(block[0]), " IN ");
                            sql.Concat(GetSubQuery((DBQueryBase)block[1], cQuery));
                            break;
                        #endregion
                        case DBQueryStructureType.WhereInValues:
                            #region
                            sql.Concat(GetFullName(block[0]), " IN (");
                            #region Добавление списка значений

                            var values = (object[])block[1];
                            for (var j = 0; j < values.Length; j++)
                            {
                                if (j > 0)
                                {
                                    sql.Concat(',');
                                }

                                var value = values[j];
                                if (value.GetType().IsPrimitive)
                                {
                                    sql.Concat(value);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            #endregion
                            sql.Concat(')');
                            break;
                            #endregion
                    }
                }
            }
        }
        protected void PrepareOrderByBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.Structure.FindAll(x => x.StartsWith("OrderBy"));
            if (blockList.Count > 0)
            {
                sql.Concat(" ORDER BY ");
                var index = 0;
                for (var i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureType.OrderBy:
                        case DBQueryStructureType.OrderByDescending:
                        case DBQueryStructureType.OrderByUpper:
                        case DBQueryStructureType.OrderByUpperDesc:
                            #region
                            var args = (string[])block.Args;
                            for (var j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                switch (block.Type)
                                {
                                    case DBQueryStructureType.OrderBy:
                                        sql.Concat(GetFullName(args[j])); break;
                                    case DBQueryStructureType.OrderByDescending:
                                        sql.Concat(GetFullName(args[j]), " DESC"); break;
                                    case DBQueryStructureType.OrderByUpper:
                                        sql.Concat("UPPER(", GetFullName(args[j]), ")"); break;
                                    case DBQueryStructureType.OrderByUpperDesc:
                                        sql.Concat("UPPER(", GetFullName(block[1]), ") DESC"); break;
                                }

                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.OrderByExpression:
                            #region
                            if (index > 0)
                            {
                                sql.Concat(',');
                            }
                            sql.Concat(GetStringListFromExpression(block[0], null));
                            index++;
                            break;
                        #endregion

                        case DBQueryStructureType.OrderByDescendingExpression:
                            #region
                            var list = GetListFromExpression(block[0], null);
                            for (var j = 0; j < list.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat(list[i], " DESC");
                                index++;
                            }
                            break;
                            #endregion
                    }
                }
            }
        }
        protected void PrepareGroupByBlock(StringBuilder sql, DBQueryBase query)
        {
            var blockList = query.Structure.FindAll(x => x.StartsWith("GroupBy"));
            if (blockList.Count > 0)
            {
                sql.Concat(" GROUP BY ");
                var index = 0;
                for (var i = 0; i < blockList.Count; i++)
                {
                    var block = blockList[i];
                    switch (block.Type)
                    {
                        case DBQueryStructureType.GroupBy:
                            #region
                            var args = (string[])block.Args;
                            for (var j = 0; j < args.Length; j++)
                            {
                                if (index > 0)
                                {
                                    sql.Concat(',');
                                }
                                sql.Concat(GetFullName(args[j]));
                                index++;
                            }
                            break;
                        #endregion

                        case DBQueryStructureType.GroupByExpression:
                            sql.Concat(GetStringListFromExpression(block[0], null));
                            break;
                    }
                }
            }
        }
        protected void PrepareHavingBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            var block = query.Structure.Find(x => x == DBQueryStructureType.HavingExpression);
            if (block != null)
            {
                sql.Concat(" HAVING ", GetSqlFromExpression(block[0], cQuery));
            }
        }
        protected void PrepareUnionBlock(StringBuilder sql, DBQueryBase query, DBCompiledQuery cQuery)
        {
            foreach (var block in query.Structure)
            {
                switch (block.Type)
                {
                    case DBQueryStructureType.UnionAll:
                        sql.Concat(" UNION ALL ", GetSubQuery((DBQueryBase)block[0], cQuery));
                        break;

                    case DBQueryStructureType.UnionDistinct:
                        sql.Concat(" UNION DISTINCT ", GetSubQuery((DBQueryBase)block[0], cQuery));
                        break;
                }
            }
        }

        protected string GetFullName(object value)
        {
            var split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[0], CloseBlock, '.', OpenBlock, split[1], CloseBlock);
        }
        protected string GetShortName(object value)
        {
            var split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[0], CloseBlock);
        }
        protected string GetColumnName(object value)
        {
            var split = ((string)value).Split('.');
            return string.Concat(OpenBlock, split[1], CloseBlock);
        }
        protected string GetParameter(object value, DBCompiledQuery cQuery)
        {
            value = value ?? DBNull.Value;

            if (value is string stringValue && Columns.Contains(stringValue))
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
        protected string GetStringListFromExpression(object expression, DBCompiledQuery cQuery)
        {
            var sql = new StringBuilder();

            var list = GetListFromExpression(expression, cQuery);
            for (var i = 0; i < list.Length; i++)
            {
                if (sql.Length > 0)
                {
                    sql.Concat(',');
                }
                sql.Concat(list[i]);
            }

            return sql.ToString();
        }
        protected string[] GetListFromExpression(object expression, DBCompiledQuery cQuery)
        {
            var list = new List<string>();

            if (expression is NewArrayExpression newArrayExpression)
            {
                foreach (var exprArg in newArrayExpression.Expressions)
                {
                    list.Add(GetSqlFromExpression(exprArg, cQuery, expression));
                }
            }
            else if (expression is NewExpression newExpression)
            {
                foreach (var exprArg in newExpression.Arguments)
                {
                    list.Add(GetSqlFromExpression(exprArg, cQuery, expression));
                }
            }
            else
            {
                list.Add(GetSqlFromExpression(expression, cQuery));
            }

            return list.ToArray();
        }

        private object ParseExpression(bool parseValue, Expression expression, DBCompiledQuery cQuery, Expression parentExpression)
        {
            var sql = new StringBuilder();

            if (expression is BinaryExpression binaryExpression)
            {
                #region

                sql.Concat('(', GetSqlFromExpression(binaryExpression.Left, cQuery, expression));

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

                    sql.Concat(@operator, rightBlock);
                }
                else
                {
                    if (binaryExpression.NodeType == ExpressionType.NotEqual)
                    {
                        sql.Concat(" IS NOT NULL");
                    }
                    else
                    {
                        sql.Concat(" IS NULL");
                    }
                }

                sql.Concat(')');

                #endregion
            }
            else if (expression is MemberExpression memberExpression)
            {
                #region

                if (memberExpression.Expression is ParameterExpression)
                {
                    var custAttr = memberExpression.Member.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                    var attr = (DBOrmColumnAttribute)custAttr[0];

                    if (parentExpression is UnaryExpression unaryExpression)
                    {
                        if (unaryExpression.NodeType == ExpressionType.Negate)
                        {
                            sql.Concat('-', GetFullName(attr.ColumnName));
                        }
                        else if (unaryExpression.NodeType == ExpressionType.Not)
                        {
                            sql.Concat(GetFullName(attr.ColumnName), '=', GetParameter(false, cQuery));
                        }
                        else
                        {
                            sql.Concat(GetFullName(attr.ColumnName));
                        }
                    }
                    else
                    {
                        if (memberExpression.Type == typeof(bool) &&
                            (parentExpression == null ||
                            parentExpression is BinaryExpression binExpression &&
                            binExpression.NodeType != ExpressionType.Equal &&
                            binExpression.NodeType != ExpressionType.NotEqual))
                        {
                            sql.Concat(GetFullName(attr.ColumnName), '=', GetParameter(true, cQuery));
                        }
                        else
                        {
                            sql.Concat(GetFullName(attr.ColumnName));
                        }
                    }
                }
                else if (memberExpression.Member is PropertyInfo propertyInfo)
                {
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
                        sql.Concat(GetParameter(value, cQuery));
                    }
                }
                else if (memberExpression.Member is FieldInfo fieldInfo)
                {
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
                            sql.Concat(GetSubQuery(subQuery, cQuery));
                        }
                        else
                        {
                            sql.Concat(GetParameter(value, cQuery));
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
                        sql.Concat(GetParameter(value, cQuery));
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
                    sql.Concat(GetSqlFromExpression(unaryExpression.Operand, cQuery, expression));
                }

                #endregion
            }
            else if (expression is ParameterExpression parameterExpression)
            {
                #region

                var attrList = parameterExpression.Type.GetCustomAttributes(typeof(DBOrmColumnAttribute), false);
                if (attrList.Length > 0)
                {
                    var attr = (DBOrmColumnAttribute)attrList[0];
                    sql.Concat(GetFullName(attr.ColumnName));
                }
                else
                {
                    attrList = parameterExpression.Type.GetCustomAttributes(typeof(DBOrmTableAttribute), false);
                    if (attrList.Length > 0)
                    {
                        var attr = (DBOrmTableAttribute)attrList[0];
                        sql.Concat(GetShortName(attr.TableName), ".*");
                    }
                    else
                    {
                        throw DBInternal.UnsupportedCommandContextException();
                    }
                }

                #endregion
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                #region

                var method = methodCallExpression.Method;
                if (method.DeclaringType == typeof(DBFunction))
                {
                    sql.Concat(ParseExpressionFunction(methodCallExpression, cQuery, parentExpression));
                }
                else
                {
                    object obj = methodCallExpression.Object;
                    if (obj != null)
                    {
                        obj = GetValueFromExpression(methodCallExpression.Object, expression);
                    }

                    var arguments = new object[methodCallExpression.Arguments.Count];
                    for (var i = 0; i < arguments.Length; i++)
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
                        sql.Concat(GetParameter(value, cQuery));
                    }
                }

                #endregion
            }
            else if (expression is NewArrayExpression newArrayExpression)
            {
                #region

                if (parseValue)
                {
                    var array = (Array)Activator.CreateInstance(newArrayExpression.Type, newArrayExpression.Expressions.Count);
                    for (var i = 0; i < array.Length; i++)
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

                #endregion
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

            var notBlock = (parentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not) ?
               "NOT " : string.Empty;

            var argumentsCount = expression.Arguments.Count;

            // для сокращения объёма кода
            string getArgument(int f_index) => GetSqlFromExpression(expression.Arguments[f_index], cQuery, expression);
            object getValueArgument(int f_index) => GetValueFromExpression(expression.Arguments[f_index], expression);
            IList<Expression> getParamsArgument(int f_index) => ((NewArrayExpression)expression.Arguments[f_index]).Expressions;

            switch (expression.Method.Name)
            {
                case nameof(DBFunction.As):
                    sql.Concat(getArgument(0), " AS ", OpenBlock, getValueArgument(1), CloseBlock);
                    break;

                case nameof(DBFunction.Descending):
                    sql.Concat(getArgument(0), " DESC");
                    break;

                case nameof(DBFunction.Distinct):
                    sql.Concat("DISTINCT ", getArgument(0));
                    break;

                case nameof(DBFunction.Alias):
                    sql.Concat(OpenBlock, getValueArgument(0), CloseBlock);
                    break;

                #region Предикаты сравнения

                case nameof(DBFunction.Between):
                    sql.Concat(getArgument(0), " ", notBlock, "BETWEEN ", getArgument(1), " AND ", getArgument(2));
                    break;

                case nameof(DBFunction.Like):
                    sql.Concat(getArgument(0), " ", notBlock, "LIKE ", getArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = getArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(" ESCAPE ", arg);
                        }
                    }
                    break;

                case nameof(DBFunction.StartingWith):
                    sql.Concat(getArgument(0), " ", notBlock, "STARTING WITH ", getArgument(1));
                    break;

                case nameof(DBFunction.Containing):
                    sql.Concat(getArgument(0), " ", notBlock, "CONTAINING ", getArgument(1));
                    break;

                case nameof(DBFunction.SimilarTo):
                    sql.Concat(getArgument(0), " ", notBlock, "SIMILAR TO ", getArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = getArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(" ESCAPE ", arg);
                        }
                    }
                    break;

                #endregion

                #region Агрегатные функции

                case nameof(DBFunction.Avg):
                    sql.Concat("AVG(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Count):
                    if (argumentsCount > 0)
                    {
                        sql.Concat("COUNT(", getArgument(0), ")");
                    }
                    else
                    {
                        sql.Concat("COUNT(*)");
                    }
                    break;

                case nameof(DBFunction.List):
                    sql.Concat("LIST(", getArgument(0));
                    if (argumentsCount > 1)
                    {
                        sql.Concat(",", getArgument(1));
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.Max):
                    sql.Concat("MAX(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Min):
                    sql.Concat("MIN(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Sum):
                    sql.Concat("SUM(", getArgument(0), ")");
                    break;

                #endregion

                #region Функции для работы со строками

                case nameof(DBFunction.CharLength):
                    sql.Concat("CHAR_LENGTH(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Hash):
                    sql.Concat("HASH(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Left):
                    sql.Concat("LEFT(", getArgument(0), ",", getArgument(1), ")");
                    break;

                case nameof(DBFunction.Lower):
                    sql.Concat("LOWER(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.LPad):
                    sql.Concat("LPAD(", getArgument(0), ",", getArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = getArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(",", arg);
                        }
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.Overlay):
                    sql.Concat("OVERLAY(", getArgument(0), " PLACING ", getArgument(1), " FROM ", getArgument(2));
                    if (argumentsCount > 3)
                    {
                        var arg = getArgument(3);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(" FOR ", arg);
                        }
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.Replace):
                    sql.Concat("REPLACE(", getArgument(0), ",", getArgument(1), ",", getArgument(2), ")");
                    break;

                case nameof(DBFunction.Reverse):
                    sql.Concat("REVERSE(", getArgument(0), ")");
                    break;

                case nameof(DBFunction.Right):
                    sql.Concat("RIGHT(", getArgument(0), ",", getArgument(1), ")");
                    break;

                case nameof(DBFunction.RPad):
                    sql.Concat("RPAD(", getArgument(0), ",", getArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = getArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(",", arg);
                        }
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.SubString):
                    sql.Concat("SUBSTRING (", getArgument(0), " FROM ", getArgument(1));
                    if (argumentsCount > 2)
                    {
                        var arg = getArgument(2);
                        if (!Format.IsEmpty(arg))
                        {
                            sql.Concat(" FOR ", arg);
                        }
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.Upper):
                    sql.Concat("UPPER(", getArgument(0), ")");
                    break;

                #endregion

                #region Предикаты существования

                case nameof(DBFunction.Exists):
                    sql.Concat(notBlock, "EXISTS", getArgument(0));
                    break;

                case nameof(DBFunction.In):
                    var value = getValueArgument(1);
                    if (value is DBQueryBase subQuery)
                    {
                        sql.Concat(getArgument(0), " ", notBlock, "IN", GetSubQuery(subQuery, cQuery));
                    }
                    else if (value is object[] array)
                    {
                        sql.Concat(getArgument(0), " ", notBlock, "IN(");
                        for (var i = 0; i < array.Length; i++)
                        {
                            if (i > 0)
                            {
                                sql.Concat(',');
                            }
                            sql.Concat(GetParameter(array[i], cQuery));
                        }
                        sql.Concat(")");
                    }
                    break;

                case nameof(DBFunction.Singular):
                    sql.Concat(notBlock, "SINGULAR", getArgument(0));
                    break;

                #endregion

                #region Количественные предикаты подзапросов

                case nameof(DBFunction.All):
                    sql.Concat(notBlock, "ALL", getArgument(0));
                    break;

                case nameof(DBFunction.Any):
                    sql.Concat(notBlock, "ANY", getArgument(0));
                    break;

                case nameof(DBFunction.Some):
                    sql.Concat(notBlock, "SOME", getArgument(0));
                    break;

                #endregion

                #region Условные функции

                case nameof(DBFunction.Coalesce):
                    sql.Concat("COALESCE(", getArgument(0), ',', getArgument(1));
                    var expressionArray = getParamsArgument(2);
                    for (var i = 0; i < expressionArray.Count; i++)
                    {
                        sql.Concat(',');
                        sql.Concat(GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.Decode):
                    sql.Concat("DECODE(", getArgument(0));
                    expressionArray = getParamsArgument(1);
                    for (var i = 0; i < expressionArray.Count; i++)
                    {
                        sql.Concat(',');
                        sql.Concat(GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.MaxValue):
                    sql.Concat("MAXVALUE(", getArgument(0));
                    expressionArray = getParamsArgument(1);
                    for (var i = 0; i < expressionArray.Count; i++)
                    {
                        sql.Concat(',');
                        sql.Concat(GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.MinValue):
                    sql.Concat("MINVALUE(", getArgument(0));
                    expressionArray = getParamsArgument(1);
                    for (var i = 0; i < expressionArray.Count; i++)
                    {
                        sql.Concat(',');
                        sql.Concat(GetSqlFromExpression(expressionArray[i], cQuery, expression));
                    }
                    sql.Concat(")");
                    break;

                case nameof(DBFunction.NullIf):
                    sql.Concat("NULLIF(", getArgument(0), ',', getArgument(0), ')');
                    break;

                    #endregion
            }

            return sql.ToString();
        }

        #endregion
    }
}