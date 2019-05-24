using MyLibrary.DataBase.Orm;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    public class DBQuery
    {
        private DBQuery()
        {
            Structure = new List<object[]>();
            QueryType = DBQueryTypeEnum.Select;
        }
        internal DBQuery(DBTable table) : this()
        {
            if (table == null)
            {
                throw DBInternal.ArgumentNullException(nameof(table));
            }
            if (table.Name == null)
            {
                throw DBInternal.ProcessViewException();
            }

            Table = table;
        }
        public DBQuery(string sql, params object[] @params) : this()
        {
            if (sql == null)
            {
                throw DBInternal.ArgumentNullException(nameof(sql));
            }

            QueryType = DBQueryTypeEnum.Sql;
            IsView = true;
            Structure.Add(new object[] { "Sql", sql, @params });
        }

        public DBQueryTypeEnum QueryType { get; private set; }
        public DBTable Table { get; private set; }
        public bool IsView { get; private set; }
        internal List<object[]> Structure { get; private set; }

        #region Построители SQL

        public DBQuery Insert()
        {
            QueryType = DBQueryTypeEnum.Insert;
            return this;
        }
        public DBQuery Update()
        {
            QueryType = DBQueryTypeEnum.Update;
            return this;
        }
        public DBQuery UpdateOrInsert()
        {
            QueryType = DBQueryTypeEnum.UpdateOrInsert;
            return this;
        }
        public DBQuery Delete()
        {
            QueryType = DBQueryTypeEnum.Delete;
            return this;
        }

        public DBQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Insert && QueryType != DBQueryTypeEnum.Update && QueryType != DBQueryTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Set", columnName, value });
            return this;
        }
        public DBQuery Matching(params string[] columns)
        {
            if (QueryType != DBQueryTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Matching", columns });
            return this;
        }
        public DBQuery Returning(params string[] columns)
        {
            if (QueryType == DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Returning", columns });
            return this;
        }

        public DBQuery Select(params string[] columns)
        {
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "Select", columns });
            return this;
        }
        public DBQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectAs", alias, columnName });
            return this;
        }
        public DBQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectSum", columns });
            return this;
        }
        public DBQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectSumAs", columns });
            return this;
        }
        public DBQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMax", columns });
            return this;
        }
        public DBQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMaxAs", columns });
            return this;
        }
        public DBQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMin", columns });
            return this;
        }
        public DBQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectMinAs", columns });
            return this;
        }
        public DBQuery SelectCount(params string[] columns)
        {
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "SelectCount", columns });
            return this;
        }

        public DBQuery Distinct()
        {
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Distinct" });
            return this;
        }
        public DBQuery First(int count)
        {
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "First", count });
            return this;
        }
        public DBQuery First()
        {
            First(1);
            return this;
        }
        public DBQuery Skip(int count)
        {
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "Skip", count });
            return this;
        }

        public DBQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "InnerJoin", joinColumnName, columnName });
            return this;
        }
        public DBQuery LeftOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "LeftOuterJoin", joinColumnName, columnName });
            return this;
        }
        public DBQuery RightOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "RightOuterJoin", joinColumnName, columnName });
            return this;
        }
        public DBQuery FullOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "FullOuterJoin", joinColumnName, columnName });
            return this;
        }

        public DBQuery InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "InnerJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBQuery LeftOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "LeftOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBQuery RightOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "RightOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }
        public DBQuery FullOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "FullOuterJoinAs", alias, joinColumnName, columnName });
            return this;
        }

        public DBQuery Where(string column, object value)
        {
            Where(column, "=", value);
            return this;
        }
        public DBQuery Where(string column1, object value1, string column2, object value2)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            return this;
        }
        public DBQuery Where(string column1, object value1, string column2, object value2, string column3, object value3)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            Where(column3, "=", value3);
            return this;
        }
        public DBQuery Where(string columnName, string equalOperator, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(equalOperator)) throw DBInternal.ArgumentNullException(nameof(equalOperator));

            Structure.Add(new object[] { "Where", columnName, equalOperator, value });
            return this;
        }
        public DBQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));

            Structure.Add(new object[] { "WhereBetween", columnName, value1, value2 });
            return this;
        }
        public DBQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { "WhereUpper", columnName, value });
            return this;
        }
        public DBQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { "WhereContaining", columnName, value });
            return this;
        }
        public DBQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { "WhereContainingUpper", columnName, value });
            return this;
        }
        public DBQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { "WhereLike", columnName, value });
            return this;
        }
        public DBQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { "WhereLikeUpper", columnName, value });
            return this;
        }
        public DBQuery WhereIn(string columnName, DBQuery query)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            Structure.Add(new object[] { "WhereIn_command", columnName, query });
            return this;
        }
        public DBQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(nameof(values));

            Structure.Add(new object[] { "WhereIn_values", columnName, values });
            return this;
        }

        public DBQuery Or()
        {
            Structure.Add(new object[] { "Or" });
            return this;
        }
        public DBQuery Not()
        {
            Structure.Add(new object[] { "Not" });
            return this;
        }
        public DBQuery BlockOpen()
        {
            Structure.Add(new object[] { "(" });
            return this;
        }
        public DBQuery BlockClose()
        {
            Structure.Add(new object[] { ")" });
            return this;
        }

        public DBQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderBy", columns });
            return this;
        }
        public DBQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByDesc", columns });
            return this;
        }
        public DBQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByUpper", columns });
            return this;
        }
        public DBQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { "OrderByUpperDesc", columns });
            return this;
        }

        public DBQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryType != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { "GroupBy", columns });
            return this;
        }

        #endregion
        #region Построители дерева выражений

        public DBQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            Structure.Add(new object[] { "Select_expression", expression.Body });
            return this;
        }
        public DBQuery Select<T>(Expression<Func<T, object>> expression) where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(new object[] { "Select_expression", expression.Body });
            return this;
        }
        public DBQuery Select<T>(Expression<Func<T, object[]>> expression) where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(new object[] { "Select_expression", expression.Body });
            return this;
        }

        public DBQuery Where<T>(Expression<Func<T, bool>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", expression.Body });
            return this;
        }
        public DBQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", expression.Body });
            return this;
        }
        public DBQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", expression.Body });
            return this;
        }
        public DBQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            Structure.Add(new object[] { "Where_expression", expression.Body });
            return this;
        }

        #endregion
    }

    public class DBQuery<T> : DBQuery where T : DBOrmTableBase
    {
        public DBQuery(DBTable table) : base(table)
        {
        }

        public new DBQuery<T> Select(Expression<Func<object>> expression)
        {
            base.Select(expression);
            return this;
        }
        public DBQuery<T> Select(Expression<Func<T, object>> expression)
        {
            base.Select(expression);
            return this;
        }
        public DBQuery<T> Select(Expression<Func<T, object[]>> expression)
        {
            base.Select(expression);
            return this;
        }

        public DBQuery<T> Where(Expression<Func<T, bool>> expression)
        {
            base.Where(expression);
            return this;
        }
        public DBQuery<T> Where<T2>(Expression<Func<T, T2, bool>> expression)
            where T2 : DBOrmTableBase
        {
            base.Where(expression);
            return this;
        }
        public DBQuery<T> Where<T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            base.Where(expression);
            return this;
        }
        public DBQuery<T> Where<T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            base.Where(expression);
            return this;
        }
    }
}
