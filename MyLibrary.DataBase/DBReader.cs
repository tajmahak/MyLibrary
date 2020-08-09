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
        private readonly DbCommand _dbCommand;
        private readonly DbDataReader _dbReader;
        private readonly DBTable _table;
        private readonly Converter<DBRow, T> _rowConverter;


        public DBReader(DBProvider provider, DbConnection connection, DBQueryBase query, Converter<DBRow, T> rowConverter, CommandBehavior behavior)
        {
            _dbCommand = provider.CreateCommand(connection, query);
            _dbReader = _dbCommand.ExecuteReader(behavior);
            _table = query.IsView ? GetTableFromSchema() : query.Table;
            _rowConverter = rowConverter;
        }


        public bool MoveNext()
        {
            if (_dbReader.Read())
            {
                DBRow row = new DBRow(_table);
                _dbReader.GetValues(row.Values);
                row.State = DataRowState.Unchanged;
                Current = _rowConverter(row);
                return true;
            }
            return false;
        }

        public List<T> ToList()
        {
            List<T> list = new List<T>();
            foreach (T row in this)
            {
                list.Add(row);
            }
            return list;
        }

        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            _dbReader.Dispose();
            _dbCommand.Dispose();
        }


        private DBTable GetTableFromSchema()
        {
            DBTable table = new DBTable();
            using (DataTable schema = _dbReader.GetSchemaTable())
            {
                int orderIndex = 0;
                foreach (DataRow schemaRow in schema.Rows)
                {
                    string schemaBaseTableName = (string)schemaRow["BaseTableName"];
                    string schemaColumnName = (string)schemaRow["ColumnName"];
                    DBColumn column = new DBColumn(table)
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        object IEnumerator.Current => Current;
    }
}
