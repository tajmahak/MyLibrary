namespace MyLibrary.DataBase
{
    public sealed class DBIndex
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsUnique { get; set; }
        public bool IsForeign { get; set; }
        public DBTable Table { get; private set; }


        public DBIndex(DBTable table)
        {
            Table = table;
        }


        public override string ToString()
        {
            return Name;
        }
    }
}
