using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using MyLibrary.Data;

namespace MyLibrary.DataBase
{
    public class DBContext : IDisposable
    {
        public DBContext(DBModelBase model, DbConnection connection)
        {
            if (!model.IsInitialized)
                model.Initialize(connection);

            Model = model;
            Connection = connection;
            AutoCommit = true;

            _rowCollectionList = new List<DBRow>[model.Tables.Length];
            _rowCollectionDict = new Dictionary<DBTable, List<DBRow>>(model.Tables.Length);
            for (int i = 0; i < model.Tables.Length; i++)
            {
                var table = model.Tables[i];
                var rowCollection = new List<DBRow>();
                _rowCollectionList[i] = rowCollection;
                _rowCollectionDict.Add(table, rowCollection);
            }
        }

        public DBModelBase Model { get; private set; }
        public DbConnection Connection { get; set; }
        public bool AutoCommit { get; set; }

        public DBCommand Command(string tableName)
        {
            return Model.CreateDBCommand(tableName);
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
        public void Execute(DBCommand cmd)
        {
            if (cmd.CommandType == DBCommandTypeEnum.Select)
                throw DBInternal.SqlExecuteException();

            OpenTransaction();
            try
            {
                using (var command = Model.BuildCommand(Connection, cmd))
                {
                    command.Transaction = _transaction;
                    command.ExecuteNonQuery();
                }
                if (AutoCommit)
                    CommitTransaction();
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

                for (int i = 0; i < _rowCollectionList.Length; i++)
                {
                    var rowCollection = _rowCollectionList[i];
                    if (rowCollection.Count == 0)
                        continue;

                    var table = rowCollection[0].Table;
                    for (int j = 0; j < rowCollection.Count; j++)
                    {
                        row = rowCollection[j];
                        if (row.State == DataRowState.Deleted)
                        {
                            if (!(row[table.PrimaryKeyIndex] is Guid))
                                ExecuteDeleteCommand(row);
                        }
                    }
                    rowCollection.RemoveAll(x => x.State == DataRowState.Deleted);
                }

                #endregion
                #region INSERT

                var insertRows = new List<InsertRowContainer>();
                var tempIDs = new Dictionary<Guid, List<InsertRowContainer>>();
                #region Формирование списков

                for (int i = 0; i < _rowCollectionList.Length; i++)
                {
                    var rowCollection = _rowCollectionList[i];
                    if (rowCollection.Count == 0)
                        continue;

                    var table = rowCollection[0].Table;
                    for (int j = 0; j < rowCollection.Count; j++)
                    {
                        row = rowCollection[j];
                        var mainContainer = new InsertRowContainer(row, 0);
                        for (int k = 0; k < table.Columns.Length; k++)
                        {
                            var value = row[k];
                            if (value is Guid)
                            {
                                mainContainer.Value++;

                                var tempID = (Guid)value;
                                var idContainer = new InsertRowContainer(row, k);
                                if (row.State == DataRowState.Added)
                                    idContainer.MainContainer = mainContainer;

                                if (!tempIDs.ContainsKey(tempID))
                                {
                                    var list = new List<InsertRowContainer>();
                                    list.Add(idContainer);
                                    tempIDs.Add(tempID, list);
                                }
                                else tempIDs[tempID].Add(idContainer);
                            }
                        }
                        if (row.State == DataRowState.Added)
                            insertRows.Add(mainContainer);
                    }
                }
                insertRows.Sort((x, y) => x.Value.CompareTo(y.Value));

                #endregion

                bool saveError = false;
                for (int i = 0; i < insertRows.Count; i++)
                {
                    var rowContainer = insertRows[i];
                    row = rowContainer.Row;
                    if (row.State != DataRowState.Added)
                        continue;
                    if (rowContainer.Value == 1)
                    {
                        Guid tempID = (Guid)row[row.Table.PrimaryKeyIndex];
                        object dbID = ExecuteInsertCommand(row);
                        #region Замена временных Id на присвоенные

                        var list = tempIDs[tempID];
                        for (int j = 0; j < list.Count; j++)
                        {
                            var idContainer = list[j];
                            idContainer.Row[idContainer.Value] = dbID;
                            if (idContainer.MainContainer != null)
                                idContainer.MainContainer.Value--;
                        }

                        #endregion
                        row.State = DataRowState.Unchanged;
                        saveError = false;
                    }
                    else
                    {
                        if (saveError)
                            throw DBInternal.DbSaveWrongRelationsException();
                        insertRows.Sort((x, y) => x.Value.CompareTo(y.Value));
                        i = -1;
                        saveError = true;
                    }
                }
                insertRows.Clear();
                tempIDs.Clear();

                #endregion
                #region UPDATE

                for (int i = 0; i < _rowCollectionList.Length; i++)
                {
                    var rowCollection = _rowCollectionList[i];
                    if (rowCollection.Count == 0)
                        continue;
                    var table = rowCollection[0].Table;

                    for (int j = 0; j < rowCollection.Count; j++)
                    {
                        row = rowCollection[j];
                        if (row.State == DataRowState.Modified)
                        {
                            ExecuteUpdateCommand(row);
                            row.State = DataRowState.Unchanged;
                        }
                    }
                }

                #endregion

                if (AutoCommit)
                    CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                Clear();
                throw DBInternal.DbSaveException(row, ex);
            }
        }
        public void Dispose()
        {
            Clear();
        }

        #region Работа с коллекцией

        public T New<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = new DBRow(table);
            row.InitializeValues();
            Add(row);
            return DBInternal.PackRow<T>(row);
        }
        public T New<T>()
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            return New<T>(tableName);
        }

