namespace MyLibrary.DataBase
{
    public class DBQueryStructureBlock
    {
        public DBQueryStructureTypeEnum Type { get; set; }
        public object[] Args { get; set; }

        public object this[int index]
        {
            get => Args[index];
            set => Args[index] = value;
        }
    }
}
