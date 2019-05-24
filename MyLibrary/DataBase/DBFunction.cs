namespace MyLibrary.DataBase
{
    public static class DBFunction
    {
        // Функции для работы со строками

        /// <summary>
        /// Возвращает длину (в символах) строки, переданной в качестве аргумента.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static int CharLength(string str) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Возвращает хэш-значение входной строки. Эта функция полностью поддерживает текстовые BLOB любой длины и с любым набором символов.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static long Hash(string str) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Возвращает левую часть строки, количество возвращаемых символов определяется вторым параметром.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="num">Целое число. Определяет количество возвращаемых символов.</param>
        /// <returns></returns>
        public static string Left(string str, int num) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Возвращает входную строку в нижнем регистре. Точный результат зависит от набора символов входной строки. Например, для наборов символов NONE и ASCII только ASCII символы переводятся в нижний регистр; для OCTETS – вся входная строка возвращается без изменений.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static string Lower(string str) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Дополняет слева входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <param name="padstr">Строка, которой дополняется исходная строка до указанной длины. По умолчанию является пробелом (' ').</param>
        /// <returns></returns>
        public static string LPad(string str, int endlen, string padstr = null) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Предназначена для замены части строки другой строкой.
        /// По умолчанию число удаляемых из строки символов равняется длине заменяемой строки.Дополнительный четвёртый параметр позволяет пользователю задать своё число символов, которые будут удалены.
        /// </summary>
        /// <param name="string">Строка, в которой происходит замена.</param>
        /// <param name="replacement">Строка, которой заменяется.</param>
        /// <param name="pos">Позиция, с которой происходит замена.</param>
        /// <param name="length">Количество символов, которые будут удалены из исходной строки.</param>
        /// <returns></returns>
        public static string Overlay(string @string, string replacement, int pos, int? length = null) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Возвращает конечную (правую) часть входной строки. Длина возвращаемой подстроки определяется вторым параметром.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="num">Целое число. Определяет количество возвращаемых символов.</param>
        /// <returns></returns>
        public static long Right(string str, int num) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Функция UPPER возвращает входную строку в верхнем регистре. Точный результат зависит от набора символов входной строки. Например, для наборов символов NONE и ASCII только ASCII символы переводятся в верхний регистр; для OCTETS — вся входная строка возвращается без изменений.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static string Upper(string str) => throw DBInternal.DBFunctionException();



        /// <summary>
        /// Дополняет справа входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <param name="padstr">Строка, которой дополняется исходная строка до указанной длины. По умолчанию является пробелом(' ').</param>
        /// <returns></returns>
        public static string RPad(string str, int endlen, string padstr = null) => throw DBInternal.DBFunctionException();

        // Предикаты сравнения

        /// <summary>
        /// Проверяет, попадает (или не попадает при использовании NOT) ли значение во включающий диапазон значений.
        /// </summary>
        /// <param name="value_1"></param>
        /// <param name="value_2"></param>
        /// <returns></returns>
        public static bool Between(object value_1, object value_2) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Сравнивает выражение символьного типа с шаблоном, определённым во втором выражении. Сравнение с шаблоном является чувствительным к регистру (за исключением случаев, когда само поле определено с сортировкой (COLLATION) нечувствительной к регистру).
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Шаблон поиска.</param>
        /// <param name="escape_character">Символ экранирования.</param>
        /// <returns></returns>
        public static bool Like(string match_value, string pattern, char? escape_character = null) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Ищет строку или тип, подобный строке, которая начинается с символов в его аргументе. Поиск STARTING WITH чувствителен к регистру.
        /// </summary>
        /// <param name="value_1"></param>
        /// <param name="value_2"></param>
        /// <returns></returns>
        public static bool StartingWith(object value_1, object value_2) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Ищет строку или тип, подобный строке, отыскивая последовательность символов, которая соответствует его аргументу. Он может быть использован для алфавитно-цифрового (подобного строковому) поиска в числах и датах. Поиск CONTAINING не чувствителен к регистру. Тем не менее, если используется сортировка чувствительная к акцентам, то поиск будет чувствителен к акцентам.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool Containing(string value1, string value2) => throw DBInternal.DBFunctionException();
        /// <summary>
        /// Оператор SIMILAR TO проверяет соответствие строки с шаблоном регулярного выражения SQL. В отличие от некоторых других языков для успешного выполнения шаблон должен соответствовать всей строке — соответствие подстроки не достаточно. Если один из операндов имеет значение NULL, то и результат будет NULL. В противном случае результат является TRUE или FALSE.
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Регулярное выражение SQL.</param>
        /// <param name="escape_character">Символ экранирования.</param>
        /// <returns></returns>
        public static bool SimilarTo(string match_value, string pattern, char? escape_character = null) => throw DBInternal.DBFunctionException();
    }
}
