using System;
using System.Collections.Generic;
using System.Text;

namespace MyLibrary.DataBase
{
    public abstract class DBQueryCompilerBase
    {
        public DBQueryCompilerBase(DBModelBase model)
        {
            Model = model;
        }

        public char BeginBlock { get; set; }
        public char EndBlock { get; set; }
        public char ParameterPrefix { get; set; }

        public abstract DBCompiledQuery CompileQuery(DBQuery query, int nextParameterNumber = 0);

        public virtual string GetSelectCommand(DBTable table)
        {
            var str = new StringBuilder();
            str.Append("SELECT ");
            str.Append(GetName(table.Name));
            str.Append(".* FROM ");
            str.Append(GetName(table.Name));
            return str.ToString();
        }
        public virtual string GetInsertCommand(DBTable table)
        {
            var str = new StringBuilder();
            str.Append("INSERT INTO ");
            str.Append(GetName(table.Name));
            str.Append(" VALUES(");

            int index = 0;
            for (int j = 0; j < table.Columns.Length; j++)
            {
                if (j > 0)
                {
                    str.Append(',');
                }
                if (table.Columns[j].IsPrimary)
                {
                    str.Append("NULL");
                }
                else
                {
                    str.Append(string.Concat(ParameterPrefix, 'p', index));
                    index++;
                }
            }
            str.Append(')');
            return str.ToString();
        }
        public virtual string GetUpdateCommand(DBTable table)
        {
            var str = new StringBuilder();

            str.Append("UPDATE ");
            str.Append(GetName(table.Name));
            str.Append(" SET ");
            int index = 0;
            for (int j = 0; j < table.Columns.Length; j++)
            {
                var column = table.Columns[j];
                if (column.IsPrimary)
                {
                    continue;
                }
                if (index != 0)
                {
                    str.Append(',');
                }
                str.Append(GetName(column.Name));
                str.Append('=').Append(ParameterPrefix).Append(index++);
            }
            str.Append(" WHERE ");
            str.Append(GetName(table.Columns[table.PrimaryKeyIndex].Name));
            str.Append('=').Append(ParameterPrefix).Append("id");
            return str.ToString();
        }
        public virtual string GetDeleteCommand(DBTable table)
        {
            var str = new StringBuilder();
            str.Append("DELETE FROM ");
            str.Append(GetName(table.Name));
            str.Append(" WHERE ");
            for (int j = 0; j < table.Columns.Length; j++)
            {
                var column = table.Columns[j];
                if (column.IsPrimary)
                {
                    str.Append(GetName(column.Name));
                    break;
                }
            }
            str.Append('=').Append(ParameterPrefix).Append("id");
            return str.ToString();
        }

        public string GetFullName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(BeginBlock, split[0], EndBlock, '.', BeginBlock, split[1], EndBlock);
        }
        public string GetName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(BeginBlock, split[0], EndBlock);
        }
        public string GetColumnName(object value)
        {
            string[] split = ((string)value).Split('.');
            return string.Concat(BeginBlock, split[1], EndBlock);
        }

        protected List<object[]> FindBlockList(DBQuery query, Predicate<string> predicate)
        {
            return query.Structure.FindAll(block => predicate((string)block[0]));
        }
        protected List<object[]> FindBlockList(DBQuery query, string name)
        {
            return FindBlockList(query, x => x == name);
        }
        protected object[] FindBlock(DBQuery query, Predicate<string> predicate)
        {
            return query.Structure.Find(block =>
                predicate((string)block[0]));
        }
        protected object[] FindBlock(DBQuery query, string name)
        {
            return FindBlock(query, x => x == name);
        }
        protected string AddParameter(object value, DBCompiledQuery cQuery)
        {
            value = value ?? DBNull.Value;

            if (value is string && Model.ColumnsDict.ContainsKey((string)value))
            {
                return GetFullName((string)value);
            }

            var type = value.GetType();
            if (type.BaseType == typeof(Enum))
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(type));
            }

            var paramNumber = cQuery.NextParameterNumber++;
            var parameter = new DBCompiledQueryParameter()
            {
                Name = string.Concat(ParameterPrefix, 'p', paramNumber),
                Value = value,
            };
            cQuery.Parameters.Add(parameter);

            return parameter.Name;
        }
        protected DBModelBase Model { get; private set; }
        protected void Add(StringBuilder str, params object[] values)
        {
            foreach (var value in values)
            {
                str.Append(value);
            }
        }
    }
}
