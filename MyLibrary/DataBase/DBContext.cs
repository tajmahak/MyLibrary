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

        public DBQuery Query(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table, this);
            return query;
        }
        public DBQuery<TTable> Query<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<TTable>(table, this);
            return query;
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

        #region Работа с данными

        public TTable NewRow<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            return NewRowInternal<TTable>(tableName);
        }
        public DBRow NewRow(string tableName)
        {
            return NewRowInternal<DBRow>(tableName);
        }

        public TTable ReadRow<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return ReadRowInternal<TTable>(query);
        }
        public TTable ReadRow<TTable>(DBQuery<TTable> query) where TTable : DBOrmTableBase
        {
            return ReadRowInternal<TTable>(query);
        }
        public TTable ReadRow<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return ReadRowInternal<TTable>(query);
        }
        public TTable ReadRow<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadRowInternal<TTable>(query);
        }
        public DBRow ReadRow(DBQueryBase query)
        {
            return ReadRowInternal<DBRow>(query);
        }
        public DBRow ReadRow(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadRowInternal<DBRow>(query);
        }

        public TTable ReadRowOrNew<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return ReadRowOrNewInternal<TTable>(query);
        }
        public TTable ReadRowOrNew<TTable>(DBQuery<TTable> query) where TTable : DBOrmTableBase
        {
            return ReadRowOrNewInternal<TTable>(query);
        }
        public TTable ReadRowOrNew<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return ReadRowOrNewInternal<TTable>(query);
        }
        public TTable ReadRowOrNew<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var item = ReadRowOrNewInternal<TTable>(query);

            if (item.Row.State == DataRowState.Added)
            {
                // установка значений в строку согласно аргументам
                for (var i = 0; i < columnConditionPair.Length; i += 2)
                {
                    var columnName = (string)columnConditionPair[i];
                    var value = columnConditionPair[i + 1];
                    item.Row[columnName] = value;
                }
            }

            return item;
        }
        public DBRow ReadRowOrNew(DBQueryBase query)
        {
            return ReadRowOrNewInternal<DBRow>(query);
        }
        public DBRow ReadRowOrNew(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var row = ReadRowOrNewInternal<DBRow>(query);

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

        public DBReader<TTable> Read<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return ReadInternal<TTable>(query);
        }
        public DBReader<TTable> Read<TTable>(DBQuery<TTable> query) where TTable : DBOrmTableBase
        {
            return ReadInternal<TTable>(query);
        }
        public DBReader<TTable> Read<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return ReadInternal<TTable>(query);
        }
        public DBReader<TTable> Read<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadInternal<TTable>(query);
        }
        public DBReader<DBRow> Read(DBQueryBase query)
        {
            return ReadInternal<DBRow>(query);
        }
        public DBReader<DBRow> Read(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ReadInternal<DBRow>(query);
        }

        public TType ReadValue<TType>(DBQueryBase query)
        {
            return ReadValueInternal<TType>(query);
        }
        public TType ReadValue<TType, TTable>(string columnName, Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Select(columnName);
            query.Where(whereExpression);
            return ReadValueInternal<TType>(query);
        }
        public TType ReadValue<TType>(string columnName, params object[] columnConditionPair)
        {
            var tableName = columnName.Split('.')[0];
            var query = CreateSelectQuery(tableName, columnConditionPair);
            query.Select(columnName);
            return ReadValueInternal<TType>(query);
        }

        public bool RowExists(DBQueryBase query)
        {
            return RowExistsInternal(query);
        }
        public bool RowExists<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return RowExistsInternal(query);
        }
        public bool RowExists<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
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
        public int Add<TTable>(TTable row) where TTable : DBOrmTableBase
        {
            return AddRowInternal(row);
        }
        public int Add(IEnumerable<DBRow> collection)
        {
            return AddRowInternal(collection);
        }
        public int Add<TTable>(IEnumerable<TTable> collection) where TTable : DBOrmTableBase
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
        public void SaveAndClear<TTable>(TTable row) where TTable : DBOrmTableBase
        {
            SaveAndClearInternal(row);
        }
        public void SaveAndClear(IEnumerable<DBRow> collection)
        {
            SaveAndClearInternal(collection);
        }
        public void SaveAndClear<TTable>(IEnumerable<TTable> collection) where TTable : DBOrmTableBase
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
        public void Clear<TTable>()
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
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
        public void Clear<TTable>(TTable row) where TTable : DBOrmTableBase
        {
            ClearInternal(row);
        }
        public void Clear(IEnumerable<DBRow> collection)
        {
            ClearInternal(collection);
        }
        public void Clear<TTable>(IEnumerable<TTable> collection) where TTable : DBOrmTableBase
        {
            ClearInternal(collection);
        }

        #endregion

        internal T NewRowInternal<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = table.CreateRow();
            AddRowInternal(row);
            return DBInternal.PackRow<T>(row);
        }
        internal T ReadRowInternal<T>(DBQueryBase query)
        {
            query.Structure.Add(DBQueryStructureType.Limit, 1);
            foreach (var row in ReadInternal<T>(query))
            {
                return row;
            }
            return default;
        }
        internal T ReadRowOrNewInternal<T>(DBQueryBase query)
        {
            var row = ReadRowInternal<T>(query);

            if (row != null)
            {
                AddRowInternal(row);
                return row;
            }

            return NewRowInternal<T>(query.Table.Name);
        }
        internal DBReader<T> ReadInternal<T>(DBQueryBase query)
        {
            if (query.StatementType != StatementType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }

            return new DBReader<T>(Connection, Model, query);
        }
        internal TType ReadValueInternal<TType>(DBQueryBase query)
        {
            if (query.StatementType == StatementType.Select) // могут быть команды с блоками RETURNING и т.п.
            {
                query.Structure.Add(DBQueryStructureType.Limit, 1);
            }

            using (var command = Model.CreateCommand(Connection, query))
            {
                var value = command.ExecuteScalar();
                return Format.Convert<TType>(value);
            }
        }
        internal bool RowExistsInternal(DBQueryBase query)
        {
            var row = ReadRowInternal<DBRow>(query);
            return (row != null);
        }

        private int AddRowInternal<T>(T value)
        {
            if (value is IEnumerable)
            {
                return AddCollectionInternal((IEnumerable)value);
            }

            var dbRow = DBInternal.UnpackRow(value);
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

            var dbRow = DBInternal.UnpackRow(value);
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
            using (var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Insert);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table[i].IsPrimary)
                    {
                        continue;
                    }

                    Model.AddCommandParameter(cmd, string.Concat("@p", index), row[i]);
                    index++;
                }
                return Model.ExecuteInsertCommand(cmd);
            }
        }
        private void ExecuteUpdateCommand(DBRow row)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Update);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table[i].IsPrimary)
                    {
                        Model.AddCommandParameter(cmd, "@id", row[i]);
                        continue;
                    }
                    Model.AddCommandParameter(cmd, string.Concat("@p", index), row[i]);
                    index++;
                }

                cmd.ExecuteNonQuery();
            }
        }
        private void ExecuteDeleteCommand(DBRow row)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Delete);
                Model.AddCommandParameter(cmd, "@id", row.PrimaryKeyValue);

                cmd.ExecuteNonQuery();
            }
        }
        private DBQuery CreateSelectQuery(string tableName, params object[] columnConditionPair)
        {
            if (columnConditionPair.Length % 2 != 0)
            {
                throw DBInternal.ParameterValuePairException();
            }

            var cmd = Query(tableName);
            for (var i = 0; i < columnConditionPair.Length; i += 2)
            {
                var columnName = (string)columnConditionPair[i];
                var value = columnConditionPair[i + 1];
                cmd.Where(columnName, value);
            }
            return cmd;
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
