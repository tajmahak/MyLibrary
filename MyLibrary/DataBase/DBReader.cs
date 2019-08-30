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
        public T Current { get; private set; }
        private readonly DbCommand _command;
        private readonly DbDataReader _reader;
        private readonly DBTable _table;
        private readonly DBModelBase _model;

        public DBReader(DbConnection connection, DBModelBase model, DBQueryBase query)
        {
            _model = model;
            _command = model.CreateCommand(connection, query);
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

        private DBTable GenerateTable()
        {
            var table = new DBTable(_model);
            using (var schema = _reader.GetSchemaTable())
            {
                var index = 0;
                foreach (DataRow schemaRow in schema.Rows)
                {
                    var column = new DBColumn(table);
                    column.OrderIndex = index++;
                    column.DataType = (Type)schemaRow["DataType"];
                    column.Name = (string)schemaRow["ColumnName"];
                    column.Size = (int)schemaRow["ColumnSize"];

                    var schemaBaseTableName = (string)schemaRow["BaseTableName"];
                    var columnName = string.IsNullOrEmpty(schemaBaseTableName) ?
                        column.Name : string.Concat(schemaBaseTableName, '.', column.Name);
                    if (_model.TryGetColumn(columnName) != null)
                    {
                        column.Name = string.Concat(schemaBaseTableName, '.', column.Name);
                    }

                    table.Columns.Add(column);
                }
            }
            return table;
        }
        #region Сущности интерфейсов IEnumerable, IEnumerator

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        object IEnumerator.Current => Current;
        public void Reset()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
