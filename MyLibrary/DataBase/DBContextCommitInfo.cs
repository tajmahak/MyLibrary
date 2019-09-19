namespace MyLibrary.DataBase
{
    public sealed class DBContextCommitInfo
    {
        public int InsertedRowsCount { get; internal set; }
        public int UpdatedRowsCount { get; internal set; }
        public int DeletedRowsCount { get; internal set; }
    }
}
