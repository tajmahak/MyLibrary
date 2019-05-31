using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Считывает поток строк последовательного доступа из источника данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class DBReader<T> : IEnumerable<T>, IEnumerator<T>
    {
        public DBReader(DbConnection connection, DBModelBase model, DBQueryBase query)
        {
            _model = model;
            _command = model.CompileCommand(connection, query);
            _reader = _command.ExecuteReader();
            _table = (!query.IsView) ? query.Table : GenerateTable();
        }
        public void Dispose()
        {
            _reader.Dispose();
            _command.Dispose();
        }

        public bool MoveNext()
        {
            if (_reader.Read())
            {
                var row = new DBRow(_table);
                _reader.GetValues(row.Values);
                row.State = DataRowState.Unchanged;
                Current = DBInternal.PackRow<T>(row);
                return true;
            }
            return false;
        }
        public T Current { get; private set; }
        public List<T> ToList()
        {
            var list = new List<T>();
            foreach (var row in this)
            {
                list.Add(row);
            }
            return list;
        }
        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        #region Скрытые сущности

        private DBTable GenerateTable()
        {
            var table = new DBTable(_model);
            using (var schema = _reader.GetSchemaTable())
            {
                int index = 0;
                foreach (DataRow schemaRow in schema.Rows)
                {
                    var column = new DBColumn(table);
                    column.OrderIndex = index++;
                    column.Name = (string)schemaRow["ColumnName"];

                    var schemaBaseTableName = (string)schemaRow["BaseTableName"];
                    string columnName = string.IsNullOrEmpty(schemaBaseTableName) ?
                        column.Name : string.Concat(schemaBaseTableName, '.', column.Name);
                    if (_model.TryGetColumn(columnName) != null)
                    {
                        column.Name = string.Concat(schemaBaseTableName, '.', column.Name);
                    }
                    table.AddColumn(column);
                }
            }
            return table;
        }

        private DbCommand _command;
        private DbDataReader _reader;
        private DBTable _table;
        private DBModelBase _model;

        #region Сущности интерфейсов IEnumerable, IEnumerator

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
        public void Reset()
        {
            throw new NotSupportedException();
        }

        #endregion

        #endregion
    }
}
