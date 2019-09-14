﻿using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет подготовленную (или скомпилированную) версию SQL-запроса <see cref="DBQueryBase"/> для источника данных.
    /// </summary>
    public sealed class DBCompiledQuery
    {
        public string CommandText { get; internal set; }
        public List<DBParameter> Parameters { get; private set; } = new List<DBParameter>();
        internal int NextParameterNumber { get; set; }
    }
}
