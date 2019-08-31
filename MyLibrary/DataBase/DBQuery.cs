using System;
using System.Data;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс структурированного запроса базы данных.
    /// </summary>
    public abstract class DBQueryBase
    {
        public StatementType StatementType { get; protected set; } = StatementType.Select;
        public bool IsView { get; protected set; }
        public DBTable Table { get; private set; }
        public DBContext Context { get; private set; }
        protected internal DBQueryStructureBlockCollection Structure { get; private set; } = new DBQueryStructureBlockCollection();

        // Работа с контекстом БД
        public TTable ReadRow<TTable>() where TTable : DBOrmTableBase
        {
            return Context.GetRowInternal<TTable>(this);
        }
        public DBRow ReadRow()
        {
            return Context.GetRowInternal<DBRow>(this);
        }
        public TTable GetRowOrNew<TTable>() where TTable : DBOrmTableBase
        {
            return Context.GetRowOrNewInternal<TTable>(this);
        }
        public DBRow GetRowOrNew()
        {
            return Context.GetRowOrNewInternal<DBRow>(this);
        }
        public DBReader<TTable> Read<TTable>() where TTable : DBOrmTableBase
        {
            return Context.ReadInternal<TTable>(this);
        }
        public DBReader<DBRow> Read()
        {
            return Context.ReadInternal<DBRow>(this);
        }
        public TType ReadValue<TType>()
        {
            return Context.ReadValueInternal<TType>(this);
        }
        public bool RowExists()
        {
            return Context.RowExistsInternal(this);
        }
        public int Execute()
        {
            return Context.Execute(this);
        }

        protected DBQueryBase(DBTable table, DBContext context)
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
            Context = context;
        }
    }

    /// <summary>
    /// Базовый класс с набором функций, представляющих запросы к базе данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public abstract class DBQueryBase<TQuery> : DBQueryBase
    {
        private TQuery This => (TQuery)((object)this);

        public DBQueryBase(DBTable table, DBContext context) : base(table, context)
        {
        }

        // Работа с командами SQL
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
            if (matchingColumns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(matchingColumns));
            }

            StatementType = StatementType.Batch;
            Structure.Add(DBQueryStructureType.UpdateOrInsert, matchingColumns);
            return This;
        }
        public TQuery Set(string columnName, object value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType == StatementType.Select || StatementType == StatementType.Delete)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Set, columnName, value);
            return This;
        }
        public TQuery Returning(params string[] columns)
        {
            if (StatementType == StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Returning, columns);
            return This;
        }

        public TQuery Select(Expression<Func<object>> expression)
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }
        public TQuery Select<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.Select_expression, expression.Body);
            return This;
        }

        public TQuery Select(params string[] columns)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.Select, columns);
            return This;
        }
        public TQuery SelectAs(string alias, string columnName)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectAs, alias, columnName);
            return This;
        }
        public TQuery SelectSum(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectSum, columns);
            return This;
        }
        public TQuery SelectSumAs(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectSumAs, columns);
            return This;
        }
        public TQuery SelectMax(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectMax, columns);
            return This;
        }
        public TQuery SelectMaxAs(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectMaxAs, columns);
            return This;
        }
        public TQuery SelectMin(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectMin, columns);
            return This;
        }
        public TQuery SelectMinAs(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectMinAs, columns);
            return This;
        }
        public TQuery SelectCount(params string[] columns)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectCount, columns);
            return This;
        }

        public TQuery Distinct()
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Distinct);
            return This;
        }
        public TQuery First()
        {
            Limit(1);
            return This;
        }
        public TQuery Limit(int count)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Limit, count);
            return This;
        }
        public TQuery Offset(int count)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Offset, count);
            return This;
        }
        public TQuery Union(DBQueryBase query)
        {
            if (query == null)
            {
                throw DBInternal.ArgumentNullException(nameof(query));
            }

            Structure.Add(DBQueryStructureType.UnionAll, query);
            return This;
        }
        public TQuery UnionDistinct(DBQueryBase query)
        {
            if (query == null)
            {
                throw DBInternal.ArgumentNullException(nameof(query));
            }

            Structure.Add(DBQueryStructureType.UnionDistinct, query);
            return This;
        }

        public TQuery InnerJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.InnerJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery LeftJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.LeftJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery RightJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.RightJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery FullJoin<T, T2>()
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.FullJoin_type, typeof(T), typeof(T2));
            return This;
        }
        public TQuery InnerJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.InnerJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.LeftJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery RightJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.RightJoin, joinColumnName, columnName);
            return This;
        }
        public TQuery FullJoin(string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.FullJoin, joinColumnName, columnName);
            return This;
        }

        public TQuery InnerJoinAs<T, T2>(string alias)
              where T : DBOrmTableBase
              where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.InnerJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery LeftJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.LeftJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery RightJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.RightJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery FullJoinAs<T, T2>(string alias)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.FullJoinAs_type, typeof(T), typeof(T2), alias);
            return This;
        }
        public TQuery InnerJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.InnerJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery LeftJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.LeftJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery RightJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.RightJoinAs, alias, joinColumnName, columnName);
            return This;
        }
        public TQuery FullJoinAs(string alias, string joinColumnName, string columnName)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw DBInternal.ArgumentNullException(nameof(alias));
            }

            if (string.IsNullOrEmpty(joinColumnName))
            {
                throw DBInternal.ArgumentNullException(nameof(joinColumnName));
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.FullJoinAs, alias, joinColumnName, columnName);
            return This;
        }

        public TQuery Where<T>(Expression<Func<T, bool>> expression)
            where T : DBOrmTableBase
        {
            Structure.Add(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2>(Expression<Func<T, T2, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            Structure.Add(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3>(Expression<Func<T, T2, T3, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            Structure.Add(DBQueryStructureType.Where_expression, expression.Body);
            return This;
        }
        public TQuery Where<T, T2, T3, T4>(Expression<Func<T, T2, T3, T4, bool>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            Structure.Add(DBQueryStructureType.Where_expression, expression.Body);
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
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(equalOperator))
            {
                throw DBInternal.ArgumentNullException(nameof(equalOperator));
            }

            Structure.Add(DBQueryStructureType.Where, columnName, equalOperator, value);
            return This;
        }
        public TQuery WhereBetween(string columnName, object value1, object value2)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            Structure.Add(DBQueryStructureType.WhereBetween, columnName, value1, value2);
            return This;
        }
        public TQuery WhereUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw DBInternal.ArgumentNullException(nameof(value));
            }

            Structure.Add(DBQueryStructureType.WhereUpper, columnName, value);
            return This;
        }
        public TQuery WhereContaining(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw DBInternal.ArgumentNullException(nameof(value));
            }

            Structure.Add(DBQueryStructureType.WhereContaining, columnName, value);
            return This;
        }
        public TQuery WhereContainingUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw DBInternal.ArgumentNullException(nameof(value));
            }

            Structure.Add(DBQueryStructureType.WhereContainingUpper, columnName, value);
            return This;
        }
        public TQuery WhereLike(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw DBInternal.ArgumentNullException(nameof(value));
            }

            Structure.Add(DBQueryStructureType.WhereLike, columnName, value);
            return This;
        }
        public TQuery WhereLikeUpper(string columnName, string value)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw DBInternal.ArgumentNullException(nameof(value));
            }

            Structure.Add(DBQueryStructureType.WhereLikeUpper, columnName, value);
            return This;
        }
        public TQuery WhereIn(string columnName, DBQueryBase query)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (query == null)
            {
                throw DBInternal.ArgumentNullException(nameof(query));
            }

            Structure.Add(DBQueryStructureType.WhereIn_command, columnName, query);
            return This;
        }
        public TQuery WhereIn(string columnName, params object[] values)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw DBInternal.ArgumentNullException(nameof(columnName));
            }

            if (values == null || values.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(values));
            }

            Structure.Add(DBQueryStructureType.WhereIn_values, columnName, values);
            return This;
        }

        public TQuery OrderBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderBy_expression, expression.Body);
            return This;
        }
        public TQuery OrderBy(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderBy, columns);
            return This;
        }
        public TQuery OrderByDesc(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDesc, columns);
            return This;
        }
        public TQuery OrderByUpper(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByUpper, columns);
            return This;
        }
        public TQuery OrderByUpperDesc(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByUpperDesc, columns);
            return This;
        }

        public TQuery GroupBy<T>(Expression<Func<T, object>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T>(Expression<Func<T, object[]>> expression)
            where T : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2>(Expression<Func<T, T2, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy<T, T2, T3>(Expression<Func<T, T2, T3, object[]>> expression)
            where T : DBOrmTableBase
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupBy_expression, expression.Body);
            return This;
        }
        public TQuery GroupBy(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.GroupBy, columns);
            return This;
        }

        public TQuery Having<T>(Expression<Func<T, bool>> expression)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.Having_expression, expression.Body);
            return This;
        }
    }

    /// <summary>
    /// Представляет структирированный запрос базы данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public class DBQuery : DBQueryBase<DBQuery>
    {
        public DBQuery(DBTable table, DBContext context) : base(table, context)
        {
        }
    }

    /// <summary>
    /// Представляет типизированный запрос базы данных для таблиц <see cref="DBOrmTableBase"/>.
    /// </summary>
    public class DBQuery<TTable> : DBQueryBase<DBQuery<TTable>> where TTable : DBOrmTableBase
    {
        public DBQuery(DBTable table, DBContext context) : base(table, context)
        {
        }

        // Работа с контекстом БД
        public new TTable ReadRow()
        {
            return Context.GetRowInternal<TTable>(this);
        }
        public new TTable GetRowOrNew()
        {
            return Context.GetRowOrNewInternal<TTable>(this);
        }
        public new DBReader<TTable> Read()
        {
            return Context.ReadInternal<TTable>(this);
        }

        public new DBQuery<TTable> Select(Expression<Func<object>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TTable> Select(Expression<Func<TTable, object>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TTable> Select(Expression<Func<TTable, object[]>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TTable> Select<T2>(Expression<Func<TTable, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return base.Select(expression);
        }
        public DBQuery<TTable> Select<T2, T3>(Expression<Func<TTable, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return base.Select(expression);
        }
        public DBQuery<TTable> Select<T2, T3, T4>(Expression<Func<TTable, T2, T3, T4, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            return base.Select(expression);
        }

        public DBQuery<TTable> Where(Expression<Func<TTable, bool>> expression)
        {
            return base.Where(expression);
        }
        public DBQuery<TTable> Where<T2>(Expression<Func<TTable, T2, bool>> expression)
            where T2 : DBOrmTableBase
        {
            return base.Where(expression);
        }
        public DBQuery<TTable> Where<T2, T3>(Expression<Func<TTable, T2, T3, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return base.Where(expression);
        }
        public DBQuery<TTable> Where<T2, T3, T4>(Expression<Func<TTable, T2, T3, T4, bool>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
            where T4 : DBOrmTableBase
        {
            return base.Where(expression);
        }

        public DBQuery<TTable> InnerJoin<T2>() where T2 : DBOrmTableBase
        {
            return InnerJoin<TTable, T2>();
        }
        public DBQuery<TTable> LeftJoin<T2>() where T2 : DBOrmTableBase
        {
            return LeftJoin<TTable, T2>();
        }
        public DBQuery<TTable> RightJoin<T2>() where T2 : DBOrmTableBase
        {
            return RightJoin<TTable, T2>();
        }
        public DBQuery<TTable> FullJoin<T2>() where T2 : DBOrmTableBase
        {
            return FullJoin<TTable, T2>();
        }

        public DBQuery<TTable> InnerJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return InnerJoinAs<TTable, T2>(alias);
        }
        public DBQuery<TTable> LeftJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return LeftJoinAs<TTable, T2>(alias);
        }
        public DBQuery<TTable> RightJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return RightJoinAs<TTable, T2>(alias);
        }
        public DBQuery<TTable> FullJoinAs<T2>(string alias) where T2 : DBOrmTableBase
        {
            return FullJoinAs<TTable, T2>(alias);
        }

        public DBQuery<TTable> OrderBy(Expression<Func<TTable, object>> expression)
        {
            return OrderBy<TTable>(expression);
        }
        public DBQuery<TTable> OrderBy(Expression<Func<TTable, object[]>> expression)
        {
            return OrderBy<TTable>(expression);
        }
        public DBQuery<TTable> OrderBy<T2>(Expression<Func<TTable, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return OrderBy<TTable, T2>(expression);
        }
        public DBQuery<TTable> OrderBy<T2, T3>(Expression<Func<TTable, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return OrderBy<TTable, T2, T3>(expression);
        }

        public DBQuery<TTable> GroupBy(Expression<Func<TTable, object>> expression)
        {
            return GroupBy<TTable>(expression);
        }
        public DBQuery<TTable> GroupBy(Expression<Func<TTable, object[]>> expression)
        {
            return GroupBy<TTable>(expression);
        }
        public DBQuery<TTable> GroupBy<T2>(Expression<Func<TTable, T2, object[]>> expression)
            where T2 : DBOrmTableBase
        {
            return GroupBy<TTable, T2>(expression);
        }
        public DBQuery<TTable> GroupBy<T2, T3>(Expression<Func<TTable, T2, T3, object[]>> expression)
            where T2 : DBOrmTableBase
            where T3 : DBOrmTableBase
        {
            return GroupBy<TTable, T2, T3>(expression);
        }

        public DBQuery<TTable> Having(Expression<Func<TTable, bool>> expression)
        {
            return Having<TTable>(expression);
        }
    }
}
