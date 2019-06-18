namespace MyLibrary.DataBase
{
    /// <summary>
    /// Предоставляет статические функции для представления функций языка SQL в объектах <see cref="DBQueryBase"/>. Функции данного класса не предназначены для непосредственного использования из кода.
    /// </summary>
    public static class DBFunction
    {
        public static object As(object expr, string alias)
        {
            throw DBInternal.DBFunctionException();
        }

        public static object Desc(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        public static object Distinct(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        public static object All(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        public static object Alias(string columnName)
        {
            throw DBInternal.DBFunctionException();
        }

        #region Предикаты сравнения

        /// <summary>
        /// Проверяет, попадает (или не попадает при использовании NOT) ли значение во включающий диапазон значений.
        /// </summary>
        /// <param name="value_1"></param>
        /// <param name="value_2"></param>
        /// <returns></returns>
        public static bool Between(object value, object value_1, object value_2)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Сравнивает выражение символьного типа с шаблоном, определённым во втором выражении. Сравнение с шаблоном является чувствительным к регистру (за исключением случаев, когда само поле определено с сортировкой (COLLATION) нечувствительной к регистру).
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Шаблон поиска.</param>
        /// <param name="escape_character">Символ экранирования.</param>
        /// <returns></returns>
        public static bool Like(string match_value, string pattern, char escape_character)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Сравнивает выражение символьного типа с шаблоном, определённым во втором выражении. Сравнение с шаблоном является чувствительным к регистру (за исключением случаев, когда само поле определено с сортировкой (COLLATION) нечувствительной к регистру).
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Шаблон поиска.</param>
        /// <returns></returns>
        public static bool Like(string match_value, string pattern)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Ищет строку или тип, подобный строке, которая начинается с символов в его аргументе. Поиск <see cref="StartingWith"/> чувствителен к регистру.
        /// </summary>
        /// <param name="value_1"></param>
        /// <param name="value_2"></param>
        /// <returns></returns>
        public static bool StartingWith(object value_1, object value_2)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Ищет строку или тип, подобный строке, отыскивая последовательность символов, которая соответствует его аргументу. Он может быть использован для алфавитно-цифрового (подобного строковому) поиска в числах и датах. Поиск не чувствителен к регистру. Тем не менее, если используется сортировка чувствительная к акцентам, то поиск будет чувствителен к акцентам.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool Containing(string value1, string value2)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Проверяет соответствие строки с шаблоном регулярного выражения SQL. В отличие от некоторых других языков для успешного выполнения шаблон должен соответствовать всей строке — соответствие подстроки не достаточно. Если один из операндов имеет значение NULL, то и результат будет NULL. В противном случае результат является TRUE или FALSE.
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Регулярное выражение SQL.</param>
        /// <param name="escape_character">Символ экранирования.</param>
        /// <returns></returns>
        public static bool SimilarTo(string match_value, string pattern, char escape_character)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Проверяет соответствие строки с шаблоном регулярного выражения SQL. В отличие от некоторых других языков для успешного выполнения шаблон должен соответствовать всей строке — соответствие подстроки не достаточно. Если один из операндов имеет значение NULL, то и результат будет NULL. В противном случае результат является TRUE или FALSE.
        /// </summary>
        /// <param name="match_value">Выражение символьного типа.</param>
        /// <param name="pattern">Регулярное выражение SQL.</param>
        /// <returns></returns>
        public static bool SimilarTo(string match_value, string pattern)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion

        #region Агрегатные функции

        /// <summary>
        /// Возвращает среднее значение для группы. Значения NULL пропускаются.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static decimal Avg(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает количество значений в группе, которые не являются NULL.
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <returns></returns>
        public static int Count(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает количество значений в группе, которые не являются NULL.
        /// </summary>
        /// <returns></returns>
        public static int Count()
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает строку, содержащую значения элементов выборки, которые не равны NULL. При пустой выборке функция возвратит NULL. 
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF, которая возвращает строковый тип данных или BLOB. Поля типа дата / время и числовые преобразуются к строке. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <param name="separator">Разделитель. Выражение строкового типа. По умолчанию разделителем является запятая.</param>
        /// <returns></returns>
        public static string List(object expr, char separator)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает строку, содержащую значения элементов выборки, которые не равны NULL. При пустой выборке функция возвратит NULL. 
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF, которая возвращает строковый тип данных или BLOB. Поля типа дата / время и числовые преобразуются к строке. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <returns></returns>
        public static string List(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает максимальный элемент выборки, которые не равны NULL. При пустой выборке, или при выборке из одних NULL функция возвратит NULL. Если аргумент функции строка, то функция вернёт значение, которое окажется последним в сортировке при применении COLLATE.
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF, которая возвращает строковый тип данных или BLOB. Поля типа дата / время и числовые преобразуются к строке. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <returns></returns>
        public static decimal Max(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает минимальный элемент выборки, которые не равны NULL. При пустой выборке, или при выборке из одних NULL функция возвратит NULL. Если аргумент функции строка, то функция вернёт значение, которое окажется первым в сортировке при применении COLLATE.
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF, которая возвращает строковый тип данных или BLOB. Поля типа дата / время и числовые преобразуются к строке. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <returns></returns>
        public static decimal Min(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает сумму элементов выборки, которые не равны NULL. При пустой выборке, или при выборке из одних NULL функция возвратит NULL.
        /// </summary>
        /// <param name="expr">Выражение. Может содержать столбец таблицы, константу, переменную, выражение, неагрегатную функцию или UDF, которая возвращает строковый тип данных или BLOB. Поля типа дата / время и числовые преобразуются к строке. Агрегатные функции в качестве выражения не допускаются.</param>
        /// <returns></returns>
        public static decimal Sum(object expr)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion

        #region Функции для работы со строками

        /// <summary>
        /// Возвращает длину (в символах) строки, переданной в качестве аргумента.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static int CharLength(string str)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает хэш-значение входной строки. Эта функция полностью поддерживает текстовые BLOB любой длины и с любым набором символов.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static long Hash(string str)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает левую часть строки, количество возвращаемых символов определяется вторым параметром.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="num">Целое число. Определяет количество возвращаемых символов.</param>
        /// <returns></returns>
        public static string Left(string str, int num)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает входную строку в нижнем регистре. Точный результат зависит от набора символов входной строки. Например, для наборов символов NONE и ASCII только ASCII символы переводятся в нижний регистр; для OCTETS – вся входная строка возвращается без изменений.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static string Lower(string str)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Дополняет слева входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <param name="padstr">Строка, которой дополняется исходная строка до указанной длины. По умолчанию является пробелом (' ').</param>
        /// <returns></returns>
        public static string LPad(string str, int endlen, string padstr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Дополняет слева входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <returns></returns>
        public static string LPad(string str, int endlen)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Предназначена для замены части строки другой строкой.
        /// По умолчанию число удаляемых из строки символов равняется длине заменяемой строки.Дополнительный четвёртый параметр позволяет пользователю задать своё число символов, которые будут удалены.
        /// </summary>
        /// <param name="string">Строка, в которой происходит замена.</param>
        /// <param name="replacement">Строка, которой заменяется.</param>
        /// <param name="pos">Позиция, с которой происходит замена.</param>
        /// <param name="length">Количество символов, которые будут удалены из исходной строки.</param>
        /// <returns></returns>
        public static string Overlay(string @string, string replacement, int pos, int? length)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Предназначена для замены части строки другой строкой.
        /// По умолчанию число удаляемых из строки символов равняется длине заменяемой строки.Дополнительный четвёртый параметр позволяет пользователю задать своё число символов, которые будут удалены.
        /// </summary>
        /// <param name="string">Строка, в которой происходит замена.</param>
        /// <param name="replacement">Строка, которой заменяется.</param>
        /// <param name="pos">Позиция, с которой происходит замена.</param>
        /// <returns></returns>
        public static string Overlay(string @string, string replacement, int pos)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Заменяет в строке все вхождения одной строки на другую строку.
        /// </summary>
        /// <param name="str">Строка, в которой делается замена.</param>
        /// <param name="find">Строка, которая ищется.</param>
        /// <param name="repl">Строка, на которую происходит замена.</param>
        /// <returns></returns>
        public static string Replace(string str, string find, string repl)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвратит строку перевёрнутую "задом наперёд".
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static string Reverse(string str)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает конечную (правую) часть входной строки. Длина возвращаемой подстроки определяется вторым параметром.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="num">Целое число. Определяет количество возвращаемых символов.</param>
        /// <returns></returns>
        public static long Right(string str, int num)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Дополняет справа входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <param name="padstr">Строка, которой дополняется исходная строка до указанной длины. По умолчанию является пробелом(' ').</param>
        /// <returns></returns>
        public static string RPad(string str, int endlen, string padstr)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Дополняет справа входную строку пробелами или определённой пользователем строкой до заданной длины.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="endlen">Длина выходной строки.</param>
        /// <param name="padstr">Строка, которой дополняется исходная строка до указанной длины. По умолчанию является пробелом(' ').</param>
        /// <returns></returns>
        public static string RPad(string str, int endlen)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает подстроку строки <paramref name="str"/>, начиная с позиции <paramref name="startpos"/> (позиция начинается с 1) до конца строки или указанной длины. Без предложения FOR возвращаются все оставшиеся символы в строке. С предложением FOR возвращается <paramref name="length"/> символов или остаток строки, в зависимости от того что короче.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="startpos">Позиция, с которой начинается извлечение подстроки. Целочисленное выражение.</param>
        /// <param name="length">Длина возвращаемой подстроки. Целочисленное выражение.</param>
        /// <returns></returns>
        public static string SubString(string str, int startpos, int? length)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает подстроку строки <paramref name="str"/>, начиная с позиции <paramref name="startpos"/> (позиция начинается с 1) до конца строки или указанной длины. Без предложения FOR возвращаются все оставшиеся символы в строке. С предложением FOR возвращается <paramref name="length"/> символов или остаток строки, в зависимости от того что короче.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <param name="startpos">Позиция, с которой начинается извлечение подстроки. Целочисленное выражение.</param>
        /// <returns></returns>
        public static string SubString(string str, int startpos)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает входную строку в верхнем регистре. Точный результат зависит от набора символов входной строки. Например, для наборов символов NONE и ASCII только ASCII символы переводятся в верхний регистр; для OCTETS — вся входная строка возвращается без изменений.
        /// </summary>
        /// <param name="str">Выражение строкового типа.</param>
        /// <returns></returns>
        public static string Upper(string str)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion

        #region Предикаты существования

        /// <summary>
        /// Если результат подзапроса будет содержать хотя бы одну запись, то предикат оценивается как истинный (TRUE), в противном случае предикат оценивается как ложный (FALSE).
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static bool Exists(DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Предикат проверяет, присутствует (или отсутствует, при использовании NOT IN) ли значение выражения слева в результате выполнения подзапроса справа. Результат подзапроса может содержать только один столбец.
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static bool In(object value, DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Предикат проверяет, присутствует ли значение выражения слева в указанном справа наборе значений. Набор значений не может превышать 1500 элементов.
        /// </summary>
        /// <param name="value_list"></param>
        /// <returns></returns>
        public static bool In(object value, object[] value_list)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Предикат использует подзапрос в качестве аргумента и оценивает его как истинный, если подзапрос возвращает одну и только одну строку результата, в противном случае предикат оценивается как ложный. Результат подзапроса может содержать несколько столбцов, поскольку значения не проверяются. Данный предикат может принимать только два значения: истина (TRUE) и ложь (FALSE).
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static bool Singular(DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion

        #region Количественные предикаты подзапросов

        /// <summary>
        /// При использовании квантора ALL, предикат является истинным, если каждое значение выбранное подзапросом удовлетворяет условию в предикате внешнего запроса. Если подзапрос не возвращает ни одной строки, то предикат автоматически считается верным.
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static decimal All(DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// При использовании квантора ANY или SOME, предикат является истинным, если любое из значений выбранное подзапросом удовлетворяет условию в предикате внешнего запроса. Если подзапрос не возвращает ни одной строки, то предикат автоматически считается ложным.
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static decimal Any(DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// При использовании квантора ANY или SOME, предикат является истинным, если любое из значений выбранное подзапросом удовлетворяет условию в предикате внешнего запроса. Если подзапрос не возвращает ни одной строки, то предикат автоматически считается ложным.
        /// </summary>
        /// <param name="select_stmt"></param>
        /// <returns></returns>
        public static decimal Some(DBQueryBase select_stmt)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion

        #region Условные функции

        /// <summary>
        /// Функция принимает два или более аргумента возвращает значение первого NOT NULL аргумента.Если все аргументы имеют значение NULL, то и результат будет NULL.
        /// </summary>
        /// <param name="expr1">Выражения любого совместимого типа.</param>
        /// <param name="expr2">Выражения любого совместимого типа.</param>
        /// <param name="expr_list">Выражения любого совместимого типа.</param>
        /// <returns></returns>
        public static object Coalesce(object expr1, object expr2, params object[] expr_list)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Данная функция эквивалентна конструкции CASE, в которой заданное выражение сравнивается с другими выражениями до нахождения совпадения. Результатом является значение, указанное после выражения, с которым найдено совпадение. Если совпадений не найдено, то возвращается значение по умолчанию (если оно, конечно, задано – в противном случае возвращается NULL).
        /// </summary>
        /// <param name="testexpr">Выражения любого совместимого типа, которое сравнивается с выражениями expr1, expr2 ... exprN</param>
        /// <param name="value_list">
        /// expr1, result1, expr2, result2 ... exprN, resultN, defaultresult, где:
        /// <para>expr: Выражения любого совместимого типа, с которыми сравнивают с выражением testexpr.</para>
        /// <para>result: Возвращаемые выражения любого типа.</para>
        /// <para>defaultresult: Выражения, возвращаемое если ни одно из условий не было выполнено.</para>
        /// </param>
        /// <returns></returns>
        public static object Decode(object testexpr, params object[] value_list)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает максимальное значение из входного списка чисел, строк или параметров с типом DATE/TIME/TIMESTAMP.
        /// </summary>
        /// <param name="expr_list">Выражения любого совместимого типа.</param>
        /// <returns></returns>
        public static object MaxValue(object expr, params object[] expr_list)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Возвращает минимальное значение из входного списка чисел, строк или параметров с типом DATE/TIME/TIMESTAMP.
        /// </summary>
        /// <param name="expr_list">Выражения любого совместимого типа.</param>
        /// <returns></returns>
        public static object MinValue(object expr, params object[] expr_list)
        {
            throw DBInternal.DBFunctionException();
        }

        /// <summary>
        /// Функция возвращает значение первого аргумента, если он неравен второму. В случае равенства аргументов возвращается NULL.
        /// </summary>
        /// <param name="expr1">Выражения любого совместимого типа.</param>
        /// <param name="expr2">Выражения любого совместимого типа.</param>
        /// <returns></returns>
        public static object NullIf(object expr1, object expr2)
        {
            throw DBInternal.DBFunctionException();
        }

        #endregion
    }
}
