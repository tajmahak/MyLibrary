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
        private readonly Func<DBRow, T> _rowConverter;

        public DBReader(DbConnection connection, DBModelBase model, DBQueryBase query, Func<DBRow, T> rowConverter)
        {
            _model = model;
            _command = model.CreateCommand(connection, query);
            _reader = _command.ExecuteReader();
            _table = query.IsView ? GetTableFromSchema() : query.Table;
            _rowConverter = rowConverter;
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
                Current = _rowConverter(row);
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

        private DBTable GetTableFromSchema()
        {
            var table = new DBTable(_model);
            using (var schema = _reader.GetSchemaTable())
            {
                var orderIndex = 0;
                foreach (DataRow schemaRow in schema.Rows)
                {
                    var schemaBaseTableName = (string)schemaRow["BaseTableName"];
                    var schemaColumnName = (string)schemaRow["ColumnName"];
                    var column = new DBColumn(table)
                    {
                        OrderIndex = orderIndex++,
                        DataType = (Type)schemaRow["DataType"],
                        Name = string.IsNullOrEmpty(schemaBaseTableName) ? schemaColumnName : string.Concat(schemaBaseTableName, '.', schemaColumnName),
                        Size = (int)schemaRow["ColumnSize"]
                    };
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
