using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public class DBCompiledQuery
    {
        public string CommandText { get; set; }
        public List<DBCompiledQueryParameter> Parameters { get; set; } = new List<DBCompiledQueryParameter>();
        public int NextParameterNumber { get; set; }
    }

    public class DBCompiledQueryParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
