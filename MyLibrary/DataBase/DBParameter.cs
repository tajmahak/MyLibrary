﻿namespace MyLibrary.DataBase
{
    /// <summary>
    /// Предоставляет параметр для <see cref="DBCompiledQuery"/>.
    /// </summary>
    public class DBParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}