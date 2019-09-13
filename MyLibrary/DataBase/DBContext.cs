using System;
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

        public DBQuery Query(string tableName, params object[] columnConditionPair)
        {
            if (columnConditionPair.Length % 2 != 0)
            {
                throw DBInternal.ParameterValuePairException();
            }

            var table = Model.GetTable(tableName);
            var query = new DBQuery(table, this);
            for (var i = 0; i < columnConditionPair.Length; i += 2)
            {
                var columnName = (string)columnConditionPair[i];
                var value = columnConditionPair[i + 1];
                query.Where(columnName, value);
            }
            return query;
        }
        public DBQuery<TRow> Query<TRow>(Expression<Func<TRow, bool>> whereExpression = null) where TRow : DBOrmRowBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<TRow>(table, this);
            if (whereExpression != null)
            {
                query.Where(whereExpression);
            }
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
                        var newID = ExecuteInsertCommand(row, transaction);
                        commitInfo.InsertedRowsCount++;

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
                            commitInfo.UpdatedRowsCount += ExecuteUpdateCommand(row, transaction);
                            row.State = DataRowState.Unchanged;
                        }
                    }
                }

                #endregion

                transaction.Commit();
                transaction.Dispose();
                Clear();
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
            return Add(ExtractDBRow(row));
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
            Clear(ExtractDBRow(row));
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

        public TValue ReadValue<TRow, TValue>(string columnName, Expression<Func<TRow, bool>> whereExpression) where TRow : DBOrmRowBase
        {
            var query = Query<TRow>();
            query.Select(columnName);
            query.Where(whereExpression);
            return query.ReadValue<TValue>();
        }
        public bool ReadBoolean<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<bool>(columnName, whereExpression);
        }
        public byte ReadByte<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<byte>(columnName, whereExpression);
        }
        public byte[] ReadBytes<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<byte[]>(columnName, whereExpression);
        }
        public DateTime ReadDateTime<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<DateTime>(columnName, whereExpression);
        }
        public decimal ReadDecimal<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<decimal>(columnName, whereExpression);
        }
        public double ReadDouble<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<double>(columnName, whereExpression);
        }
        public short ReadInt16<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<short>(columnName, whereExpression);
        }
        public int ReadInt32<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<int>(columnName, whereExpression);
        }
        public long ReadInt64<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<long>(columnName, whereExpression);
        }
        public float ReadSingle<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<float>(columnName, whereExpression);
        }
        public string ReadString<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<string>(columnName, whereExpression);
        }
        public TimeSpan ReadTimeSpan<TRow>(string columnName, Expression<Func<TRow, bool>> whereExpression)
        {
            return ReadValue<TimeSpan>(columnName, whereExpression);
        }
        public TValue ReadValue<TValue>(string columnName, params object[] columnConditionPair)
        {
            var tableName = columnName.Split('.')[0];
            var query = Query(tableName, columnConditionPair);
            query.Select(columnName);
            return query.ReadValue<TValue>();
        }
        public bool ReadBoolean(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<bool>(columnName, columnConditionPair);
        }
        public byte ReadByte(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<byte>(columnName, columnConditionPair);
        }
        public byte[] ReadBytes(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<byte[]>(columnName, columnConditionPair);
        }
        public DateTime ReadDateTime(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<DateTime>(columnName, columnConditionPair);
        }
        public decimal ReadDecimal(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<decimal>(columnName, columnConditionPair);
        }
        public double ReadDouble(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<double>(columnName, columnConditionPair);
        }
        public short ReadInt16(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<short>(columnName, columnConditionPair);
        }
        public int ReadInt32(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<int>(columnName, columnConditionPair);
        }
        public long ReadInt64(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<long>(columnName, columnConditionPair);
        }
        public float ReadSingle(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<float>(columnName, columnConditionPair);
        }
        public string ReadString(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<string>(columnName, columnConditionPair);
        }
        public TimeSpan ReadTimeSpan(string columnName, params object[] columnConditionPair)
        {
            return ReadValue<TimeSpan>(columnName, columnConditionPair);
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
                        continue;
                    }
                    Model.AddCommandParameter(dbCommand, string.Concat("@p", index), row[i]);
                    index++;
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
        private static DBRow ExtractDBRow(DBOrmRowBase row)
        {
            if (row == null)
            {
                return null;
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
