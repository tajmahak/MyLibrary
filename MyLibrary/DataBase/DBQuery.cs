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
            QueryCommandType = DBQueryCommandTypeEnum.Select;
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

            QueryCommandType = DBQueryCommandTypeEnum.Sql;
            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.Sql, sql, @params });
        }

        public DBQueryCommandTypeEnum QueryCommandType { get; private set; }
        public DBTable Table { get; private set; }
        public bool IsView { get; protected set; }
        internal List<object[]> Structure { get; private set; }

        #region Построители SQL

        public DBQuery Insert()
        {
            QueryCommandType = DBQueryCommandTypeEnum.Insert;
            return this;
        }
        public DBQuery Update()
        {
            QueryCommandType = DBQueryCommandTypeEnum.Update;
            return this;
        }
        public DBQuery UpdateOrInsert()
        {
            QueryCommandType = DBQueryCommandTypeEnum.UpdateOrInsert;
            return this;
        }
        public DBQuery Delete()
        {
            QueryCommandType = DBQueryCommandTypeEnum.Delete;
            return this;
        }

        public DBQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Insert && QueryCommandType != DBQueryCommandTypeEnum.Update && QueryCommandType != DBQueryCommandTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.Set, columnName, value });
            return this;
        }
        public DBQuery Matching(params string[] columns)
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.Matching, columns });
            return this;
        }
        public DBQuery Returning(params string[] columns)
        {
            if (QueryCommandType == DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.Returning, columns });
            return this;
        }

        public DBQuery Select(params string[] columns)
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.Select, columns });
            return this;
        }
        public DBQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectAs, alias, columnName });
            return this;
        }
        public DBQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectSum, columns });
            return this;
        }
        public DBQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectSumAs, columns });
            return this;
        }
        public DBQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectMax, columns });
            return this;
        }
        public DBQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectMaxAs, columns });
            return this;
        }
        public DBQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectMin, columns });
            return this;
        }
        public DBQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectMinAs, columns });
            return this;
        }
        public DBQuery SelectCount(params string[] columns)
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.SelectCount, columns });
            return this;
        }

        public DBQuery Distinct()
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.Distinct });
            return this;
        }
        public DBQuery First(int count)
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.First, count });
            return this;
        }
        public DBQuery First()
        {
            First(1);
            return this;
        }
        public DBQuery Skip(int count)
        {
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.Skip, count });
            return this;
        }

        public DBQuery Union(DBQuery query, DBFunction.OptionEnum? operation = null)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            Structure.Add(new object[] { DBQueryTypeEnum.Union, query, operation });
            return this;
        }

        public DBQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.InnerJoin, joinColumnName, columnName });
            return this;
        }
        public DBQuery LeftOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.LeftOuterJoin, joinColumnName, columnName });
            return this;
        }
        public DBQuery RightOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.RightOuterJoin, joinColumnName, columnName });
            return this;
        }
        public DBQuery FullOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.FullOuterJoin, joinColumnName, columnName });
            return this;
        }

        public DBQuery InnerJoin<T1, T2>()
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.InnerJoin_type, typeof(T1), typeof(T2) });
            return this;
        }
        public DBQuery LeftOuterJoin<T1, T2>()
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.LeftOuterJoin_type, typeof(T1), typeof(T2) });
            return this;
        }
        public DBQuery RightOuterJoin<T1, T2>()
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.RightOuterJoin_type, typeof(T1), typeof(T2) });
            return this;
        }
        public DBQuery FullOuterJoin<T1, T2>()
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.FullOuterJoin_type, typeof(T1), typeof(T2) });
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

            Structure.Add(new object[] { DBQueryTypeEnum.Where, columnName, equalOperator, value });
            return this;
        }
        public DBQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereBetween, columnName, value1, value2 });
            return this;
        }
        public DBQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereUpper, columnName, value });
            return this;
        }
        public DBQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereContaining, columnName, value });
            return this;
        }
        public DBQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereContainingUpper, columnName, value });
            return this;
        }
        public DBQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereLike, columnName, value });
            return this;
        }
        public DBQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereLikeUpper, columnName, value });
            return this;
        }
        public DBQuery WhereIn(string columnName, DBQuery query)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereIn_command, columnName, query });
            return this;
        }
        public DBQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(nameof(values));

            Structure.Add(new object[] { DBQueryTypeEnum.WhereIn_values, columnName, values });
            return this;
        }

        public DBQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.OrderBy, columns });
            return this;
        }
        public DBQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.OrderByDesc, columns });
            return this;
        }
        public DBQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.OrderByUpper, columns });
            return this;
        }
        public DBQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            Structure.Add(new object[] { DBQueryTypeEnum.OrderByUpperDesc, columns });
            return this;
        }

        public DBQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (QueryCommandType != DBQueryCommandTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.GroupBy, columns });
            return this;
        }

        #endregion
        #region Построители дерева выражений

        public DBQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.Select_expression, expression.Body });
            return this;
        }
        public DBQuery Select<T>(Expression<Func<T, object>> expression) where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.Select_expression, expression.Body });
            return this;
        }
        public DBQuery Select<T>(Expression<Func<T, object[]>> expression) where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(new object[] { DBQueryTypeEnum.Select_expression, expression.Body });
            return this;
        }

        public DBQuery Where<T>(Expression<Func<T, bool>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.Where_expression, expression.Body });
            return this;
        }
        public DBQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.Where_expression, expression.Body });
            return this;
        }
        public DBQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.Where_expression, expression.Body });
            return this;
        }
        public DBQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.Where_expression, expression.Body });
            return this;
        }

        public DBQuery OrderBy<T>(Expression<Func<T, object>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.OrderBy_expression, expression.Body });
            return this;
        }
        public DBQuery OrderBy<T>(Expression<Func<T, object[]>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.OrderBy_expression, expression.Body });
            return this;
        }
        public DBQuery OrderBy<T1, T2>(Expression<Func<T1, T2, object[]>> expression)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.OrderBy_expression, expression.Body });
            return this;
        }
        public DBQuery OrderBy<T1, T2, T3>(Expression<Func<T1, T2, T3, object[]>> expression)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.OrderBy_expression, expression.Body });
            return this;
        }

        public DBQuery GroupBy<T>(Expression<Func<T, object>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.GroupBy_expression, expression.Body });
            return this;
        }
        public DBQuery GroupBy<T>(Expression<Func<T, object[]>> expression) where T : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.GroupBy_expression, expression.Body });
            return this;
        }
        public DBQuery GroupBy<T1, T2>(Expression<Func<T1, T2, object[]>> expression)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.GroupBy_expression, expression.Body });
            return this;
        }
        public DBQuery GroupBy<T1, T2, T3>(Expression<Func<T1, T2, T3, object[]>> expression)
            where T1 : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(new object[] { DBQueryTypeEnum.GroupBy_expression, expression.Body });
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

        public DBQuery<T> InnerJoin<T2>() where T2 : DBOrmTableBase
        {
            InnerJoin<T, T2>();
            return this;
        }
        public DBQuery<T> LeftOuterJoin<T2>() where T2 : DBOrmTableBase
        {
            LeftOuterJoin<T, T2>();
            return this;
        }
        public DBQuery<T> RightOuterJoin<T2>() where T2 : DBOrmTableBase
        {
            RightOuterJoin<T, T2>();
            return this;
        }
        public DBQuery<T> FullOuterJoin<T2>() where T2 : DBOrmTableBase
        {
            FullOuterJoin<T, T2>();
            return this;
        }

        public DBQuery<T> OrderBy(Expression<Func<T, object>> expression)
        {
            OrderBy<T>(expression);
            return this;
        }
        public DBQuery<T> OrderBy(Expression<Func<T, object[]>> expression)
        {
            OrderBy<T>(expression);
            return this;
        }
        public DBQuery<T> OrderBy<T2>(Expression<Func<T, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            OrderBy<T, T2>(expression);
            return this;
        }
        public DBQuery<T> OrderBy<T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            OrderBy<T, T2, T3>(expression);
            return this;
        }

        public DBQuery<T> GroupBy(Expression<Func<T, object>> expression)
        {
            GroupBy<T>(expression);
            return this;
        }
        public DBQuery<T> GroupBy(Expression<Func<T, object[]>> expression)
        {
            GroupBy<T>(expression);
            return this;
        }
        public DBQuery<T> GroupBy<T2>(Expression<Func<T, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            GroupBy<T, T2>(expression);
            return this;
        }
        public DBQuery<T> GroupBy<T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            GroupBy<T, T2, T3>(expression);
            return this;
        }
    }
}
