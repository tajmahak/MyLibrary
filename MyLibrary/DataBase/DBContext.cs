using MyLibrary.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет механизм для работы с БД.
    /// </summary>
    public class DBContext
    {
        public DBModelBase Model { get; private set; }
        public DbConnection Connection { get; set; }
        public bool AutoCommit { get; set; } = true;
        public int RowCount
        {
            get
            {
                var count = 0;
                foreach (var tableRows in _tableRows.Values)
                {
                    count += tableRows.Count;
                }
                return count;
            }
        }
        private readonly Dictionary<DBTable, DBRowCollection> _tableRows = new Dictionary<DBTable, DBRowCollection>();
        private DbTransaction _transaction;

        internal DBContext(DBModelBase model, DbConnection connection)
        {
            Model = model;
            Connection = connection;

            if (!Model.Initialized)
            {
                Model.Initialize(Connection);
            }
        }

        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }
        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }
        public int Execute(DBQueryBase query)
        {
            if (query.StatementType == StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }
            try
            {
                OpenTransaction();
                int affectedRows;
                using (var command = Model.CreateCommand(Connection, query))
                {
                    command.Transaction = _transaction;
                    affectedRows = command.ExecuteNonQuery();
                }
                if (AutoCommit)
                {
                    CommitTransaction();
                }
                return affectedRows;
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }
        public void Save()
        {
            OpenTransaction();

            DBRow row = null;
            try
            {
                #region DELETE

                foreach (var tableRowsItem in _tableRows)
                {
                    var table = tableRowsItem.Key;
                    var rowCollection = tableRowsItem.Value;

                    for (var rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
                    {
                        row = rowCollection[rowIndex];
                        if (row.State == DataRowState.Deleted)
                        {
                            if (row.PrimaryKeyValue is DBTempId == false)
                            {
                                ExecuteDeleteCommand(row);
                            }
                        }
                    }
                    rowCollection.Clear(x => x.State == DataRowState.Deleted);
                }

                #endregion
                #region INSERT

                // список всех Insert-строк
                var rowContainerList = new List<InsertRowContainer>();
                // список всех временных ID, с привязкой к Insert-строкам
                var idContainerList = new Dictionary<DBTempId, List<TempIdContainer>>();

                #region Формирование списков

                foreach (var item in _tableRows)
                {
                    var table = item.Key;
                    var rowCollection = item.Value;

                    for (var rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
                    {
                        row = rowCollection[rowIndex];

                        InsertRowContainer rowContainer = null;
                        if (row.State == DataRowState.Added)
                        {
                            rowContainer = new InsertRowContainer(row);
                            rowContainerList.Add(rowContainer);
                        }

                        for (var columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                        {
                            if (row[columnIndex] is DBTempId tempID)
                            {
                                var idContainer = new TempIdContainer(row, columnIndex);

                                if (rowContainer != null)
                                {
                                    idContainer.ParentContainer = rowContainer;
                                    rowContainer.TempIdCount++;
                                }

                                if (idContainerList.ContainsKey(tempID))
                                {
                                    idContainerList[tempID].Add(idContainer);
                                }
                                else
                                {
                                    idContainerList.Add(tempID, new List<TempIdContainer>
                                    {
                                        idContainer
                                    });
                                }
                            }
                        }
                    }
                }
                rowContainerList.Sort((x, y) => x.TempIdCount.CompareTo(y.TempIdCount));

                #endregion

                var saveError = false;
                for (var i = 0; i < rowContainerList.Count; i++)
                {
                    var rowContainer = rowContainerList[i];
                    row = rowContainer.Row;

                    if (rowContainer.TempIdCount == 1)
                    {
                        var tempID = (DBTempId)row.PrimaryKeyValue;
                        var newID = ExecuteInsertCommand(row);

                        #region Замена временных Id на присвоенные

                        var idContainer = idContainerList[tempID];
                        for (var j = 0; j < idContainer.Count; j++)
                        {
                            var list = idContainer[j];
                            list.Row[list.ColumnIndex] = newID;

                            if (list.ParentContainer != null)
                            {
                                list.ParentContainer.TempIdCount--;
                            }
                        }

                        #endregion
                        row.State = DataRowState.Unchanged;
                        saveError = false;
                    }
                    else
                    {
                        if (saveError)
                        {
                            throw DBInternal.DbSaveWrongRelationsException();
                        }

                        rowContainerList.FindAll(x => x.TempIdCount > 0);
                        rowContainerList.Sort((x, y) => x.TempIdCount.CompareTo(y.TempIdCount));
                        i = -1;
                        saveError = true;
                    }
                }
                idContainerList.Clear();

                #endregion
                #region UPDATE

                foreach (var tableRowsItem in _tableRows)
                {
                    var table = tableRowsItem.Key;
                    var rowCollection = tableRowsItem.Value;

                    for (var rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
                    {
                        row = rowCollection[rowIndex];
                        if (row.State == DataRowState.Modified)
                        {
                            ExecuteUpdateCommand(row);
                            row.State = DataRowState.Unchanged;
                        }
                    }
                }

                #endregion

                if (AutoCommit)
                {
                    CommitTransaction();
                }
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                Clear();
                throw DBInternal.DbSaveException(row, ex);
            }
        }

        public DBQuery Query(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table, this);
            return query;
        }
        public DBQuery<TRow> Query<TRow>() where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<TRow>(table, this);
            return query;
        }

        #region Работа с данными

        public DBRow NewRow(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = table.CreateRow();
            AddRowInternal(row);
            return row;
        }
        public TRow NewRow<TRow>() where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var row = NewRow(tableName);
            return CreateOrmRow<TRow>(row);
        }

        public DBReader<DBRow> Read(DBQueryBase query)
        {
            return ReadInternal(query, row => row);
        }
        public DBReader<DBRow> Read(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return Read(query);
        }
        public DBReader<TRow> Read<TRow>(DBQueryBase query) where TRow : DBOrmRowBase
        {
            return ReadInternal(query, row => CreateOrmRow<TRow>(row));
        }
        public DBReader<TRow> Read<TRow>(DBQuery<TRow> query) where TRow : DBOrmRowBase
        {
            return ReadInternal(query, row => CreateOrmRow<TRow>(row));
        }
        public DBReader<TRow> Read<TRow>(Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Where(whereExpression);
            return Read(query);
        }
        public DBReader<TRow> Read<TRow>(params object[] columnConditionPair) where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return Read<TRow>(query);
        }
        public DBReader<T> Read<T>(DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            return ReadInternal(query, rowConverter);
        }
        public DBReader<T> Read<TRow, T>(DBQuery<TRow> query, Func<TRow, T> rowConverter) where TRow : DBOrmRowBase
        {
            return ReadInternal(query, row => rowConverter(CreateOrmRow<TRow>(row)));
        }

        public DBRow ReadRow(DBQueryBase query)
        {
            return ReadRowInternal(query, row => row);
        }
        public DBRow ReadRow(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadRow(query);
        }
        public TRow ReadRow<TRow>(DBQueryBase query) where TRow : DBOrmRowBase
        {
            return ReadRowInternal(query, row => CreateOrmRow<TRow>(row));
        }
        public TRow ReadRow<TRow>(DBQuery<TRow> query) where TRow : DBOrmRowBase
        {
            return ReadRowInternal<TRow>(query, row => CreateOrmRow<TRow>(row));
        }
        public TRow ReadRow<TRow>(Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Where(whereExpression);
            return ReadRow(query);
        }
        public TRow ReadRow<TRow>(params object[] columnConditionPair) where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadRow<TRow>(query);
        }
        public T ReadRow<T>(DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            return ReadRowInternal<T>(query, rowConverter);
        }
        public T ReadRow<TRow, T>(DBQuery<TRow> query, Func<TRow, T> rowConverter) where TRow : DBOrmRowBase
        {
            return ReadRowInternal(query, row => rowConverter(CreateOrmRow<TRow>(row)));
        }

        public DBRow ReadRowOrNew(DBQueryBase query)
        {
            return ReadRowOrNewInternal(query, row => row);
        }
        public DBRow ReadRowOrNew(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var row = ReadRowOrNew(query);

            if (row.State == DataRowState.Added)
            {
                // установка значений в строку согласно аргументам
                for (var i = 0; i < columnConditionPair.Length; i += 2)
                {
                    var columnName = (string)columnConditionPair[i];
                    var value = columnConditionPair[i + 1];
                    row[columnName] = value;
                }
            }

            return row;
        }
        public TRow ReadRowOrNew<TRow>(DBQueryBase query) where TRow : DBOrmRowBase
        {
            return ReadRowOrNewInternal(query, row => CreateOrmRow<TRow>(row));
        }
        public TRow ReadRowOrNew<TRow>(DBQuery<TRow> query) where TRow : DBOrmRowBase
        {
            return ReadRowOrNewInternal(query, row => CreateOrmRow<TRow>(row));
        }
        public TRow ReadRowOrNew<TRow>(Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Where(whereExpression);
            return ReadRowOrNew(query);
        }
        public TRow ReadRowOrNew<TRow>(params object[] columnConditionPair) where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var item = ReadRowOrNew<TRow>(query);

            if (item.Row.State == DataRowState.Added)
            {
                // установка значений в строку согласно аргументам
                for (var i = 0; i < columnConditionPair.Length; i += 2)
                {
                    var columnName = (string)columnConditionPair[i];
                    var value = columnConditionPair[i + 1];
                    item[columnName] = value;
                }
            }

            return item;
        }

        public TValue ReadValue<TValue>(DBQueryBase query)
        {
            return ReadValueInternal<TValue>(query);
        }
        public TValue ReadValue<TValue, TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Select(columnName);
            query.Where(whereExpression);
            return ReadValueInternal<TValue>(query);
        }
        public TValue ReadValue<TValue>(string columnName, params object[] columnConditionPair)
        {
            var tableName = columnName.Split('.')[0];
            var query = CreateSelectQuery(tableName, columnConditionPair);
            query.Select(columnName);
            return ReadValueInternal<TValue>(query);
        }

        public bool RowExists(DBQueryBase query)
        {
            return RowExistsInternal(query);
        }
        public bool RowExists<TRow>(Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Where(whereExpression);
            return RowExistsInternal(query);
        }
        public bool RowExists<TRow>(params object[] columnConditionPair) where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return RowExistsInternal(query);
        }
        public bool RowExists(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return RowExistsInternal(query);
        }

        public int Add(DBRow row)
        {
            return AddRowInternal(row);
        }
        public int Add<TRow>(TRow row) where TRow : DBOrmRowBase
        {
            return AddRowInternal(row);
        }
        public int Add(IEnumerable<DBRow> collection)
        {
            return AddRowInternal(collection);
        }
        public int Add<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            return AddRowInternal(collection);
        }

        public void SaveAndClear()
        {
            Save();
            Clear();
        }
        public void SaveAndClear(DBRow row)
        {
            SaveAndClearInternal(row);
        }
        public void SaveAndClear<TRow>(TRow row) where TRow : DBOrmRowBase
        {
            SaveAndClearInternal(row);
        }
        public void SaveAndClear(IEnumerable<DBRow> collection)
        {
            SaveAndClearInternal(collection);
        }
        public void SaveAndClear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            SaveAndClearInternal(collection);
        }

        public void Clear()
        {
            foreach (var rowCollection in _tableRows.Values)
            {
                foreach (var row in rowCollection)
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                }
                rowCollection.Clear();
            }
            _tableRows.Clear();
        }
        public void Clear<TRow>()
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            Clear(tableName);
        }
        public void Clear(string tableName)
        {
            var table = Model.GetTable(tableName);
            if (_tableRows.TryGetValue(table, out var rowCollection))
            {
                rowCollection.Clear();
            }
        }
        public void Clear(DBRow row)
        {
            ClearInternal(row);
        }
        public void Clear<TRow>(TRow row) where TRow : DBOrmRowBase
        {
            ClearInternal(row);
        }
        public void Clear(IEnumerable<DBRow> collection)
        {
            ClearInternal(collection);
        }
        public void Clear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            ClearInternal(collection);
        }

        #endregion

        private T ReadRowInternal<T>(DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            query.Structure.Add(DBQueryStructureType.Limit, 1);
            foreach (var row in ReadInternal<T>(query, rowConverter))
            {
                return row;
            }
            return default;
        }
        private T ReadRowOrNewInternal<T>(DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            var row = ReadRowInternal(query, rowConverter);
            if (row != null)
            {
                AddRowInternal(row);
            }
            else
            {
                var dbRow = NewRow(query.Table.Name);
                row = rowConverter(dbRow);
            }
            return row;
        }
        private DBReader<T> ReadInternal<T>(DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            if (query.StatementType != StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }

            return new DBReader<T>(Connection, Model, query, rowConverter);
        }
        private TValue ReadValueInternal<TValue>(DBQueryBase query)
        {
            if (query.StatementType == StatementType.Select) // могут быть команды с блоками RETURNING и т.п.
            {
                query.Structure.Add(DBQueryStructureType.Limit, 1);
            }

            using (var command = Model.CreateCommand(Connection, query))
            {
                var value = command.ExecuteScalar();
                return Format.Convert<TValue>(value);
            }
        }
        private bool RowExistsInternal(DBQueryBase query)
        {
            var row = ReadRowInternal<DBRow>(query, x => x);
            return (row != null);
        }

        private int AddRowInternal<T>(T value)
        {
            if (value is IEnumerable)
            {
                return AddCollectionInternal((IEnumerable)value);
            }

            var dbRow = ExtractDBRow(value);
            if (dbRow.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            if (dbRow.State == DataRowState.Deleted && dbRow.PrimaryKeyValue is DBTempId)
            {
                return 0;
            }

            if (!_tableRows.TryGetValue(dbRow.Table, out var rowCollection))
            {
                rowCollection = new DBRowCollection();
                _tableRows.Add(dbRow.Table, rowCollection);
            }

            if (!rowCollection.Contains(dbRow))
            {
                rowCollection.Add(dbRow);
            }

            if (dbRow.State == DataRowState.Detached)
            {
                dbRow.State = DataRowState.Added;
            }

            return 1;
        }
        private void SaveAndClearInternal<T>(T value)
        {
            AddRowInternal(value);
            Save();
            ClearInternal(value);
        }
        private void ClearInternal<T>(T value)
        {
            if (value is IEnumerable)
            {
                ClearCollectionInternal((IEnumerable)value);
                return;
            }

            var dbRow = ExtractDBRow(value);
            if (dbRow.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            if (dbRow.State == DataRowState.Added)
            {
                dbRow.State = DataRowState.Detached;
            }

            if (_tableRows.TryGetValue(dbRow.Table, out var rowCollection))
            {
                rowCollection.Remove(dbRow);
                if (rowCollection.Count == 0)
                {
                    _tableRows.Remove(dbRow.Table);
                }
            }
        }
        private int AddCollectionInternal(IEnumerable collection)
        {
            var count = 0;
            foreach (var row in collection)
            {
                count += AddRowInternal(row);
            }
            return count;
        }
        private void ClearCollectionInternal(IEnumerable collection)
        {
            foreach (var row in collection)
            {
                ClearInternal(row);
            }
        }

        private void OpenTransaction()
        {
            if (_transaction == null)
            {
                _transaction = Connection.BeginTransaction();
            }
        }
        private object ExecuteInsertCommand(DBRow row)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = _transaction;
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Insert);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table[i].IsPrimary)
                    {
                        continue;
                    }

                    Model.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                    index++;
                }
                return Model.ExecuteInsertCommand(dbCommand);
            }
        }
        private void ExecuteUpdateCommand(DBRow row)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = _transaction;
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Update);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table[i].IsPrimary)
                    {
                        Model.AddCommandParameter(dbCommand, "@id", row[i]);
                        continue;
                    }
                    Model.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                    index++;
                }

                dbCommand.ExecuteNonQuery();
            }
        }
        private void ExecuteDeleteCommand(DBRow row)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = _transaction;
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Delete);
                Model.AddCommandParameter(dbCommand, "@id", row.PrimaryKeyValue);

                dbCommand.ExecuteNonQuery();
            }
        }
        private DBQuery CreateSelectQuery(string tableName, params object[] columnConditionPair)
        {
            if (columnConditionPair.Length % 2 != 0)
            {
                throw DBInternal.ParameterValuePairException();
            }

            var query = Query(tableName);
            for (var i = 0; i < columnConditionPair.Length; i += 2)
            {
                var columnName = (string)columnConditionPair[i];
                var value = columnConditionPair[i + 1];
                query.Where(columnName, value);
            }
            return query;
        }
        private static TRow CreateOrmRow<TRow>(DBRow row) where TRow : DBOrmRowBase
        {
            return (TRow)Activator.CreateInstance(typeof(TRow), row);
        }
        private static DBRow ExtractDBRow(object row)
        {
            if (row == null)
            {
                return null;
            }
            if (row is DBRow dbRow)
            {
                return dbRow;
            }
            if (row is DBOrmRowBase ormRow)
            {
                return ormRow.Row;
            }
            throw DBInternal.ExtractDBRowException(row.GetType());
        }

        private class InsertRowContainer
        {
            public DBRow Row;
            public int TempIdCount;

            public InsertRowContainer(DBRow row)
            {
                Row = row;
            }
        }
        private class TempIdContainer
        {
            public DBRow Row;
            public int ColumnIndex;
            public InsertRowContainer ParentContainer;

            public TempIdContainer(DBRow row, int columnIndex)
            {
                Row = row;
                ColumnIndex = columnIndex;
            }
        }
    }
}
