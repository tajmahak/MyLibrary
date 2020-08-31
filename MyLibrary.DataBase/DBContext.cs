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
        public DBContext(DBProvider provider, DbConnection connection)
        {
            Provider = provider;
            Connection = connection;

            if (!Provider.Initialized)
            {
                Provider.Initialize(Connection);
            }
        }

        public DBProvider Provider { get; private set; }
        public DbConnection Connection { get; set; }
        public int RowCount
        {
            get
            {
                int count = 0;
                foreach (DBRowCollection tableRows in tableRows.Values)
                {
                    count += tableRows.Count;
                }
                return count;
            }
        }

        private readonly Dictionary<DBTable, DBRowCollection> tableRows = new Dictionary<DBTable, DBRowCollection>();


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
            DBContextCommitInfo commitInfo = new DBContextCommitInfo();
            DBRow row = null;
            DbTransaction transaction = null;
            try
            {
                transaction = Connection.BeginTransaction();

                #region DELETE

                List<DBTable> emptyTables = new List<DBTable>();

                foreach (KeyValuePair<DBTable, DBRowCollection> tableRowsItem in tableRows)
                {
                    DBTable table = tableRowsItem.Key;
                    DBRowCollection rowCollection = tableRowsItem.Value;

                    for (int rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
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

                foreach (DBTable table in emptyTables)
                {
                    tableRows.Remove(table);
                }

                #endregion

                #region INSERT

                // список всех Insert-строк
                List<InsertRowContainer> rowContainerList = new List<InsertRowContainer>();
                // список всех временных ID, с привязкой к Insert-строкам
                Dictionary<DBTempId, List<TempIdContainer>> idContainerList = new Dictionary<DBTempId, List<TempIdContainer>>();

                #region Формирование списков

                foreach (KeyValuePair<DBTable, DBRowCollection> item in tableRows)
                {
                    DBTable table = item.Key;
                    DBRowCollection rowCollection = item.Value;

                    for (int rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
                    {
                        row = rowCollection[rowIndex];

                        InsertRowContainer rowContainer = null;
                        if (row.State == DataRowState.Added)
                        {
                            rowContainer = new InsertRowContainer(row);
                            rowContainerList.Add(rowContainer);
                        }

                        for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                        {
                            if (row[columnIndex] is DBTempId tempID)
                            {
                                TempIdContainer idContainer = new TempIdContainer(row, columnIndex, rowContainer);
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

                bool insertError = false;
                for (int i = 0; i < rowContainerList.Count; i++)
                {
                    InsertRowContainer rowContainer = rowContainerList[i];
                    row = rowContainer.Row;

                    if (rowContainer.TempIdCount == 1)
                    {
                        DBTempId tempId = (DBTempId)row.PrimaryKeyValue;
                        object rowId = ExecuteInsertCommand(row, transaction);
                        commitInfo.InsertedRowsCount++;

                        // замена временных ID на присвоенные
                        List<TempIdContainer> idContainer = idContainerList[tempId];
                        for (int j = 0; j < idContainer.Count; j++)
                        {
                            TempIdContainer list = idContainer[j];
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
                            throw DBExceptionFactory.DbSaveWrongRelationsException();
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

                foreach (KeyValuePair<DBTable, DBRowCollection> tableRowsItem in tableRows)
                {
                    DBTable table = tableRowsItem.Key;
                    DBRowCollection rowCollection = tableRowsItem.Value;

                    for (int rowIndex = 0; rowIndex < rowCollection.Count; rowIndex++)
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
                throw DBExceptionFactory.DbSaveException(row, ex);
            }
            return commitInfo;
        }


        public DBRow NewRow(string tableName, bool addToContext)
        {
            DBTable table = Provider.Tables[tableName];
            DBRow row = table.CreateRow();
            if (addToContext)
            {
                AddRow(row);
            }
            return row;
        }

        public TRow NewRow<TRow>(bool addToContext) where TRow : DBOrmRow
        {
            string tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            DBRow row = NewRow(tableName, addToContext);
            return DBInternal.CreateOrmRow<TRow>(row);
        }


        public int AddRow(DBRow row)
        {
            if (row.Table.Name == null)
            {
                throw DBExceptionFactory.ProcessRowException();
            }

            if (row.State == DataRowState.Deleted && row.PrimaryKeyValueIsTemporary)
            {
                return 0;
            }

            if (!tableRows.TryGetValue(row.Table, out DBRowCollection rowCollection))
            {
                rowCollection = new DBRowCollection();
                tableRows.Add(row.Table, rowCollection);
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
            int count = 0;
            foreach (DBRow row in collection)
            {
                count += AddRow(row);
            }
            return count;
        }

        public int AddRows<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            int count = 0;
            foreach (TRow row in collection)
            {
                count += AddRow(row);
            }
            return count;
        }


        public void Clear()
        {
            foreach (DBRowCollection rowCollection in tableRows.Values)
            {
                foreach (DBRow row in rowCollection)
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                }
            }
            tableRows.Clear();
        }

        public void Clear<TRow>()
        {
            string tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            Clear(tableName);
        }

        public void Clear(string tableName)
        {
            DBTable table = Provider.Tables[tableName];
            if (tableRows.TryGetValue(table, out DBRowCollection rowCollection))
            {
                foreach (DBRow row in rowCollection)
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                }
                tableRows.Remove(table);
            }
        }

        public void Clear(DBRow row)
        {
            if (row.Table.Name == null)
            {
                throw DBExceptionFactory.ProcessRowException();
            }

            if (tableRows.TryGetValue(row.Table, out DBRowCollection rowCollection))
            {
                if (rowCollection.Remove(row))
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                    if (rowCollection.Count == 0)
                    {
                        tableRows.Remove(row.Table);
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
            foreach (DBRow row in collection)
            {
                Clear(row);
            }
        }

        public void Clear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            foreach (TRow row in collection)
            {
                Clear(row);
            }
        }


        public void CommitAndClear()
        {
            try
            {
                Commit();
            }
            finally
            {
                Clear();
            }
        }

        public void CommitAndClear(DBRow row)
        {
            try
            {
                AddRow(row);
                Commit();
            }
            finally
            {
                Clear(row);
            }
        }

        public void CommitAndClear<TRow>(TRow row) where TRow : DBOrmRow
        {
            try
            {
                AddRow(row);
                Commit();
            }
            finally
            {
                Clear(row);
            }
        }

        public void CommitAndClear(IEnumerable<DBRow> collection)
        {
            try
            {
                AddRows(collection);
                Commit();
            }
            finally
            {
                Clear(collection);
            }
        }

        public void CommitAndClear<TRow>(IEnumerable<TRow> collection) where TRow : DBOrmRow
        {
            try
            {
                AddRows(collection);
                Commit();
            }
            finally
            {
                Clear(collection);
            }
        }


        private DBQuery CreateQuery(string tableName, StatementType statementType)
        {
            DBTable table = Provider.Tables[tableName];
            DBQuery query = new DBQuery(table, this, statementType);
            return query;
        }

        private DBQuery<TRow> CreateQuery<TRow>(StatementType statementType) where TRow : DBOrmRow
        {
            string tableName = DBInternal.GetTableNameFromAttribute(typeof(TRow));
            DBTable table = Provider.Tables[tableName];
            DBQuery<TRow> query = new DBQuery<TRow>(table, this, statementType);
            return query;
        }

        private object ExecuteInsertCommand(DBRow row, DbTransaction dbTransaction)
        {
            using (DbCommand dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Insert);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (!row.Table.Columns[i].IsPrimary)
                    {
                        DbParameter dbParameter = Provider.CreateParameter(string.Concat("@p", index), row[i]);
                        dbCommand.Parameters.Add(dbParameter);
                        index++;
                    }
                }
                return Provider.ExecuteInsertCommand(dbCommand);
            }
        }

        private int ExecuteUpdateCommand(DBRow row, DbTransaction dbTransaction)
        {
            using (DbCommand dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Update);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                    {
                        DbParameter dbParameter = Provider.CreateParameter("@id", row[i]);
                        dbCommand.Parameters.Add(dbParameter);
                    }
                    else
                    {
                        DbParameter dbParameter = Provider.CreateParameter(string.Concat("@p", index), row[i]);
                        dbCommand.Parameters.Add(dbParameter);
                        index++;
                    }
                }

                return dbCommand.ExecuteNonQuery();
            }
        }

        private int ExecuteDeleteCommand(DBRow row, DbTransaction dbTransaction)
        {
            using (DbCommand dbCommand = Connection.CreateCommand())
            {
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandText = Provider.GetDefaultSqlQuery(row.Table, StatementType.Delete);

                DbParameter dbParameter = Provider.CreateParameter("@id", row.PrimaryKeyValue);
                dbCommand.Parameters.Add(dbParameter);

                return dbCommand.ExecuteNonQuery();
            }
        }


        private class InsertRowContainer
        {
            public InsertRowContainer(DBRow row)
            {
                Row = row;
            }

            public DBRow Row;
            public int TempIdCount;
        }

        private class TempIdContainer
        {
            public TempIdContainer(DBRow row, int columnIndex, InsertRowContainer insertRowContainer)
            {
                Row = row;
                ColumnIndex = columnIndex;
                InsertRowContainer = insertRowContainer;
            }

            public DBRow Row;
            public int ColumnIndex;
            public InsertRowContainer InsertRowContainer;
        }
    }
}
