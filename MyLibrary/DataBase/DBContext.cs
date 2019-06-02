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
    public class DBContext : IDisposable
    {
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

        public DBQuery Query(string tableName)
        {
            var table = Model.GetTable(tableName);
            var query = new DBQuery(table);
            return query;
        }
        public DBQuery<TTable> Query<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var table = Model.GetTable(tableName);
            var query = new DBQuery<TTable>(table);
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
        public void Dispose()
        {
            CommitTransaction();
            Clear();
        }

        #region Работа с данными

        public TTable New<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            return NewInternal<TTable>(tableName);
        }
        public DBRow New(string tableName)
        {
            return NewInternal<DBRow>(tableName);
        }

        public TTable Get<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return GetInternal<TTable>(query);
        }
        public TTable Get<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return GetInternal<TTable>(query);
        }
        public TTable Get<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return GetInternal<TTable>(query);
        }
        public DBRow Get(DBQueryBase query)
        {
            return GetInternal<DBRow>(query);
        }
        public DBRow Get(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return GetInternal<DBRow>(query);
        }

        public TTable GetOrNew<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return GetOrNewInternal<TTable>(query);
        }
        public TTable GetOrNew<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return GetOrNewInternal<TTable>(query);
        }
        public TTable GetOrNew<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var item = GetOrNewInternal<TTable>(query);

            if (item.Row.State == DataRowState.Added)
            {
                // установка значений в строку согласно аргументам
                for (int i = 0; i < columnConditionPair.Length; i += 2)
                {
                    string columnName = (string)columnConditionPair[i];
                    object value = columnConditionPair[i + 1];
                    item.Row[columnName] = value;
                }
            }

            return item;
        }
        public DBRow GetOrNew(DBQueryBase query)
        {
            return GetOrNewInternal<DBRow>(query);
        }
        public DBRow GetOrNew(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            var row = GetOrNewInternal<DBRow>(query);

            if (row.State == DataRowState.Added)
            {
                // установка значений в строку согласно аргументам
                for (int i = 0; i < columnConditionPair.Length; i += 2)
                {
                    string columnName = (string)columnConditionPair[i];
                    object value = columnConditionPair[i + 1];
                    row[columnName] = value;
                }
            }

            return row;
        }

        public DBReader<TTable> Select<TTable>(DBQueryBase query) where TTable : DBOrmTableBase
        {
            return SelectInternal<TTable>(query);
        }
        public DBReader<TTable> Select<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return SelectInternal<TTable>(query);
        }
        public DBReader<TTable> Select<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return SelectInternal<TTable>(query);
        }
        public DBReader<DBRow> Select(DBQueryBase query)
        {
            return SelectInternal<DBRow>(query);
        }
        public DBReader<DBRow> Select(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return SelectInternal<DBRow>(query);
        }

        public TType GetValue<TType>(DBQueryBase query)
        {
            return GetValueInternal<TType>(query);
        }
        public TType GetValue<TType, TTable>(string columnName, Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Select(columnName);
            query.Where(whereExpression);
            return GetValueInternal<TType>(query);
        }
        public TType GetValue<TType>(string columnName, params object[] columnConditionPair)
        {
            var tableName = columnName.Split('.')[0];
            var query = CreateSelectQuery(tableName, columnConditionPair);
            query.Select(columnName);
            return GetValueInternal<TType>(query);
        }

        public bool Exists(DBQueryBase query)
        {
            return ExistsInternal(query);
        }
        public bool Exists<TTable>(Expression<Func<TTable, bool>> whereExpression) where TTable : DBOrmTableBase
        {
            var query = Query<TTable>();
            query.Where(whereExpression);
            return ExistsInternal(query);
        }
        public bool Exists<TTable>(params object[] columnConditionPair) where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ExistsInternal(query);
        }
        public bool Exists(string tableName, params object[] columnConditionPair)
        {
            var query = CreateSelectQuery(tableName, columnConditionPair);
            return ExistsInternal(query);
        }

        public int Add<T>(T row)
        {
            if (row is IEnumerable)
            {
                return AddCollectionInternal((IEnumerable)row);
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
                ClearCollectionInternal((IEnumerable)row);
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

        public List<TTable> GetSetRows<TTable>() where TTable : DBOrmTableBase
        {
            var tableName = DBInternal.GetTableNameFromAttribute(typeof(TTable));
            return GetSetRowsInternal<TTable>(tableName);
        }
        public List<DBRow> GetSetRows(string tableName)
        {
            return GetSetRowsInternal<DBRow>(tableName);
        }

        #endregion

        private T NewInternal<T>(string tableName)
        {
            var table = Model.GetTable(tableName);
            var row = table.CreateRow();
            Add(row);
            return DBInternal.PackRow<T>(row);
        }
        private T GetInternal<T>(DBQueryBase query)
        {
            query.AddBlock(DBQueryStructureType.Limit, 1);
            foreach (var row in SelectInternal<T>(query))
            {
                return row;
            }
            return default(T);
        }
        private T GetOrNewInternal<T>(DBQueryBase query)
        {
            var row = GetInternal<T>(query);

            if (row != null)
            {
                Add(row);
                return row;
            }

            return NewInternal<T>(query.Table.Name);
        }
        private DBReader<T> SelectInternal<T>(DBQueryBase query)
        {
            if (query.Type != DBQueryType.Select)
            {
                throw DBInternal.SqlExecuteException();
            }

            return new DBReader<T>(Connection, Model, query);
        }
        private TType GetValueInternal<TType>(DBQueryBase query)
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
        private bool ExistsInternal(DBQueryBase query)
        {
            var row = GetInternal<DBRow>(query);
            return (row != null);
        }

        private List<T> GetSetRowsInternal<T>(string tableName)
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
        private int AddCollectionInternal(IEnumerable collection)
        {
            int count = 0;
            foreach (var row in collection)
            {
                count += Add(row);
            }
            return count;
        }
        private void ClearCollectionInternal(IEnumerable collection)
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
        private DBQuery CreateSelectQuery(string tableName, params object[] columnConditionPair)
        {
            if (columnConditionPair.Length % 2 != 0)
            {
                throw DBInternal.ParameterValuePairException();
            }

            var cmd = Query(tableName);
            for (int i = 0; i < columnConditionPair.Length; i += 2)
            {
                string columnName = (string)columnConditionPair[i];
                object value = columnConditionPair[i + 1];
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
