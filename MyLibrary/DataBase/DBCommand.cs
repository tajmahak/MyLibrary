using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MyLibrary.DataBase.Orm;

namespace MyLibrary.DataBase
{
    public class DBCommand
    {
        private DBCommand()
        {
            Structure = new List<object[]>();
        }
        internal DBCommand(DBTable table)
            : this()
        {
            if (table == null)
                throw DBInternal.ArgumentNullException(() => table);
            if (table.Name == null)
                throw DBInternal.ProcessViewException();

            Table = table;
            CommandType = DBCommandTypeEnum.Select;
        }
        public DBCommand(string sql, params object[] @params)
            : this()
        {
            if (sql == null)
                throw DBInternal.ArgumentNullException(() => sql);

            CommandType = DBCommandTypeEnum.Sql;
            IsView = true;
            Structure.Add(new object[] { "Sql", sql, @params });
        }

        public DBCommandTypeEnum CommandType { get; private set; }
        public DBTable Table { get; private set; }
        public bool IsView { get; private set; }
        internal List<object[]> Structure { get; private set; }

        #region Построители SQL

        public DBCommand Insert()
        {
            CommandType = DBCommandTypeEnum.Insert;
            return this;
        }
        public DBCommand Update()
        {
            CommandType = DBCommandTypeEnum.Update;
            return this;
        }
        public DBCommand UpdateOrInsert()
        {
            CommandType = DBCommandTypeEnum.UpdateOrInsert;
            return this;
        }
        public DBCommand Delete()
        {
            CommandType = DBCommandTypeEnum.Delete;
            return this;
        }

        public DBCommand Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Insert && CommandType != DBCommandTypeEnum.Update && CommandType != DBCommandTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Set", columnName, value });
            return this;
        }
        public DBCommand Matching(params string[] columns)
        {
            if (CommandType != DBCommandTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Matching", columns });
            return this;
        }
        public DBCommand Returning(params string[] columns)
        {
            if (CommandType == DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Returning", columns });
            return this;
        }

        public DBCommand Select(params string[] columns)
        {
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "Select", columns });
            return this;
        }
        public DBCommand SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(() => alias);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectAs", alias, columnName });
            return this;
        }
        public DBCommand SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectSum", columns });
            return this;
        }
        public DBCommand SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectSumAs", columns });
            return this;
        }
        public DBCommand SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMax", columns });
            return this;
        }
        public DBCommand SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMaxAs", columns });
            return this;
        }
        public DBCommand SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMin", columns });
            return this;
        }
        public DBCommand SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMinAs", columns });
            return this;
        }
        public DBCommand SelectCount(params string[] columns)
        {
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectCount", columns });
            return this;
        }

        public DBCommand Distinct()
        {
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Distinct" });
            return this;
        }
        public DBCommand First(int count)
        {
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "First", count });
            return this;
        }
        public DBCommand First()
        {
            First(1);
            return this;
        }
        public DBCommand Skip(int count)
        {
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Skip", count });
            return this;
        }

        public DBCommand InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "InnerJoin", joinColumnName, columnName });
            return this;
        }
        public DBCommand LeftOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "LeftOuterJoin", joinColumnName, columnName });
            return this;
        }
        public DBCommand RightOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "RightOuterJoin", joinColumnName, columnName });
            return this;
        }
        public DBCommand FullOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "FullOuterJoin", joinColumnName, columnName });
            return this;
        }

        public DBCommand InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(() => alias);
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "InnerJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBCommand LeftOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(() => alias);
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "LeftOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBCommand RightOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(() => alias);
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "RightOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBCommand FullOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(() => alias);
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(() => joinColumnName);
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "FullOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }

        public DBCommand Where(string column, object value)
        {
            Where(column, "=", value);
            return this;
        }
        public DBCommand Where(string column1, object value1, string column2, object value2)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            return this;
        }
        public DBCommand Where(string column1, object value1, string column2, object value2, string column3, object value3)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            Where(column3, "=", value3);
            return this;
        }
        public DBCommand Where(string columnName, string equalOperator, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(equalOperator)) throw DBInternal.ArgumentNullException(() => equalOperator);

            Structure.Add(new object[] { "Where", columnName, equalOperator, value });
            return this;
        }
        public DBCommand WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);

            Structure.Add(new object[] { "WhereBetween", columnName, value1, value2 });
            return this;
        }
        public DBCommand WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(() => value);

            Structure.Add(new object[] { "WhereUpper", columnName, value });
            return this;
        }
        public DBCommand WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(() => value);

            Structure.Add(new object[] { "WhereContaining", columnName, value });
            return this;
        }
        public DBCommand WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(() => value);

            Structure.Add(new object[] { "WhereContainingUpper", columnName, value });
            return this;
        }
        public DBCommand WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(() => value);

            Structure.Add(new object[] { "WhereLike", columnName, value });
            return this;
        }
        public DBCommand WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(() => value);

            Structure.Add(new object[] { "WhereLikeUpper", columnName, value });
            return this;
        }
        public DBCommand WhereIn(string columnName, DBCommand cmd)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (cmd == null) throw DBInternal.ArgumentNullException(() => cmd);

            Structure.Add(new object[] { "WhereIn_command", columnName, cmd });
            return this;
        }
        public DBCommand WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(() => columnName);
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(() => values);

            Structure.Add(new object[] { "WhereIn_values", columnName, values });
            return this;
        }

        public DBCommand Or()
        {
            Structure.Add(new object[] { "Or" });
            return this;
        }
        public DBCommand Not()
        {
            Structure.Add(new object[] { "Not" });
            return this;
        }
        public DBCommand BlockOpen()
        {
            Structure.Add(new object[] { "(" });
            return this;
        }
        public DBCommand BlockClose()
        {
            Structure.Add(new object[] { ")" });
            return this;
        }

        public DBCommand OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderBy", columns });
            return this;
        }
        public DBCommand OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByDesc", columns });
            return this;
        }
        public DBCommand OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByUpper", columns });
            return this;
        }
        public DBCommand OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByUpperDesc", columns });
            return this;
        }

        public DBCommand GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(() => columns);
            if (CommandType != DBCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "GroupBy", columns });
            return this;
        }

        #endregion
        #region Построители дерева выражений

        public DBCommand Where<T>(Expression<Func<T, bool>> query) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", query.Body });
            return this;
        }
        public DBCommand Where<T1, T2>(Expression<Func<T1, T2, bool>> query)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", query.Body });
            return this;
        }
        public DBCommand Where<T1, T2, T3>(Expression<Func<T1, T2, T3, bool>> query)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", query.Body });
            return this;
        }
        public DBCommand Where<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, bool>> query)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", query.Body });
            return this;
        }

        #endregion
    }
}