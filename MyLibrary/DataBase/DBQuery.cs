using MyLibrary.Data;
using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс структурированного запроса базы данных.
    /// </summary>
    public abstract class DBQueryBase
    {
        protected DBQueryBase(DBTable table, DBContext context, StatementType statementType)
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
            StatementType = statementType;
        }

        public StatementType StatementType { get; protected set; }
        public bool IsView { get; protected set; }
        public DBTable Table { get; private set; }
        protected internal DBContext Context { get; private set; }
        protected internal DBQueryStructureBlockCollection Structure { get; private set; } = new DBQueryStructureBlockCollection();

        // Работа с контекстом БД
        public DBReader<DBRow> Read()
        {
            return Read(row => row);
        }
        public DBReader<TRow> Read<TRow>() where TRow : DBOrmRow
        {
            return Read(row => DBInternal.CreateOrmRow<TRow>(row));
        }
        public DBReader<T> Read<T>(Converter<DBRow, T> rowConverter)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }
            return new DBReader<T>(Context.Provider, Context.Connection, this, rowConverter, CommandBehavior.Default);
        }

        public DBRow ReadRow()
        {
            return ReadRow(row => row);
        }
        public TRow ReadRow<TRow>() where TRow : DBOrmRow
        {
            return ReadRow(row => DBInternal.CreateOrmRow<TRow>(row));
        }
        public T ReadRow<T>(Converter<DBRow, T> rowConverter)
        {
            Structure.Add(DBQueryStructureType.Limit, 1);

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }

            var reader = new DBReader<T>(Context.Provider, Context.Connection, this, rowConverter, CommandBehavior.SingleRow);
            foreach (var row in reader)
            {
                return row;
            }

            return default;
        }

        public DBRow ReadRowOrNew()
        {
            var row = ReadRow();
            if (row != null)
            {
                Context.AddRow(row);
            }
            else
            {
                row = Context.NewRow(Table.Name);
            }
            return row;
        }
        public TRow ReadRowOrNew<TRow>() where TRow : DBOrmRow
        {
            var row = ReadRowOrNew();
            return DBInternal.CreateOrmRow<TRow>(row);
        }

        public int Execute()
        {
            if (StatementType == StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }
            DbTransaction transaction = null;
            try
            {
                transaction = Context.Connection.BeginTransaction();
                int affectedRows;
                using (var command = Context.Provider.CreateCommand(Context.Connection, this))
                {
                    command.Transaction = transaction;
                    affectedRows = command.ExecuteNonQuery();
                }
                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
        }
        public bool RowExists()
        {
            var row = ReadRow();
            return row != null;
        }

        public TValue ReadValue<TValue>()
        {
            if (StatementType == StatementType.Select) // могут быть команды с блоками RETURNING и т.п.
            {
                Structure.Add(DBQueryStructureType.Limit, 1);
            }

            using (var command = Context.Provider.CreateCommand(Context.Connection, this))
            {
                var value = command.ExecuteScalar();
                return Format.Convert<TValue>(value);
            }
        }
        public bool ReadBoolean()
        {
            return ReadValue<bool>();
        }
        public byte ReadByte()
        {
            return ReadValue<byte>();
        }
        public byte[] ReadBytes()
        {
            return ReadValue<byte[]>();
        }
        public DateTime ReadDateTime()
        {
            return ReadValue<DateTime>();
        }
        public decimal ReadDecimal()
        {
            return ReadValue<decimal>();
        }
        public double ReadDouble()
        {
            return ReadValue<double>();
        }
        public short ReadInt16()
        {
            return ReadValue<short>();
        }
        public int ReadInt32()
        {
            return ReadValue<int>();
        }
        public long ReadInt64()
        {
            return ReadValue<long>();
        }
        public float ReadSingle()
        {
            return ReadValue<float>();
        }
        public string ReadString()
        {
            return ReadValue<string>();
        }
        public TimeSpan ReadTimeSpan()
        {
            return ReadValue<TimeSpan>();
        }
    }

    /// <summary>
    /// Базовый класс с набором функций, представляющих запросы к базе данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public abstract class DBQueryBase<TQuery> : DBQueryBase where TQuery : DBQueryBase
    {
        private TQuery This => (TQuery)(object)this;

        internal DBQueryBase(DBTable table, DBContext context, StatementType statementType) : base(table, context, statementType)
        {
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
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
            return This;
        }
        public TQuery Select<TRow>(Expression<Func<TRow, object>> expression)
            where TRow : DBOrmRow
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
            return This;
        }
        public TQuery Select<TRow>(Expression<Func<TRow, object[]>> expression)
            where TRow : DBOrmRow
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
            return This;
        }
        public TQuery Select<TRow, TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
            return This;
        }
        public TQuery Select<TRow, TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
            return This;
        }
        public TQuery Select<TRow, TRow2, TRow3, TRow4>(Expression<Func<TRow, TRow2, TRow3, TRow4, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
            where TRow4 : DBOrmRow
        {
            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, expression.Body);
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

        public TQuery InnerJoin<TRow, TRow2>()
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.InnerJoinType, typeof(TRow), typeof(TRow2));
            return This;
        }
        public TQuery InnerJoin(Type rowType, Type row2Type)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.InnerJoinType, rowType, row2Type);
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
        public TQuery InnerJoinAs<TRow, TRow2>(string alias)
              where TRow : DBOrmRow
              where TRow2 : DBOrmRow
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
            Structure.Add(DBQueryStructureType.InnerJoinAsType, typeof(TRow), typeof(TRow2), alias);
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

        public TQuery LeftJoin<TRow, TRow2>()
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.LeftJoinType, typeof(TRow), typeof(TRow2));
            return This;
        }
        public TQuery LeftJoin(Type rowType, Type row2Type)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.LeftJoinType, rowType, row2Type);
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
        public TQuery LeftJoinAs<TRow, TRow2>(string alias)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
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
            Structure.Add(DBQueryStructureType.LeftJoinAsType, typeof(TRow), typeof(TRow2), alias);
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

        public TQuery RightJoin<TRow, TRow2>()
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.RightJoinType, typeof(TRow), typeof(TRow2));
            return This;
        }
        public TQuery RightJoin(Type rowType, Type row2Type)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.RightJoinType, rowType, row2Type);
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
        public TQuery RightJoinAs<TRow, TRow2>(string alias)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
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
            Structure.Add(DBQueryStructureType.RightJoinAsType, typeof(TRow), typeof(TRow2), alias);
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

        public TQuery FullJoin<TRow, TRow2>()
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.FullJoinType, typeof(TRow), typeof(TRow2));
            return This;
        }
        public TQuery FullJoin(Type rowType, Type row2Type)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.FullJoinType, rowType, row2Type);
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
        public TQuery FullJoinAs<TRow, TRow2>(string alias)
           where TRow : DBOrmRow
           where TRow2 : DBOrmRow
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
            Structure.Add(DBQueryStructureType.FullJoinAsType, typeof(TRow), typeof(TRow2), alias);
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

        public TQuery Join<TRow, TRow2, TKey>(DBQuery<TRow2> inner, Expression<Func<TRow, TKey>> outerKeySelector, Expression<Func<TRow2, TKey>> innerKeySelector, Expression<Func<TRow, TRow2, object>> resultSelector)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            IsView = true;
            Structure.Add(DBQueryStructureType.SelectExpression, resultSelector.Body);
            Structure.Add(DBQueryStructureType.Join, outerKeySelector.Body, innerKeySelector.Body);
            return This;
        }

        public TQuery Where<TRow>(Expression<Func<TRow, bool>> expression)
            where TRow : DBOrmRow
        {
            Structure.Add(DBQueryStructureType.WhereExpression, expression.Body);
            return This;
        }
        public TQuery Where<TRow, TRow2>(Expression<Func<TRow, TRow2, bool>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            Structure.Add(DBQueryStructureType.WhereExpression, expression.Body);
            return This;
        }
        public TQuery Where<TRow, TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, bool>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            Structure.Add(DBQueryStructureType.WhereExpression, expression.Body);
            return This;
        }
        public TQuery Where<TRow, TRow2, TRow3, TRow4>(Expression<Func<TRow, TRow2, TRow3, TRow4, bool>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
            where TRow4 : DBOrmRow
        {
            Structure.Add(DBQueryStructureType.WhereExpression, expression.Body);
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

            Structure.Add(DBQueryStructureType.WhereInQuery, columnName, query);
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

            Structure.Add(DBQueryStructureType.WhereInValues, columnName, values);
            return This;
        }

        public TQuery OrderBy<TRow>(Expression<Func<TRow, object>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByExpression, expression.Body);
            return This;
        }
        public TQuery OrderBy<TRow>(Expression<Func<TRow, object[]>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByExpression, expression.Body);
            return This;
        }
        public TQuery OrderBy<TRow, TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByExpression, expression.Body);
            return This;
        }
        public TQuery OrderBy<TRow, TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByExpression, expression.Body);
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

        public TQuery OrderByDescending<TRow>(Expression<Func<TRow, object>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDescendingExpression, expression.Body);
            return This;
        }
        public TQuery OrderByDescending<TRow>(Expression<Func<TRow, object[]>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDescendingExpression, expression.Body);
            return This;
        }
        public TQuery OrderByDescending<TRow, TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDescendingExpression, expression.Body);
            return This;
        }
        public TQuery OrderByDescending<TRow, TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDescendingExpression, expression.Body);
            return This;
        }
        public TQuery OrderByDescending(params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw DBInternal.ArgumentNullException(nameof(columns));
            }

            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.OrderByDescending, columns);
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
        public TQuery OrderByUpperDescendig(params string[] columns)
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

        public TQuery GroupBy<TRow>(Expression<Func<TRow, object>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupByExpression, expression.Body);
            return This;
        }
        public TQuery GroupBy<TRow>(Expression<Func<TRow, object[]>> expression)
            where TRow : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupByExpression, expression.Body);
            return This;
        }
        public TQuery GroupBy<TRow, TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupByExpression, expression.Body);
            return This;
        }
        public TQuery GroupBy<TRow, TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow : DBOrmRow
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.GroupByExpression, expression.Body);
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

        public TQuery Having<TRow>(Expression<Func<TRow, bool>> expression)
        {
            if (StatementType != StatementType.Select)
            {
                throw DBInternal.UnsupportedCommandContextException();
            }

            Structure.Add(DBQueryStructureType.HavingExpression, expression.Body);
            return This;
        }
    }

    /// <summary>
    /// Представляет структирированный запрос базы данных.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    public class DBQuery : DBQueryBase<DBQuery>
    {
        internal DBQuery(DBTable table, DBContext context, StatementType statementType) : base(table, context, statementType)
        {
        }
    }

    /// <summary>
    /// Представляет типизированный запрос базы данных для таблиц <see cref="DBOrmRow"/>.
    /// </summary>
    public class DBQuery<TRow> : DBQueryBase<DBQuery<TRow>> where TRow : DBOrmRow
    {
        internal DBQuery(DBTable table, DBContext context, StatementType statementType) : base(table, context, statementType)
        {
        }

        public new DBReader<TRow> Read()
        {
            return Read<TRow>();
        }
        public DBReader<T> Read<T>(Converter<TRow, T> rowConverter)
        {
            return Read(x => rowConverter(DBInternal.CreateOrmRow<TRow>(x)));
        }
        public new TRow ReadRow()
        {
            return ReadRow<TRow>();
        }
        public T ReadRow<T>(Converter<TRow, T> rowConverter)
        {
            return ReadRow(x => rowConverter(DBInternal.CreateOrmRow<TRow>(x)));
        }
        public new TRow ReadRowOrNew()
        {
            return ReadRowOrNew<TRow>();
        }
        public TRow NewRow()
        {
            return Context.NewRow<TRow>();
        }

        public new DBQuery<TRow> Select(Expression<Func<object>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TRow> Select(Expression<Func<TRow, object>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TRow> Select(Expression<Func<TRow, object[]>> expression)
        {
            return base.Select(expression);
        }
        public DBQuery<TRow> Select<TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow2 : DBOrmRow
        {
            return base.Select(expression);
        }
        public DBQuery<TRow> Select<TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            return base.Select(expression);
        }
        public DBQuery<TRow> Select<TRow2, TRow3, TRow4>(Expression<Func<TRow, TRow2, TRow3, TRow4, object[]>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
            where TRow4 : DBOrmRow
        {
            return base.Select(expression);
        }

        public DBQuery<TRow> Where(Expression<Func<TRow, bool>> expression)
        {
            return base.Where(expression);
        }
        public DBQuery<TRow> Where<TRow2>(Expression<Func<TRow, TRow2, bool>> expression)
            where TRow2 : DBOrmRow
        {
            return base.Where(expression);
        }
        public DBQuery<TRow> Where<TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, bool>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            return base.Where(expression);
        }
        public DBQuery<TRow> Where<TRow2, TRow3, TRow4>(Expression<Func<TRow, TRow2, TRow3, TRow4, bool>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
            where TRow4 : DBOrmRow
        {
            return base.Where(expression);
        }

        public DBQuery<TRow> InnerJoin<TRow2>() where TRow2 : DBOrmRow
        {
            return InnerJoin<TRow, TRow2>();
        }
        public DBQuery<TRow> InnerJoin<TRow2>(DBQuery<TRow2> query) where TRow2 : DBOrmRow
        {
            return InnerJoin(typeof(TRow), typeof(TRow2));
        }
        public DBQuery<TRow> InnerJoinAs<TRow2>(string alias) where TRow2 : DBOrmRow
        {
            return InnerJoinAs<TRow, TRow2>(alias);
        }

        public DBQuery<TRow> LeftJoin<TRow2>() where TRow2 : DBOrmRow
        {
            return LeftJoin<TRow, TRow2>();
        }
        public DBQuery<TRow> LeftJoin<TRow2>(DBQuery<TRow2> query) where TRow2 : DBOrmRow
        {
            return LeftJoin(typeof(TRow), typeof(TRow2));
        }
        public DBQuery<TRow> LeftJoinAs<TRow2>(string alias) where TRow2 : DBOrmRow
        {
            return LeftJoinAs<TRow, TRow2>(alias);
        }

        public DBQuery<TRow> RightJoin<TRow2>() where TRow2 : DBOrmRow
        {
            return RightJoin<TRow, TRow2>();
        }
        public DBQuery<TRow> RightJoin<TRow2>(DBQuery<TRow2> query) where TRow2 : DBOrmRow
        {
            return RightJoin(typeof(TRow), typeof(TRow2));
        }
        public DBQuery<TRow> RightJoinAs<TRow2>(string alias) where TRow2 : DBOrmRow
        {
            return RightJoinAs<TRow, TRow2>(alias);
        }

        public DBQuery<TRow> FullJoin<TRow2>() where TRow2 : DBOrmRow
        {
            return FullJoin<TRow, TRow2>();
        }
        public DBQuery<TRow> FullJoin<TRow2>(DBQuery<TRow2> query) where TRow2 : DBOrmRow
        {
            return FullJoin(typeof(TRow), typeof(TRow2));
        }
        public DBQuery<TRow> FullJoinAs<TRow2>(string alias) where TRow2 : DBOrmRow
        {
            return FullJoinAs<TRow, TRow2>(alias);
        }

        public DBQuery<TRow> Join<TRow2, TKey>(DBQuery<TRow2> inner, Expression<Func<TRow, TKey>> outerKeySelector, Expression<Func<TRow2, TKey>> innerKeySelector, Expression<Func<TRow, TRow2, object>> resultSelector) where TRow2 : DBOrmRow
        {
            return base.Join(inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        public DBQuery<TRow> OrderBy(Expression<Func<TRow, object>> expression)
        {
            return OrderBy<TRow>(expression);
        }
        public DBQuery<TRow> OrderBy(Expression<Func<TRow, object[]>> expression)
        {
            return OrderBy<TRow>(expression);
        }
        public DBQuery<TRow> OrderBy<TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow2 : DBOrmRow
        {
            return OrderBy<TRow, TRow2>(expression);
        }
        public DBQuery<TRow> OrderBy<TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            return OrderBy<TRow, TRow2, TRow3>(expression);
        }

        public DBQuery<TRow> OrderByDescending(Expression<Func<TRow, object>> expression)
        {
            return OrderByDescending<TRow>(expression);
        }
        public DBQuery<TRow> OrderByDescending(Expression<Func<TRow, object[]>> expression)
        {
            return OrderByDescending<TRow>(expression);
        }
        public DBQuery<TRow> OrderByDescending<TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow2 : DBOrmRow
        {
            return OrderByDescending<TRow, TRow2>(expression);
        }
        public DBQuery<TRow> OrderByDescending<TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            return OrderByDescending<TRow, TRow2, TRow3>(expression);
        }

        public DBQuery<TRow> GroupBy(Expression<Func<TRow, object>> expression)
        {
            return GroupBy<TRow>(expression);
        }
        public DBQuery<TRow> GroupBy(Expression<Func<TRow, object[]>> expression)
        {
            return GroupBy<TRow>(expression);
        }
        public DBQuery<TRow> GroupBy<TRow2>(Expression<Func<TRow, TRow2, object[]>> expression)
            where TRow2 : DBOrmRow
        {
            return GroupBy<TRow, TRow2>(expression);
        }
        public DBQuery<TRow> GroupBy<TRow2, TRow3>(Expression<Func<TRow, TRow2, TRow3, object[]>> expression)
            where TRow2 : DBOrmRow
            where TRow3 : DBOrmRow
        {
            return GroupBy<TRow, TRow2, TRow3>(expression);
        }

        public DBQuery<TRow> Having(Expression<Func<TRow, bool>> expression)
        {
            return Having<TRow>(expression);
        }
    }
}
