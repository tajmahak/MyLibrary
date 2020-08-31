namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет структурный блок для запроса <see cref="DBQueryBase"/>.
    /// </summary>
    public sealed class DBQueryStructureBlock
    {
        public DBQueryStructureType Type { get; set; }

        public object[] Args { get; set; }

        public int Length => Args.Length;

        public object this[int index]
        {
            get => Args[index];
            set => Args[index] = value;
        }
    }
}
