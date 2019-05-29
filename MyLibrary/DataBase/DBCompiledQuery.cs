using MyLibrary.Collections;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет подготовленную (или скомпилированную) версию SQL-запроса <see cref="DBQueryBase"/> для источника данных.
    /// </summary>
    public sealed class DBCompiledQuery
    {
        public string CommandText { get; set; }
        public ReadOnlyList<DBParameter> Parameters { get; set; } = new ReadOnlyList<DBParameter>();
        public int NextParameterNumber { get; set; }
    }
}
