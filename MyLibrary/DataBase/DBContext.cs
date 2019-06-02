using MyLibrary.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace MyLibrary.DataBase
{
    public class DBContext : IDisposable
    {
        /// <summary>
        /// Представляет механизм для работы с БД.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="connection"></param>
        public DBContext(DBModelBase model, DbConnection connection)
        {
            Model = model;
            Connection = connection;

            if (!Model.IsInitialized)
            {
                Model.Initialize(Connection);
            }
        }

        public DBModelBase Model { get; private set; }
        public DbConnection Connection { get; set; }
        public bool AutoCommit { get; set; } = true;

        /// <summary>
        /// Создание нового запроса <see cref="DBQuery"/>.
        /// </summary>
        /// <param name="tableName">Имя таблицы базы данных, указанной в запросе.</param>
        /// <returns></returns>
        public DBQuery Query(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table);
            return query;
        }
        /// <summary>
        /// Создание нового запроса <see cref="DBQuery{T}"/>.
        /// </summary>
        /// <typeparam name="T">Тип данных для таблицы.</typeparam>
        /// <returns></returns>
        public DBQuery<T> Query<T>() where T : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<T>(table);
            return query;
        }
        /// <summary>
        /// Фиксирование транзакции (используется при использовании <see cref="AutoCommit"/>).
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
        /// Выполнение запроса <see cref="DBQuery"/>.
        /// </summary>
        /// <param name="query"></param>
        public void Execute(DBQueryBase query)
        {
            if (query.Type == DBQueryType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }
            try
            {
                OpenTransaction();
                using (var command = Model.CreateCommand(Connection, query))
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

                foreach (var tableRowsItem in _tableRows)
                {
                    var table = tableRowsItem.Key;
                    var rowList = tableRowsItem.Value;

                    for (int rowIndex = 0; rowIndex < rowList.Count; rowIndex++)
                    {
                        row = rowList[rowIndex];
                        if (row.State == DataRowState.Deleted)
                        {
                            if (!(row[table.PrimaryKeyColumn.OrderIndex] is Guid))
                            {
                                ExecuteDeleteCommand(row);
                            }
                        }
                    }
                    rowList.RemoveAll(x => x.State == DataRowState.Deleted);
                }

                #endregion
                #region INSERT

                // список всех Insert-строк
                var rowContainerList = new List<InsertRowContainer>();
                // список всех временных ID, с привязкой к Insert-строкам
                var idContainerList = new Dictionary<Guid, List<TempIdContainer>>();

                #region Формирование списков

                foreach (var item in _tableRows)
                {
                    var table = item.Key;
                    var rowList = item.Value;

                    for (int rowIndex = 0; rowIndex < rowList.Count; rowIndex++)
                    {
                        row = rowList[rowIndex];

                        InsertRowContainer rowContainer = null;
                        if (row.State == DataRowState.Added)
                        {
                            rowContainer = new InsertRowContainer(row);
                            rowContainerList.Add(rowContainer);
                        }

                        for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                        {
                            var value = row[columnIndex];
                            if (value is Guid tempID)
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

                bool saveError = false;
                for (int i = 0; i < rowContainerList.Count; i++)
                {
                    var rowContainer = rowContainerList[i];
                    row = rowContainer.Row;

                    if (rowContainer.TempIdCount == 1)
                    {
                        var tempID = (Guid)row[row.Table.PrimaryKeyColumn.OrderIndex];
                        var newID = ExecuteInsertCommand(row);

                        #region Замена временных Id на присвоенные

                        var idContainer = idContainerList[tempID];
                        for (int j = 0; j < idContainer.Count; j++)
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
                    var rowList = tableRowsItem.Value;

                    for (int rowIndex = 0; rowIndex < rowList.Count; rowIndex++)
                    {
                        row = rowList[rowIndex];
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
            CommitTransaction();
            Clear();
        }

        #region Работа с данными

        /// <summary>
        /// Создание новой строки и помещение её в данный экземпляр <see cref="DBContext"/>
        /// </summary>
        /// <typeparam name="TTable">Тип строки формата <see cref="DBRow"/> или <see cref="Orm.DBOrmTableBase"/></typeparam>
        /// <returns></returns>
        public TTable New<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            return New<TTable>(tableName);
        }
        /// <summary>
        /// Создание новой строки и помещение её в текущий экземпляр <see cref="DBContext"/>
        /// </summary>
        /// <param name="tableName">Имя таблицы базы данных, для которой будет создана строка</param>
        /// <returns></returns>
        public DBRow New(string tableName)
        {
            return New<DBRow>(tableName);
        }

        public T Get<T>(DBQueryBase query)
        {
            query.AddBlock(DBQueryStructureType.Limit, 1);
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
        public TTable Get<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.First();
            query.Where(whereExpression);
            return Get<TTable>(query);
        }
        public DBRow Get(DBQueryBase query)
        {
            return Get<DBRow>(query);
        }
        public DBRow Get(string tableName, params object[] columnNameValuePair)
        {
            return Get<DBRow>(tableName, columnNameValuePair);
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

            // установка значений в строку согласно аргументам
            if (row.Values[row.Table.PrimaryKeyColumn.OrderIndex] is Guid)
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
        public TTable GetOrNew<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.First();
            query.Where(whereExpression);
            return GetOrNew<TTable>(query);
        }
        public DBRow GetOrNew(DBQueryBase query)
        {
            return GetOrNew<DBRow>(query);
        }
        public DBRow GetOrNew(string tableName, params object[] columnNameValuePair)
        {
            return GetOrNew<DBRow>(tableName, columnNameValuePair);
        }

        public DBReader<T> Select<T>(DBQueryBase query)
        {
            if (query.Type != DBQueryType.Select)
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
        public DBReader<TTable> Select<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.First();
            query.Where(whereExpression);
            return Select<TTable>(query);
        }
        public DBReader<DBRow> Select(DBQueryBase query)
        {
            return Select<DBRow>(query);
        }
        public DBReader<DBRow> Select(string tableName, params object[] columnNameValuePair)
        {
            return Select<DBRow>(tableName, columnNameValuePair);
        }

        public TType GetValue<TType>(DBQueryBase query)
        {
            if (query.Type == DBQueryType.Select) // могут быть команды с блоками RETURNING и т.п.
            {
                query.AddBlock(DBQueryStructureType.Limit, 1);
            }

            using (var command = Model.CreateCommand(Connection, query))
            {
                var value = command.ExecuteScalar();
                return Format.Convert<TType>(value);
            }
        }
        public TType GetValue<TType, TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.First();
            query.Where(whereExpression);
            return GetValue<TType>(query);
        }
        public TType GetValue<TType>(string columnName, params object[] columnNameValuePair)
        {
            var tableName = columnName.Split('.')[0];
            var cmd = CreateSelectCommand(tableName, columnNameValuePair);
            cmd.Select(columnName);
            return GetValue<TType>(cmd);
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

        public int Add<T>(T row)
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
                if (dbRow[dbRow.Table.PrimaryKeyColumn.OrderIndex] is Guid)
                {
                    return 0;
                }
            }

            if (!_tableRows.TryGetValue(dbRow.Table, out var rowList))
            {
                rowList = new List<DBRow>();
                _tableRows.Add(dbRow.Table, rowList);
            }

            if (!rowList.Contains(dbRow))
            {
                rowList.Add(dbRow);
            }

            if (dbRow.State == DataRowState.Detached)
            {
                dbRow.State = DataRowState.Added;
            }

            return 1;
        }

        public void Clear()
        {
            foreach (var rowList in _tableRows.Values)
            {
                rowList.ForEach(row =>
                {
                    if (row.State == DataRowState.Added)
                    {
                        row.State = DataRowState.Detached;
                    }
                });
                rowList.Clear();
            }
        }
        public void Clear(string tableName)
        {
            var table = Model.GetTable(tableName);
            if (_tableRows.TryGetValue(table, out var rowList))
            {
                rowList.Clear();
            }
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

            if (_tableRows.TryGetValue(dbRow.Table, out var rowList))
            {
                rowList.Remove(dbRow);
            }
        }

        public List<T> GetSetRows<T>() where T : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(T));
            return GetSetRows<T>(tableName);
        }
        public List<DBRow> GetSetRows(string tableName)
        {
            return GetSetRows<DBRow>(tableName);
        }

        #endregion

        private T New<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = table.CreateRow();
            Add(row);
            return DBInternal.PackRow<T>(row);
        }
        private List<T> GetSetRows<T>(string tableName)
        {
            var table = Model.GetTable(tableName);

            if (_tableRows.TryGetValue(table, out var rowList))
            {
                var list = new List<T>(rowList.Count);
                foreach (var row in rowList)
                {
                    list.Add(DBInternal.PackRow<T>(row));
                }
                return list;
            }
            else
            {
                return new List<T>(0);
            }
        }
        private int AddCollection(IEnumerable collection)
        {
            int count = 0;
            foreach (var row in collection)
            {
                count += Add(row);
            }
            return count;
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryType.Insert);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryType.Update);

                int index = 0;
                for (int i = 0; i < row.Table.Columns.Count; i++)
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
                cmd.CommandText = Model.GetDefaultSqlQuery(row.Table, DBQueryType.Delete);
                Model.AddCommandParameter(cmd, "@id", row[row.Table.PrimaryKeyColumn.OrderIndex]);

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

        private DbTransaction _transaction;
        private Dictionary<DBTable, List<DBRow>> _tableRows = new Dictionary<DBTable, List<DBRow>>();

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
