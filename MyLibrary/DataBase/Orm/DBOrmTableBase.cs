namespace MyLibrary.DataBase.Orm
{
    public abstract class DBOrmTableBase
    {
        public DBRow Row { get; set; }

        public void Delete()
        {
            Row.Delete();
        }
        public void SetNotNull()
        {
            Row.SetNotNull();
        }
    }
}
