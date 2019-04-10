using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему столбца в таблице DBTable
    /// </summary>
    public sealed class DBColumn
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса
        /// </summary>
        /// <param name="table">Таблица, к которой будет принадлежать столбец</param>
        public DBColumn(DBTable table)
        {
            Table = table;
        }

        /// <summary>
        /// Имя столбца
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Тип данных, хранимых в столбце
        /// </summary>
        public Type DataType { get; set; }
        /// <summary>
        /// Значение, указывающее на то, является ли этот столбец первичным ключом
        /// </summary>
        public bool IsPrimary { get; set; }
        /// <summary>
        /// Значение, указывающее на допустимость нулевых значений в этом столбце для строк, принадлежащих таблице
        /// </summary>
        public bool AllowDBNull { get; set; }
        /// <summary>
        /// Значение по умолчанию для столбца при создании новых столбцов
        /// </summary>
        public object DefaultValue { get; set; }
        /// <summary>
        /// Максимальная длина текстового столбца
        /// </summary>
        public int MaxTextLength { get; set; }
        /// <summary>
        /// Комментарий к столбцу, извлечённый из базы данных
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Получает таблицу, к которому принадлежит столбец
        /// </summary>
        public DBTable Table { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
