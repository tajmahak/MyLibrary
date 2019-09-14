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
        public DBModelBase Model { get; private set; }
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

        internal DBContext(DBModelBase model, DbConnection connection)
        {
            Model = model;
            Connection = connection;

            if (!Model.Initialized)
            {
                Model.Initialize(Connection);
            }
        }

        public DBQuery Select(string tableName)
        {
            var query = CreateQuery(tableName);
            return query;
        }
        public DBQuery Insert(string tableName)
        {
            var query = CreateQuery(tableName);
            query.Insert();
            return query;
        }
        public DBQuery Update(string tableName)
        {
            var query = CreateQuery(tableName);
            query.Update();
            return query;
        }
        public DBQuery Delete(string tableName)
        {
            var query = CreateQuery(tableName);
            query.Delete();
            return query;
        }
        public DBQuery<TRow> Select<TRow>() where TRow : DBOrmRowBase
        {
            var query = CreateQuery<TRow>();
            return query;
        }
        public DBQuery<TRow> Insert<TRow>() where TRow : DBOrmRowBase
        {
            var query = CreateQuery<TRow>();
            query.Insert();
            return query;
        }
        public DBQuery<TRow> Update<TRow>() where TRow : DBOrmRowBase
        {
            var query = CreateQuery<TRow>();
            query.Update();
            return query;
        }
        public DBQuery<TRow> Delete<TRow>() where TRow : DBOrmRowBase
        {
            var query = CreateQuery<TRow>();
            query.Delete();
            return query;
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

                        #region Замена временных Id на присвоенные

                        var idContainer = idContainerList[tempId];
                        for (var j = 0; j < idContainer.Count; j++)
                        {
                            var list = idContainer[j];
                            list.Row[list.ColumnIndex] = rowId;

                            if (list.ParentContainer != null)
                            {
                                list.ParentContainer.TempIdCount--;
                            }
                        }

                        #endregion
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
            var table = Model.GetTable(tableName);
            var row = table.CreateRow();
            Add(row);
            return row;
        }
        public TRow NewRow<TRow>() where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var row = NewRow(tableName);
            return DBInternal.CreateOrmRow<TRow>(row);
        }

        public int Add(DBRow row)
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
        public int Add<TRow>(TRow row) where TRow : DBOrmRowBase
        {
            return Add(DBInternal.ExtractDBRow(row));
        }
        public int Add(IEnumerable<DBRow> collection)
        {
            var count = 0;
            foreach (var row in collection)
            {
                count += Add(row);
            }
            return count;
        }
        public int Add<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            var count = 0;
            foreach (var row in collection)
            {
                count += Add(row);
            }
            return count;
        }

        public void SaveAndClear()
        {
            Commit();
            Clear();
        }
        public void SaveAndClear(DBRow row)
        {
            Add(row);
            Commit();
            Clear(row);
        }
        public void SaveAndClear<TRow>(TRow row) where TRow : DBOrmRowBase
        {
            Add(row);
            Commit();
            Clear(row);
        }
        public void SaveAndClear(IEnumerable<DBRow> collection)
        {
            Add(collection);
            Commit();
            Clear(collection);
        }
        public void SaveAndClear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            Add(collection);
            Commit();
            Clear(collection);
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
            var table = Model.GetTable(tableName);
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
        public void Clear<TRow>(TRow row) where TRow : DBOrmRowBase
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
        public void Clear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRowBase
        {
            foreach (var row in collection)
            {
                Clear(row);
            }
        }

        private DBQuery CreateQuery(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table, this);
            return query;
        }
        private DBQuery<TRow> CreateQuery<TRow>() where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<TRow>(table, this);
            return query;
        }
        private object ExecuteInsertCommand(DBRow row, DbTransaction transaction)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = transaction;
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Insert);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (!row.Table[i].IsPrimary)
                    {
                        Model.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                        index++;
                    }
                }
                return Model.ExecuteInsertCommand(dbCommand);
            }
        }
        private int ExecuteUpdateCommand(DBRow row, DbTransaction transaction)
        {
            using (var dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = transaction;
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Update);

                var index = 0;
                for (var i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table[i].IsPrimary)
                    {
                        Model.AddCommandParameter(dbCommand, "@id", row[i]);
                    }
                    else
                    {
                        Model.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
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
                dbCommand.CommandText = Model.GetDefaultSqlQuery(row.Table, StatementType.Delete);
                Model.AddCommandParameter(dbCommand, "@id", row.PrimaryKeyValue);

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
            public InsertRowContainer ParentContainer;

            public TempIdContainer(DBRow row, int columnIndex)
            {
                Row = row;
                ColumnIndex = columnIndex;
            }
        }
    }
}
