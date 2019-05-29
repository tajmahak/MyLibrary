using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс структурированного запроса базы данных.
    /// </summary>
    public abstract class DBQueryBase
    {
        protected DBQueryBase(DBTable table)
        {
            Structure = new List<DBQueryStructureBlock>();
            Type = DBQueryType.Select;

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

        public DBQueryType Type { get; protected set; }
        public bool IsView { get; protected set; }
        public DBTable Table { get; private set; }
        protected internal List<DBQueryStructureBlock> Structure { get; private set; }

        protected internal void AddItem(DBQueryStructureType type, params object[] args)
        {
            Structure.Add(new DBQueryStructureBlock()
            {
                Type = type,
                Args = args,
            });
        }
    }

    /// <summary>
    /// Базовый класс с набором функций, представляющих запросы к базе данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public abstract class DBQueryBase<TQuery> : DBQueryBase
    {
        public DBQueryBase(DBTable table) : base(table)
        {
        }

        #region Строковые методы

        public TQuery Insert()
        {
            Type = DBQueryType.Insert;
            return This;
        }
        public TQuery Update()
        {
            Type = DBQueryType.Update;
            return This;
        }
        public TQuery UpdateOrInsert()
        {
            Type = DBQueryType.UpdateOrInsert;
            return This;
        }
        public TQuery Delete()
        {
            Type = DBQueryType.Delete;
            return This;
        }

        public TQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Insert && Type != DBQueryType.Update && Type != DBQueryType.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.Set, columnName, value);
            return This;
        }
        public TQuery Matching(params string[] columns)
        {
            if (Type != DBQueryType.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.Matching, columns);
            return This;
        }
        public TQuery Returning(params string[] columns)
        {
            if (Type == DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.Returning, columns);
            return This;
        }

        public TQuery Select(params string[] columns)
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.Select, columns);
            return This;
        }
        public TQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectAs, alias, columnName);
            return This;
        }
        public TQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectSum, columns);
            return This;
        }
        public TQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectSumAs, columns);
            return This;
        }
        public TQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectMax, columns);
            return This;
        }
        public TQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectMaxAs, columns);
            return This;
        }
        public TQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectMin, columns);
            return This;
        }
        public TQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectMinAs, columns);
            return This;
        }
        public TQuery SelectCount(params string[] columns)
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.SelectCount, columns);
            return This;
        }

        public TQuery Distinct()
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.Distinct);
            return This;
        }
        public TQuery First(int count)
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.First, count);
            return This;
        }
        public TQuery First()
        {
            First(1);
            return This;
        }
        public TQuery Skip(int count)
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.Skip, count);
            return This;
        }
        public TQuery Union(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureType.UnionAll, query);
            return This;
        }
        public TQuery UnionDistinct(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureType.UnionDistinct, query);
            return This;
        }

        public TQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.InnerJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.LeftOuterJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery RightOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.RightOuterJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery FullOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.FullOuterJoin, joinColumnName, columnName);
            return This;
        }

        public TQuery InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.InnerJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.LeftOuterJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery RightOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.RightOuterJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery FullOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.FullOuterJoinAs, alias, joinColumnName, columnName);
            return This;
        }

        public TQuery Where(string column, object value)
        {
            Where(column, "=", value);
            return This;
        }
        public TQuery Where(string column1, object value1, string column2, object value2)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            return This;
        }
        public TQuery Where(string column1, object value1, string column2, object value2, string column3, object value3)
        {
            Where(column1, "=", value1);
            Where(column2, "=", value2);
            Where(column3, "=", value3);
            return This;
        }
        public TQuery Where(string columnName, string equalOperator, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(equalOperator)) throw DBInternal.ArgumentNullException(nameof(equalOperator));

            AddItem(DBQueryStructureType.Where, columnName, equalOperator, value);
            return This;
        }
        public TQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));

            AddItem(DBQueryStructureType.WhereBetween, columnName, value1, value2);
            return This;
        }
        public TQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureType.WhereUpper, columnName, value);
            return This;
        }
        public TQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureType.WhereContaining, columnName, value);
            return This;
        }
        public TQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureType.WhereContainingUpper, columnName, value);
            return This;
        }
        public TQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureType.WhereLike, columnName, value);
            return This;
        }
        public TQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureType.WhereLikeUpper, columnName, value);
            return This;
        }
        public TQuery WhereIn(string columnName, DBQueryBase query)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureType.WhereIn_command, columnName, query);
            return This;
        }
        public TQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(nameof(values));

            AddItem(DBQueryStructureType.WhereIn_values, columnName, values);
            return This;
        }

        public TQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.OrderBy, columns);
            return This;
        }
        public TQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.OrderByDesc, columns);
            return This;
        }
        public TQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.OrderByUpper, columns);
            return This;
        }
        public TQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureType.OrderByUpperDesc, columns);
            return This;
        }

        public TQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.GroupBy, columns);
            return This;
        }

        #endregion
        #region Типизированные методы, дерево выражений

        public TQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            AddItem(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddItem(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddItem(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }

        public TQuery InnerJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.InnerJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery LeftOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.LeftOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery RightOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.RightOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery FullOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.FullOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }

        public TQuery InnerJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.InnerJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery LeftOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.LeftOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery RightOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.RightOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery FullOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureType.FullOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }

        public TQuery Where<T>(Expression<Func<T, bool>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }

        public TQuery OrderBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }

        public TQuery GroupBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }

        #endregion

        private TQuery This => (TQuery)((object)this);
    }

    /// <summary>
    /// Представляет структирированный запрос базы данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public class DBQuery : DBQueryBase<DBQuery>
    {
        public DBQuery(DBTable table) : base(table)
        {
        }
    }

    /// <summary>
    /// Представляет типизированный запрос базы данных для таблиц <see cref="DBOrmTableBase"/>.
    /// </summary>
    public class DBQuery<T> : DBQueryBase<DBQuery<T>> where T : DBOrmTableBase
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

        public DBQuery<T> InnerJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            InnerJoinAs<T, T2>(alias);
            return this;
        }
        public DBQuery<T> LeftOuterJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            LeftOuterJoinAs<T, T2>(alias);
            return this;
        }
        public DBQuery<T> RightOuterJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            RightOuterJoinAs<T, T2>(alias);
            return this;
        }
        public DBQuery<T> FullOuterJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            FullOuterJoinAs<T, T2>(alias);
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
