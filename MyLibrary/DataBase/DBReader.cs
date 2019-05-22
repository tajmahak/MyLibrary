using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MyLibrary.DataBase
{
    public sealed class DBReader<T> : IEnumerable<T>, IEnumerator<T>
    {
        internal DBReader(DbConnection connection, DBModelBase model, DBQuery query)
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
                _currentRow = DBInternal.PackRow<T>(row);
                return true;
            }
            return false;
        }
        public T Current
        {
            get
            {
                return _currentRow;
            }
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

        #region Скрытые сущности

        private DBTable GenerateTable()
        {
            var table = new DBTable(_model, null);
            var columns = new DBColumn[_reader.FieldCount];
            using (var schema = _reader.GetSchemaTable())
            {
                for (int i = 0; i < schema.Rows.Count; i++)
                {
                    var schemaRow = schema.Rows[i];
                    var baseTableName = (string)schemaRow["BaseTableName"];
                    if (!string.IsNullOrEmpty(baseTableName))
                    {
                        var baseColumnName = (string)schemaRow["BaseColumnName"];
                        var columnName = string.Concat(baseTableName, '.', baseColumnName);
                        columns[i] = _model.GetColumn(columnName);
                    }
                    else
                    {
                        var columnName = (string)schemaRow["ColumnName"];
                        var column = new DBColumn(table);
                        column.Name = columnName;
                        columns[i] = column;
                    }
                }
            }
            table.AddColumns(columns);
            return table;
        }

        private DbCommand _command;
        private DbDataReader _reader;
        private T _currentRow;
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
