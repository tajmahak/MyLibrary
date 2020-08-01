namespace MyLibrary.DataBase
{
    /// <summary>
    /// Предоставляет параметр для <see cref="DBCompiledQuery"/>.
    /// </summary>
    public sealed class DBParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
