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
        private readonly DbCommand dbCommand;
        private readonly DbDataReader dbReader;
        private readonly DBTable table;
        private readonly Converter<DBRow, T> rowConverter;


        public DBReader(DBProvider provider, DbConnection connection, DBQueryBase query, Converter<DBRow, T> rowConverter, CommandBehavior behavior)
        {
            dbCommand = provider.CreateCommand(connection, query);
            dbReader = dbCommand.ExecuteReader(behavior);
            table = query.IsView ? GetTableFromSchema() : query.Table;
            this.rowConverter = rowConverter;
        }


        public bool MoveNext()
        {
            if (dbReader.Read())
            {
                DBRow row = new DBRow(table);
                dbReader.GetValues(row.Values);
                row.State = DataRowState.Unchanged;
                Current = rowConverter(row);
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
            dbReader.Dispose();
            dbCommand.Dispose();
        }


        private DBTable GetTableFromSchema()
        {
            DBTable table = new DBTable();
            using (DataTable schema = dbReader.GetSchemaTable())
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
