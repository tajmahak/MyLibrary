namespace MyLibrary.DataBase
{
    public class DBIndex
    {
        public string Name { get; set; }
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
