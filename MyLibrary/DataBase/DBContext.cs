using MyLibrary.Data;
using MyLibrary.DataBase.Orm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    public class DBContext : IDisposable
    {
        public DBContext(DBModelBase model, DbConnection connection)
        {
            if (!model.Initialized)
            {
                model.Initialize(connection);
            }

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

        /// <summary>
        /// Создание нового запроса <see cref="DBQuery"/>
        /// </summary>
        /// <param name="tableName">Имя таблицы базы данных, указанной в запросе</param>
        /// <returns></returns>
        public DBQuery Query(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table);
            return query;
        }
        /// <summary>
        /// Создание нового запроса <see cref="DBQuery{T}"/>
        /// </summary>
        /// <typeparam name="T">Тип данных для таблицы</typeparam>
        /// <returns></returns>
        public DBQuery<T> Query<T>() where T : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<T>(table);
            return query;
        }
        /// <summary>
        /// Фиксирование транзакции (используется при использовании <see cref="AutoCommit"/>)
        /// </summary>
        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }
        /// <summary>
        /// Выполнение запроса <see cref="DBQuery"/>
        /// </summary>
        /// <param name="query"></param>
        public void Execute(DBQueryBase query)
        {
            if (query.Type == DBQueryTypeEnum.Select)
            {
                throw DBInternal.SqlExecuteException();
            }
            try
            {
                OpenTransaction();
                using (var command = Model.CompileCommand(Connection, query))
                {
                    command.Transaction = _transaction;
                    command.ExecuteNonQuery();
                }
                if (AutoCommit)
                {
                    CommitTransaction();
                }
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }
        /// <summary>
        /// Сохраняет все изменения, внесенные в <see cref="DBContext"/> после его загрузки или после последнего вызова метода <see cref="Save"/>.
        /// </summary>
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
                    {
                        continue;
                    }

                    var table = rowCollection[0].Table;
                    for (int j = 0; j < rowCollection.Count; j++)
                    {
                        row = rowCollection[j];
                        if (row.State == DataRowState.Deleted)
                        {
                            if (!(row[table.PrimaryKeyColumn.Index] is Guid))
                            {
                                ExecuteDeleteCommand(row);
                            }
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
                    {
                        continue;
                    }

                    var table = rowCollection[0].Table;
                    for (int j = 0; j < rowCollection.Count; j++)
                    {
                        row = rowCollection[j];
                        var mainContainer = new InsertRowContainer(row, 0);
                        for (int k = 0; k < table.Columns.Count; k++)
                        {
                            var value = row[k];
                            if (value is Guid)
                            {
                                mainContainer.Value++;

                                var tempID = (Guid)value;
                                var idContainer = new InsertRowContainer(row, k);
                                if (row.State == DataRowState.Added)
                                {
                                    idContainer.ParentContainer = mainContainer;
                                }

                                if (!tempIDs.ContainsKey(tempID))
                                {
                                    var list = new List<InsertRowContainer>
                                    {
                                        idContainer
                                    };
                                    tempIDs.Add(tempID, list);
                                }
                                else
                                {
                                    tempIDs[tempID].Add(idContainer);
                                }
                            }
                        }
                        if (row.State == DataRowState.Added)
                        {
                            insertRows.Add(mainContainer);
                        }
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
                    {
                        continue;
                    }

                    if (rowContainer.Value == 1)
                    {
                        Guid tempID = (Guid)row[row.Table.PrimaryKeyColumn.Index];
                        object dbID = ExecuteInsertCommand(row);
                        #region Замена временных Id на присвоенные

                        var list = tempIDs[tempID];
                        for (int j = 0; j < list.Count; j++)
                        {
                            var idContainer = list[j];
                            idContainer.Row[idContainer.Value] = dbID;
                            if (idContainer.ParentContainer != null)
                            {
                                idContainer.ParentContainer.Value--;
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
                    {
                        continue;
                    }

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
        /// <summary>
        /// Освобождает все ресурсы, используемые объектом.
        /// </summary>
        public void Dispose()
        {
            Clear();
            CommitTransaction();
        }

        #region Работа с коллекцией

        /// <summary>
        /// Создание новой строки и помещение её в данный экземпляр <see cref="DBContext"/>
        /// </summary>
        /// <typeparam name="T">Тип строки формата <see cref="DBRow"/> или <see cref="Orm.DBOrmTableBase"/></typeparam>
        /// <param name="tableName">Имя таблицы базы данных, для которой будет создана строка</param>
        /// <returns></returns>
        public T New<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = new DBRow(table);
            row.InitializeValues();
            Add(row);
            return DBInternal.PackRow<T>(row);
        }
        /// <summary>
        /// Создание новой строки и помещение её в данный экземпляр <see cref="DBContext"/>
        /// </summary>
        /// <typeparam name="T">Тип строки формата <see cref="DBRow"/> или <see cref="Orm.DBOrmTableBase"/></typeparam>
        /// <returns></returns>
        public T New<T>()
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            return New<T>(tableName);
        }

        public bool Add<T>(T row)
        {
            if (row is IEnumerable)
            {
                return AddCollection((IEnumerable)row);
            }

            var dbRow = DBInternal.UnpackRow(row);
            if (dbRow.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            if (dbRow.State == DataRowState.Deleted)
            {
                if (dbRow[dbRow.Table.PrimaryKeyColumn.Index] is Guid)
                {
                    return false;
                }
            }

            var rowCollection = _rowCollectionDict[dbRow.Table];
            if (!rowCollection.Contains(dbRow))
            {
                lock (_rowCollectionDict)
                {
                    rowCollection.Add(dbRow);
                }
            }

            if (dbRow.State == DataRowState.Detached)
            {
                dbRow.State = DataRowState.Added;
            }

            return true;
        }
        public void Delete<T>(T row)
        {
            var dbRow = DBInternal.UnpackRow(row);
            if (dbRow.Table.Name == null)
            {
                throw DBInternal.ProcessRowException();
            }

            dbRow.Delete();

            if (dbRow[dbRow.Table.PrimaryKeyColumn.Index] is Guid)
            {
                lock (_rowCollectionDict)
                {
                    _rowCollectionDict[dbRow.Table].Remove(dbRow);
                }
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
                    {
                        row.State = DataRowState.Detached;
                    }
                });

                lock (_rowCollectionDict)
                {
                    rowCollection.Clear();
                }
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
            {
                throw DBInternal.ProcessRowException();
            }

            if (dbRow.State == DataRowState.Added)
            {
                dbRow.State = DataRowState.Detached;
            }

            lock (_rowCollectionDict)
            {
                _rowCollectionDict[dbRow.Table].Remove(dbRow);
            }
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

        public T Get<T>(DBQueryBase query)
        {
            query.AddItem(DBQueryStructureTypeEnum.First, 1);
            foreach (var row in Select<T>(query))
            {
                return row;
            }

            return default(T);
        }
        public T Get<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Get<T>(cmd);
        }

        public T GetOrNew<T>(DBQueryBase query)
        {
            var row = Get<T>(query);

            if (row != null)
            {
                Add(row);
                return row;
            }

            return New<T>(query.Table.Name);
        }
        public T GetOrNew<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            var row = GetOrNew(cmd);

            // установка значений в строку согласно аргументов
            if (row.Values[row.Table.PrimaryKeyColumn.Index] is Guid)
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

        public DBReader<T> Select<T>(DBQueryBase query)
        {
            if (query.Type != DBQueryTypeEnum.Select)
            {
                throw DBInternal.SqlExecuteException();
            }

            return new DBReader<T>(Connection, Model, query);
        }
        public DBReader<T> Select<T>(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Select<T>(cmd);
        }

        public T GetValue<T>(DBQueryBase query)
        {
            if (query.Type == DBQueryTypeEnum.Select)
            {
                query.AddItem(DBQueryStructureTypeEnum.First, 1);
            }

            using (var command = Model.CompileCommand(Connection, query))
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

        public bool Exists(DBQueryBase query)
        {
            var row = Get(query);
            return (row != null);
        }
        public bool Exists(string tableName, params object[] columnNameValuePair)
        {
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            return Exists(cmd);
        }

        #endregion
        #region Работа с данными типа <DBRow>

        /// <summary>
        /// Создание новой строки и помещение её в текущий экземпляр <see cref="DBContext"/>
        /// </summary>
        /// <param name="tableName">Имя таблицы базы данных, для которой будет создана строка</param>
        /// <returns></returns>
        public DBRow New(string tableName)
        {
            return New<DBRow>(tableName);
        }

        public DBRow Get(DBQueryBase query)
        {
            return Get<DBRow>(query);
        }
        public DBRow Get(string tableName, params object[] columnNameValuePair)
        {
            return Get<DBRow>(tableName, columnNameValuePair);
        }

        public DBRow GetOrNew(DBQueryBase query)
        {
            return GetOrNew<DBRow>(query);
        }
        public DBRow GetOrNew(string tableName, params object[] columnNameValuePair)
        {
            return GetOrNew<DBRow>(tableName, columnNameValuePair);
        }

        public DBReader<DBRow> Select(DBQueryBase query)
        {
            return Select<DBRow>(query);
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
            {
                Clear(row);
            }
        }

        private void OpenTransaction()
        {
            if (_transaction == null)
            {
                _transaction = Connection.BeginTransaction();
            }
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryTypeEnum.Insert);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                    {
                        continue;
                    }

                    Model.AddCommandParameter(cmd, string.Concat(Model.ParameterPrefix, 'p', index), row[i]);
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryTypeEnum.Update);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table.Columns[i].IsPrimary)
                    {
                        Model.AddCommandParameter(cmd, string.Concat(Model.ParameterPrefix, "id"), row[i]);
                        continue;
                    }
                    Model.AddCommandParameter(cmd, string.Concat(Model.ParameterPrefix, 'p', index), row[i]);
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryTypeEnum.Delete);
                Model.AddCommandParameter(cmd, string.Concat(Model.ParameterPrefix, "id"), row[row.Table.PrimaryKeyColumn.Index]);

                cmd.ExecuteNonQuery();
            }
        }

        private DBQuery CreateSelectCommand(string tableName, params object[] columnNameValuePair)
        {
            if (columnNameValuePair.Length % 2 != 0)
            {
                throw DBInternal.ParameterValuePairException();
            }

            var cmd = Query(tableName);
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
            public InsertRowContainer ParentContainer;

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
