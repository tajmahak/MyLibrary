using MyLibrary.DataBase.Orm;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    public abstract class DBQueryBase
    {
        protected DBQueryBase(DBTable table)
        {
            Structure = new List<DBQueryStructureBlock>();
            Type = DBQueryTypeEnum.Select;

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

        public DBQueryTypeEnum Type { get; protected set; }
        public bool IsView { get; protected set; }
        public DBTable Table { get; private set; }
        protected internal List<DBQueryStructureBlock> Structure { get; private set; }

        protected internal void AddItem(DBQueryStructureTypeEnum type, params object[] args)
        {
            Structure.Add(new DBQueryStructureBlock()
            {
                Type = type,
                Args = args,
            });
        }
    }

    public abstract class DBQueryBase<TQuery> : DBQueryBase
    {
        public DBQueryBase(DBTable table) : base(table)
        {
        }

        #region Строковые методы

        public TQuery Insert()
        {
            Type = DBQueryTypeEnum.Insert;
            return This;
        }
        public TQuery Update()
        {
            Type = DBQueryTypeEnum.Update;
            return This;
        }
        public TQuery UpdateOrInsert()
        {
            Type = DBQueryTypeEnum.UpdateOrInsert;
            return This;
        }
        public TQuery Delete()
        {
            Type = DBQueryTypeEnum.Delete;
            return This;
        }

        public TQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Insert && Type != DBQueryTypeEnum.Update && Type != DBQueryTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.Set, columnName, value);
            return This;
        }
        public TQuery Matching(params string[] columns)
        {
            if (Type != DBQueryTypeEnum.UpdateOrInsert) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.Matching, columns);
            return This;
        }
        public TQuery Returning(params string[] columns)
        {
            if (Type == DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.Returning, columns);
            return This;
        }

        public TQuery Select(params string[] columns)
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.Select, columns);
            return This;
        }
        public TQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectAs, alias, columnName);
            return This;
        }
        public TQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectSum, columns);
            return This;
        }
        public TQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectSumAs, columns);
            return This;
        }
        public TQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectMax, columns);
            return This;
        }
        public TQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectMaxAs, columns);
            return This;
        }
        public TQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectMin, columns);
            return This;
        }
        public TQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectMinAs, columns);
            return This;
        }
        public TQuery SelectCount(params string[] columns)
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.SelectCount, columns);
            return This;
        }

        public TQuery Distinct()
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.Distinct);
            return This;
        }
        public TQuery First(int count)
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.First, count);
            return This;
        }
        public TQuery First()
        {
            First(1);
            return This;
        }
        public TQuery Skip(int count)
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.Skip, count);
            return This;
        }
        public TQuery Union(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureTypeEnum.UnionAll, query);
            return This;
        }
        public TQuery UnionDistinct(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureTypeEnum.UnionDistinct, query);
            return This;
        }

        public TQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.InnerJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.LeftOuterJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery RightOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.RightOuterJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery FullOuterJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.FullOuterJoin, joinColumnName, columnName);
            return This;
        }

        public TQuery InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.InnerJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.LeftOuterJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery RightOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.RightOuterJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery FullOuterJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.FullOuterJoinAs, alias, joinColumnName, columnName);
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

            AddItem(DBQueryStructureTypeEnum.Where, columnName, equalOperator, value);
            return This;
        }
        public TQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));

            AddItem(DBQueryStructureTypeEnum.WhereBetween, columnName, value1, value2);
            return This;
        }
        public TQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureTypeEnum.WhereUpper, columnName, value);
            return This;
        }
        public TQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureTypeEnum.WhereContaining, columnName, value);
            return This;
        }
        public TQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureTypeEnum.WhereContainingUpper, columnName, value);
            return This;
        }
        public TQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureTypeEnum.WhereLike, columnName, value);
            return This;
        }
        public TQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddItem(DBQueryStructureTypeEnum.WhereLikeUpper, columnName, value);
            return This;
        }
        public TQuery WhereIn(string columnName, DBQueryBase query)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddItem(DBQueryStructureTypeEnum.WhereIn_command, columnName, query);
            return This;
        }
        public TQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(nameof(values));

            AddItem(DBQueryStructureTypeEnum.WhereIn_values, columnName, values);
            return This;
        }

        public TQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.OrderBy, columns);
            return This;
        }
        public TQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.OrderByDesc, columns);
            return This;
        }
        public TQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.OrderByUpper, columns);
            return This;
        }
        public TQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            AddItem(DBQueryStructureTypeEnum.OrderByUpperDesc, columns);
            return This;
        }

        public TQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.GroupBy, columns);
            return This;
        }

        #endregion
        #region Типизированные методы, дерево выражений

        public TQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            AddItem(DBQueryStructureTypeEnum.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddItem(DBQueryStructureTypeEnum.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddItem(DBQueryStructureTypeEnum.Select_expression, expression.Body);
            return This;
        }

        public TQuery InnerJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.InnerJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery LeftOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.LeftOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery RightOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.RightOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery FullOuterJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.FullOuterJoin_type, typeof(T), typeof(T2));
            return This;
        }

        public TQuery InnerJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.InnerJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery LeftOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.LeftOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery RightOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.RightOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery FullOuterJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (Type != DBQueryTypeEnum.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddItem(DBQueryStructureTypeEnum.FullOuterJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }

        public TQuery Where<T>(Expression<Func<T, bool>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.Where_expression, expression.Body);
            return This;
        }

        public TQuery OrderBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.OrderBy_expression, expression.Body);
            return This;
        }

        public TQuery GroupBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddItem(DBQueryStructureTypeEnum.GroupBy_expression, expression.Body);
            return This;
        }

        #endregion

        private TQuery This
        {
            get => (TQuery)((object)this);
        }
    }

    public class DBQuery : DBQueryBase<DBQuery>
    {
        public DBQuery(DBTable table) : base(table)
        {
        }
    }

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
