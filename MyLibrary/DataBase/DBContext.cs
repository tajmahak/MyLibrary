using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет механизм для работы с БД.
    /// </summary>
    public class DBContext
    {
        public DBProvider Provider { get; private set; }
        public DbConnection Connection { get; set; }
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

        public DBContext(DBProvider provider, DbConnection connection)
        {
            Provider = provider;
            Connection = connection;

            if (!Provider.Initialized)
            {
                Provider.Initialize(Connection);
            }
        }

        public DBQuery Select(string tableName)
        {
            return CreateQuery(tableName, StatementType.Select);
        }
        public DBQuery Insert(string tableName)
        {
            return CreateQuery(tableName, StatementType.Insert);
        }
        public DBQuery Update(string tableName)
        {
            return CreateQuery(tableName, StatementType.Update);
        }
        public DBQuery Delete(string tableName)
        {
            return CreateQuery(tableName, StatementType.Delete);
        }
        public DBQuery<TRow> Select<TRow>() where TRow : DBOrmRow
        {
            return CreateQuery<TRow>(StatementType.Select);
        }
        public DBQuery<TRow> Insert<TRow>() where TRow : DBOrmRow
        {
            return CreateQuery<TRow>(StatementType.Insert);
        }
        public DBQuery<TRow> Update<TRow>() where TRow : DBOrmRow
        {
            return CreateQuery<TRow>(StatementType.Update);
        }
        public DBQuery<TRow> Delete<TRow>() where TRow : DBOrmRow
        {
            return CreateQuery<TRow>(StatementType.Delete);
        }

        public DBContextCommitInfo Commit()
        {
            var commitInfo = new DBContextCommitInfo();
            DBRow row = null;
            DbTransaction transaction = null;
            try
            {
                transaction = Connection.BeginTransaction();

                #region DELETE

                var emptyTables = new List<DBTable>();

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
                                commitInfo.DeletedRowsCount += ExecuteDeleteCommand(row, transaction);
                            }
                        }
                    }

                    rowCollection.Clear(x => x.State == DataRowState.Deleted);
                    if (rowCollection.Count == 0)
                    {
                        emptyTables.Add(table);
                    }
                }

                foreach (var table in emptyTables)
                {
                    _tableRows.Remove(table);
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
                                var idContainer = new TempIdContainer(row, columnIndex, rowContainer);
                                if (rowContainer != null)
                                {
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

                var insertError = false;
                for (var i = 0; i < rowContainerList.Count; i++)
                {
                    var rowContainer = rowContainerList[i];
                    row = rowContainer.Row;

                    if (rowContainer.TempIdCount == 1)
                    {
                        var tempId = (DBTempId)row.PrimaryKeyValue;
                        var rowId = ExecuteInsertCommand(row, transaction);
                        commitInfo.InsertedRowsCount++;

                        // замена временных ID на присвоенные
                        var idContainer = idContainerList[tempId];
                        for (var j = 0; j < idContainer.Count; j++)
                        {
                            var list = idContainer[j];
                            list.Row[list.ColumnIndex] = rowId;

                            if (list.InsertRowContainer != null)
                            {
                                list.InsertRowContainer.TempIdCount--;
                            }
                        }

                        row.State = DataRowState.Unchanged;
                        insertError = false;
                    }
                    else
                    {
                        if (insertError)
                        {
                            throw DBInternal.DbSaveWrongRelationsException();
                        }
                        rowContainerList.Sort((x, y) => x.TempIdCount.CompareTo(y.TempIdCount));
                        i = -1;
                        insertError = true;
                    }
                }

                rowContainerList.Clear();
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
                            commitInfo.UpdatedRowsCount += ExecuteUpdateCommand(row, transaction);
                            row.State = DataRowState.Unchanged;
                        }
                    }
                }

                #endregion

                transaction.Commit();
                transaction.Dispose();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                transaction?.Dispose();
                throw DBInternal.DbSaveException(row, ex);
            }
            return commitInfo;
        }

        public DBRow NewRow(string tableName)
        {
            var table = Provider.Tables[tableName];
            var row = table.CreateRow();
            AddRow(row);
            return row;
        }
        public TRow NewRow<TRow>() where TRow : DBOrmRow
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var row = NewRow(tableName);
            return DBInternal.CreateOrmRow<TRow>(row);
        }

        public int AddRow(DBRow row)
        {
            if (row.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            if (row.State == DataRowState.Deleted && row.PrimaryKeyValue is DBTempId)
            {
                return 0;
            }

            if (!_tableRows.TryGetValue(row.Table, out var rowCollection))
            {
                rowCollection = new DBRowCollection();
                _tableRows.Add(row.Table, rowCollection);
            }

            if (!rowCollection.Contains(row))
            {
                rowCollection.Add(row);
            }

            if (row.State == DataRowState.Detached)
            {
                row.State = DataRowState.Added;
            }

            return 1;
        }
        public int AddRow<TRow>(TRow row) where TRow : DBOrmRow
        {
            return AddRow(DBInternal.ExtractDBRow(row));
        }
        public int AddRows(IEnumerable<DBRow> collection)
        {
            var count = 0;
            foreach (var row in collection)
            {
                count += AddRow(row);
            }
            return count;
        }
        public int AddRows<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            var count = 0;
            foreach (var row in collection)
            {
                count += AddRow(row);
            }
            return count;
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
            var table = Provider.Tables[tableName];
            if (_tableRows.TryGetValue(table, out var rowCollection))
            {
                foreach (var row in rowCollection)
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                }
                _tableRows.Remove(table);
            }
        }
        public void Clear(DBRow row)
        {
            if (row.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            if (_tableRows.TryGetValue(row.Table, out var rowCollection))
            {
                if (rowCollection.Remove(row))
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                    if (rowCollection.Count == 0)
                    {
                        _tableRows.Remove(row.Table);
                    }
                }
            }
        }
        public void Clear<TRow>(TRow row) where TRow : DBOrmRow
        {
            Clear(DBInternal.ExtractDBRow(row));
        }
        public void Clear(IEnumerable<DBRow> collection)
        {
            foreach (var row in collection)
            {
                Clear(row);
            }
        }
        public void Clear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            foreach (var row in collection)
            {
                Clear(row);
            }
        }

        public void CommitAndClear()
        {
            Commit();
            Clear();
        }
        public void CommitAndClear(DBRow row)
        {
            AddRow(row);
            Commit();
            Clear(row);
        }
        public void CommitAndClear<TRow>(TRow row) where TRow : DBOrmRow
        {
            AddRow(row);
            Commit();
            Clear(row);
        }
        public void CommitAndClear(IEnumerable<DBRow> collection)
        {
            AddRows(collection);
            Commit();
            Clear(collection);
        }
        public void CommitAndClear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            AddRows(collection);
            Commit();
            Clear(collection);
        }

        private DBQuery CreateQuery(string tableName, StatementType statementType)
        {
            var table = Provider.Tables[tableName];
            var query = new DBQuery(table, this, statementType);
            return query;
        }
        private DBQuery<TRow> CreateQuery<TRow>(StatementType statementType) where TRow : DBOrmRow
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var table = Provider.Tables[tableName];
            var query = new DBQuery<TRow>(table, this, statementType);
            return query;
        }
        private object ExecuteInsertCommand(DBRow row, DbTransaction transaction)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = transaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Insert);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (!row.Table.Columns[i].IsPrimary)
                    {
                        Provider.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                        index++;
                    }
                }
                return Provider.ExecuteInsertCommand(dbCommand);
            }
        }
        private int ExecuteUpdateCommand(DBRow row, DbTransaction transaction)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = transaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Update);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                    {
                        Provider.AddCommandParameter(dbCommand, "@id", row[i]);
                    }
                    else
                    {
                        Provider.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                        index++;
                    }
                }

                return dbCommand.ExecuteNonQuery();
            }
        }
        private int ExecuteDeleteCommand(DBRow row, DbTransaction transaction)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = transaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Delete);
                Provider.AddCommandParameter(dbCommand, "@id", row.PrimaryKeyValue);

                return dbCommand.ExecuteNonQuery();
            }
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
            public InsertRowContainer InsertRowContainer;

            public TempIdContainer(DBRow row, int columnIndex, InsertRowContainer insertRowContainer)
            {
                Row = row;
                ColumnIndex = columnIndex;
                InsertRowContainer = insertRowContainer;
            }
        }
    }
}
