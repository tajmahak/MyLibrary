using MyLibrary.Collections;
using System;
using System.Collections.Generic;
using System.Data;
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
            StatementType = StatementType.Select;

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

        public StatementType StatementType { get; protected set; }
        public bool IsView { get; protected set; }
        public DBTable Table { get; private set; }
        protected internal ReadOnlyList<DBQueryStructureBlock> Structure { get; private set; }

        protected internal void AddBlock(DBQueryStructureType type, params object[] args)
        {
            Structure.List.Add(new DBQueryStructureBlock()
            {
                Type = type,
                Args = args,
            });
        }
        protected internal List<DBQueryStructureBlock> FindBlocks(Predicate<DBQueryStructureType> predicate)
        {
            return Structure.FindAll(x => predicate(x.Type));
        }
        protected internal List<DBQueryStructureBlock> FindBlocks(DBQueryStructureType type)
        {
            return FindBlocks(x => x == type);
        }
        protected internal List<DBQueryStructureBlock> FindBlocks(Predicate<string> predicate)
        {
            return Structure.FindAll(x => predicate(x.Type.ToString()));
        }
        protected internal DBQueryStructureBlock FindBlock(Predicate<DBQueryStructureType> type)
        {
            return Structure.Find(x => type(x.Type));
        }
        protected internal DBQueryStructureBlock FindBlock(DBQueryStructureType type)
        {
            return FindBlock(x => x == type);
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
        private TQuery This => (TQuery)((object)this);

        public TQuery Insert()
        {
            StatementType = StatementType.Insert;
            return This;
        }
        public TQuery Update()
        {
            StatementType = StatementType.Update;
            return This;
        }
        public TQuery Delete()
        {
            StatementType = StatementType.Delete;
            return This;
        }

        public TQuery UpdateOrInsert(params string[] matchingColumns)
        {
            StatementType = StatementType.Batch;
            AddBlock(DBQueryStructureType.UpdateOrInsert, matchingColumns);
            return This;
        }
        public TQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType == StatementType.Select || StatementType == StatementType.Delete) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Set, columnName, value);
            return This;
        }
        public TQuery Returning(params string[] columns)
        {
            if (StatementType == StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Returning, columns);
            return This;
        }

        public TQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            IsView = true;
            AddBlock(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }

        public TQuery Select(params string[] columns)
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.Select, columns);
            return This;
        }
        public TQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectAs, alias, columnName);
            return This;
        }
        public TQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectSum, columns);
            return This;
        }
        public TQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectSumAs, columns);
            return This;
        }
        public TQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectMax, columns);
            return This;
        }
        public TQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectMaxAs, columns);
            return This;
        }
        public TQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectMin, columns);
            return This;
        }
        public TQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectMinAs, columns);
            return This;
        }
        public TQuery SelectCount(params string[] columns)
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.SelectCount, columns);
            return This;
        }

        public TQuery Distinct()
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Distinct);
            return This;
        }
        public TQuery First()
        {
            Limit(1);
            return This;
        }
        public TQuery Limit(int count)
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Limit, count);
            return This;
        }
        public TQuery Offset(int count)
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Offset, count);
            return This;
        }
        public TQuery Union(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddBlock(DBQueryStructureType.UnionAll, query);
            return This;
        }
        public TQuery UnionDistinct(DBQueryBase query)
        {
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddBlock(DBQueryStructureType.UnionDistinct, query);
            return This;
        }

        public TQuery InnerJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.InnerJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery LeftJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.LeftJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery RightJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.RightJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery FullJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.FullJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.InnerJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.LeftJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery RightJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.RightJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery FullJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.FullJoin, joinColumnName, columnName);
            return This;
        }

        public TQuery InnerJoinAs<T, T2>(string alias)
              where T : DBOrmTableBase
              where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.InnerJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery LeftJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.LeftJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery RightJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.RightJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery FullJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.FullJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.InnerJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.LeftJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery RightJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.RightJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery FullJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias)) throw DBInternal.ArgumentNullException(nameof(alias));
            if (string.IsNullOrEmpty(joinColumnName)) throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.FullJoinAs, alias, joinColumnName, columnName);
            return This;
        }

        public TQuery Where<T>(Expression<Func<T, bool>> expression)
            where T : DBOrmTableBase
        {
            AddBlock(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            AddBlock(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            AddBlock(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            AddBlock(DBQueryStructureType.Where_expression, expression.Body);
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

            AddBlock(DBQueryStructureType.Where, columnName, equalOperator, value);
            return This;
        }
        public TQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));

            AddBlock(DBQueryStructureType.WhereBetween, columnName, value1, value2);
            return This;
        }
        public TQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddBlock(DBQueryStructureType.WhereUpper, columnName, value);
            return This;
        }
        public TQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddBlock(DBQueryStructureType.WhereContaining, columnName, value);
            return This;
        }
        public TQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddBlock(DBQueryStructureType.WhereContainingUpper, columnName, value);
            return This;
        }
        public TQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddBlock(DBQueryStructureType.WhereLike, columnName, value);
            return This;
        }
        public TQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(value)) throw DBInternal.ArgumentNullException(nameof(value));

            AddBlock(DBQueryStructureType.WhereLikeUpper, columnName, value);
            return This;
        }
        public TQuery WhereIn(string columnName, DBQueryBase query)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (query == null) throw DBInternal.ArgumentNullException(nameof(query));

            AddBlock(DBQueryStructureType.WhereIn_command, columnName, query);
            return This;
        }
        public TQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName)) throw DBInternal.ArgumentNullException(nameof(columnName));
            if (values == null || values.Length == 0) throw DBInternal.ArgumentNullException(nameof(values));

            AddBlock(DBQueryStructureType.WhereIn_values, columnName, values);
            return This;
        }

        public TQuery OrderBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderBy, columns);
            return This;
        }
        public TQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderByDesc, columns);
            return This;
        }
        public TQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderByUpper, columns);
            return This;
        }
        public TQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.OrderByUpperDesc, columns);
            return This;
        }

        public TQuery GroupBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0) throw DBInternal.ArgumentNullException(nameof(columns));
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            IsView = true;
            AddBlock(DBQueryStructureType.GroupBy, columns);
            return This;
        }

        public TQuery Having<T>(Expression<Func<T, bool>> expression)
        {
            if (StatementType != StatementType.Select) throw DBInternal.UnsupportedCommandContextException();

            AddBlock(DBQueryStructureType.Having_expression, expression.Body);
            return This;
        }
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
            return base.Select(expression);
        }
        public DBQuery<T> Select(Expression<Func<T, object>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<T> Select(Expression<Func<T, object[]>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<T> Select<T2>(Expression<Func<T, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return base.Select(expression);
        }
        public DBQuery<T> Select<T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return base.Select(expression);
        }
        public DBQuery<T> Select<T2, T3, T4>(Expression<Func<T, T2, T3, T4, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            return base.Select(expression);
        }

        public DBQuery<T> Where(Expression<Func<T, bool>> expression)
        {
            return base.Where(expression);
        }
        public DBQuery<T> Where<T2>(Expression<Func<T, T2, bool>> expression)
            where T2 : DBOrmTableBase
        {
            return base.Where(expression);
        }
        public DBQuery<T> Where<T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return base.Where(expression);
        }
        public DBQuery<T> Where<T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            return base.Where(expression);
        }

        public DBQuery<T> InnerJoin<T2>() where T2 : DBOrmTableBase
        {
            return InnerJoin<T, T2>();
        }
        public DBQuery<T> LeftJoin<T2>() where T2 : DBOrmTableBase
        {
            return LeftJoin<T, T2>();
        }
        public DBQuery<T> RightJoin<T2>() where T2 : DBOrmTableBase
        {
            return RightJoin<T, T2>();
        }
        public DBQuery<T> FullJoin<T2>() where T2 : DBOrmTableBase
        {
            return FullJoin<T, T2>();
        }

        public DBQuery<T> InnerJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return InnerJoinAs<T, T2>(alias);
        }
        public DBQuery<T> LeftJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return LeftJoinAs<T, T2>(alias);
        }
        public DBQuery<T> RightJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return RightJoinAs<T, T2>(alias);
        }
        public DBQuery<T> FullJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return FullJoinAs<T, T2>(alias);
        }

        public DBQuery<T> OrderBy(Expression<Func<T, object>> expression)
        {
            return OrderBy<T>(expression);
        }
        public DBQuery<T> OrderBy(Expression<Func<T, object[]>> expression)
        {
            return OrderBy<T>(expression);
        }
        public DBQuery<T> OrderBy<T2>(Expression<Func<T, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return OrderBy<T, T2>(expression);
        }
        public DBQuery<T> OrderBy<T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return OrderBy<T, T2, T3>(expression);
        }

        public DBQuery<T> GroupBy(Expression<Func<T, object>> expression)
        {
            return GroupBy<T>(expression);
        }
        public DBQuery<T> GroupBy(Expression<Func<T, object[]>> expression)
        {
            return GroupBy<T>(expression);
        }
        public DBQuery<T> GroupBy<T2>(Expression<Func<T, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return GroupBy<T, T2>(expression);
        }
        public DBQuery<T> GroupBy<T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return GroupBy<T, T2, T3>(expression);
        }

        public DBQuery<T> Having(Expression<Func<T, bool>> expression)
        {
            return Having<T>(expression);
        }
    }
}