        public bool Add<T>(T row)
        {
            if (row is IEnumerable)
                return AddCollection((IEnumerable)row);

            var dbRow = DBInternal.UnpackRow(row);
            if (dbRow.Table.Name == null)
                throw DBInternal.ProcessRowException();

            if (dbRow.State == DataRowState.Deleted)
                if (dbRow[dbRow.Table.PrimaryKeyIndex] is Guid)
                    return false;

            var rowCollection = _rowCollectionDict[dbRow.Table];
            if (!rowCollection.Contains(dbRow))
            {
                lock (_rowCollectionDict)
                    rowCollection.Add(dbRow);
            }

            if (dbRow.State == DataRowState.Detached)
                dbRow.State = DataRowState.Added;

            return true;
        }
        public void Delete<T>(T row)
        {
            var dbRow = DBInternal.UnpackRow(row);
            if (dbRow.Table.Name == null)
                throw DBInternal.ProcessRowException();

            dbRow.Delete();

            if (dbRow[dbRow.Table.PrimaryKeyIndex] is Guid)
            {
                lock (_rowCollectionDict)
                    _rowCollectionDict[dbRow.Table].Remove(dbRow);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _rowCollectionList.Length; i++)
            {
                var rowCollection = _rowCollectionList[i];
                rowCollection.ForEach(row =>
                {
                    if (row.State == DataRowState.Added)
                        row.State = DataRowState.Detached;
                });

                lock (_rowCollectionDict)
                    rowCollection.Clear();
            }
        }
        public void Clear(string tableName)
        {
            var table = Model.GetTable(tableName);
            _rowCollectionDict[table].Clear();
        }
        public void Clear<T>(T row)
        {
            if (row is IEnumerable)
            {
                ClearCollection((IEnumerable)row);
                return;
            }

            var dbRow = DBInternal.UnpackRow(row);
            if (dbRow.Table.Name == null)
                throw DBInternal.ProcessRowException();

            if (dbRow.State == DataRowState.Added)
                dbRow.State = DataRowState.Detached;

            lock (_rowCollectionDict)
                _rowCollectionDict[dbRow.Table].Remove(dbRow);
        }

        public List<T> GetSetRows<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var rowList = _rowCollectionDict[table];

