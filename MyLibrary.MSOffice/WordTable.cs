using Word = Microsoft.Office.Interop.Word;

namespace MyLibrary.MSOffice
{
    public sealed class WordTable
    {
        public Word.Table Table { get; private set; }
        public Word.Document Document { get; private set; }
        public int RowsCount => Table.Rows.Count;
        public int ColumnsCount => Table.Columns.Count;
        public object this[int rowIndex, int columnIndex]
        {
            get => GetValue(rowIndex, columnIndex);
            set => SetValue(rowIndex, columnIndex, Data.Convert<string>(value));
        }


        public WordTable(Word.Table wTable, Word.Document wDocument)
        {
            Table = wTable;
            Document = wDocument;
        }


        public void SetValue(int rowIndex, int columnIndex, string text)
        {
            text = text ?? string.Empty;
            Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text = text;
        }

        public void SetValue(int rowIndex, int columnIndex, object value)
        {
            SetValue(rowIndex, columnIndex, Data.Convert<string>(value));
        }

        public void MergeCells(int rowIndex, int columnIndex, int rowsCount, int columnsCount)
        {
            Word.Cell wCell1 = Table.Cell(rowIndex + 1, columnIndex + 1);
            Word.Cell wCell2 = Table.Cell(rowIndex + rowsCount, columnIndex + columnsCount);
            wCell1.Merge(wCell2);
        }

        public void InsertRow(int rowIndex, int columnIndex = 0)
        {
            Word.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            Word.Range wRange = wCell.Range;
            wRange.Rows.Add(wCell);
        }

        public void AddRow(int rowIndex, int columnIndex = 0)
        {
            Word.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            Word.Range wRange = wCell.Range;
            wRange.Rows.Add();
        }

        public void DeleteRow(int rowIndex, int columnIndex = 0)
        {
            Word.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            Word.Range wRange = wCell.Range;
            wRange.Rows.Delete();
        }

        public string GetValue(int rowIndex, int columnIndex)
        {
            return Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text;
        }

        public WordRange GetCellRange(int rowIndex, int columnIndex)
        {
            Word.Range wRange = Table.Cell(rowIndex + 1, columnIndex + 1).Range;
            return new WordRange(wRange);
        }

        public WordRange GetCellRange(int rowIndex, int columnIndex, int rowsCount, int columnsCount)
        {
            int wCell1 = Table.Cell(rowIndex + 1, columnIndex + 1).Range.Start;
            int wCell2 = Table.Cell(rowIndex + rowsCount, columnIndex + columnsCount).Range.End;
            Word.Range wRange = Document.Range(wCell1, wCell2);
            return new WordRange(wRange);
        }

        public void AutoFitContent()
        {
            Table.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
        }

        public void AutoFitWindow()
        {
            Table.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitWindow);
        }

        public void AutoFitFixed()
        {
            Table.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitFixed);
        }
    }
}