            var list = new List<T>(rowList.Count);
            foreach (var row in rowList)
            {
                list.Add(DBInternal.PackRow<T>(row));
            }
            return list;
        }
        public List<T> GetSetRows<T>()
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            return GetSetRows<T>(tableName);
        }
        public List<DBRow> GetSetRows(string tableName)
        {
            return GetSetRows<DBRow>(tableName);
        }

        #endregion
        #region Работа с данными

        public T Get<T>(DBCommand cmd)
        {
            cmd.First(1);
            foreach (var row in Select<T>(cmd))
                return row;
            return default(T);
        }
        public T Get<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Get<T>(cmd);
        }

        public T GetOrNew<T>(DBCommand cmd)
        {
            var row = Get<T>(cmd);

            if (row != null)
            {
                Add(row);
                return row;
            }

            return New<T>(cmd.Table.Name);
        }
        public T GetOrNew<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            var row = GetOrNew(cmd);

            // установка значений в строку согласно аргументов
            if (row.Values[row.Table.PrimaryKeyIndex] is Guid)
            {
                for (int i = 0; i < columnNameValuePair.Length; i += 2)
                {
                    string columnName = (string)columnNameValuePair[i];
                    object value = columnNameValuePair[i + 1];
                    row[columnName] = value;
                }
            }

            return DBInternal.PackRow<T>(row);
        }

        public DBReader<T> Select<T>(DBCommand cmd)
        {
            if (cmd.CommandType != DBCommandTypeEnum.Select && cmd.CommandType != DBCommandTypeEnum.Sql)
                throw DBInternal.SqlExecuteException();

            return new DBReader<T>(Connection, Model, cmd);
        }
        public DBReader<T> Select<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Select<T>(cmd);
        }

        public T GetValue<T>(DBCommand cmd)
        {
            if (cmd.CommandType == DBCommandTypeEnum.Select)
                cmd.First(1);

            using (var command = Model.BuildCommand(Connection, cmd))
            {
                var value = command.ExecuteScalar();
                return Format.Convert<T>(value);
            }
        }
        public T GetValue<T>(string columnName, params object[] columnNameValuePair)
        {
            var tableName = columnName.Split('.')[0];
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            cmd.Select(columnName);
            return GetValue<T>(cmd);
        }

        public bool Exists(DBCommand cmd)
        {
            var row = Get(cmd);
            return (row != null);
        }
        public bool Exists(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Exists(cmd);
        }

        #endregion
        #region Работа с данными типа <DBRow>

        public DBRow New(string tableName)
        {
            return New<DBRow>(tableName);
        }

        public DBRow Get(DBCommand cmd)
        {
            return Get<DBRow>(cmd);
        }
        public DBRow Get(string tableName, params object[] columnNameValuePair)
        {
            return Get<DBRow>(tableName, columnNameValuePair);
        }

        public DBRow GetOrNew(DBCommand cmd)
        {
            return GetOrNew<DBRow>(cmd);
        }
        public DBRow GetOrNew(string tableName, params object[] columnNameValuePair)
        {
            return GetOrNew<DBRow>(tableName, columnNameValuePair);
        }

        public DBReader<DBRow> Select(DBCommand cmd)
        {
            return Select<DBRow>(cmd);
        }
        public DBReader<DBRow> Select(string tableName, params object[] columnNameValuePair)
        {
            return Select<DBRow>(tableName, columnNameValuePair);
        }

        #endregion

        #region Закрытые элементы

        private DbTransaction _transaction;
        private List<DBRow>[] _rowCollectionList;
        private Dictionary<DBTable, List<DBRow>> _rowCollectionDict;

        private bool AddCollection(IEnumerable collection)
        {
            bool added = true;
            foreach (var row in collection)
            {
                if (!Add(row))
                {
                    added = false;
                }
            }
            return added;
        }
        private void ClearCollection(IEnumerable collection)
        {
            foreach (var row in collection)
                Clear(row);
        }

        private void OpenTransaction()
        {
            if (_transaction == null)
                _transaction = Connection.BeginTransaction();
        }
        private void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }
        private object ExecuteInsertCommand(DBRow row)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = Model.DefaultInsertCommandsDict[row.Table];

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Length; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                        continue;
                    Model.AddParameter(cmd, "@p" + index, row[i]);
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
                cmd.CommandText = Model.DefaultUpdateCommandsDict[row.Table];

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Length; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                    {
                        Model.AddParameter(cmd, "@id", row[i]);
                        continue;
                    }
                    Model.AddParameter(cmd, "@p" + index, row[i]);
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
                cmd.CommandText = Model.DefaultDeleteCommandsDict[row.Table];
                Model.AddParameter(cmd, "@id", row[row.Table.PrimaryKeyIndex]);
                cmd.ExecuteNonQuery();
            }
        }

        private DBCommand CreateSelectCommand(string tableName, params object[] columnNameValuePair)
        {
            if (columnNameValuePair.Length % 2 != 0)
                throw DBInternal.ParameterValuePairException();

            var cmd = Command(tableName);
            for (int i = 0; i < columnNameValuePair.Length; i += 2)
            {
                string columnName = (string)columnNameValuePair[i];
                object value = columnNameValuePair[i + 1];
                cmd.Where(columnName, value);
            }
            return cmd;
        }

        private class InsertRowContainer
        {
            public DBRow Row;
            public int Value;
            public InsertRowContainer MainContainer;

            public InsertRowContainer(DBRow row, int value)
            {
                Row = row;
                Value = value;
            }
            public override string ToString()
            {
                return string.Format("{0} - {1}", Value, Row);
            }
        }

        #endregion
    }
}
