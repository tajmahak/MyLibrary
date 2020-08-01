using MyLibrary.Data;
using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.MSOffice
{
    public sealed class WordTable
    {
        public W.Table Table { get; private set; }
        public W.Document Document { get; private set; }
        public int RowsCount => Table.Rows.Count;
        public int ColumnsCount => Table.Columns.Count;

        public WordTable(W.Table wTable, W.Document wDocument)
        {
            Table = wTable;
            Document = wDocument;
        }
        public object this[int rowIndex, int columnIndex]
        {
            get => GetValue(rowIndex, columnIndex);
            set => SetValue(rowIndex, columnIndex, Format.Convert<string>(value));
        }

        public void SetValue(int rowIndex, int columnIndex, string text)
        {
            text = text ?? string.Empty;
            Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text = text;
        }
        public void SetValue(int rowIndex, int columnIndex, object value)
        {
            SetValue(rowIndex, columnIndex, Format.Convert<string>(value));
        }

        public void MergeCells(int rowIndex, int columnIndex, int rowsCount, int columnsCount)
        {
            W.Cell wCell1 = Table.Cell(rowIndex + 1, columnIndex + 1);
            W.Cell wCell2 = Table.Cell(rowIndex + rowsCount, columnIndex + columnsCount);
            wCell1.Merge(wCell2);
        }
        public void InsertRow(int rowIndex, int columnIndex = 0)
        {
            W.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            W.Range wRange = wCell.Range;
            wRange.Rows.Add(wCell);
        }
        public void AddRow(int rowIndex, int columnIndex = 0)
        {
            W.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            W.Range wRange = wCell.Range;
            wRange.Rows.Add();
        }
        public void DeleteRow(int rowIndex, int columnIndex = 0)
        {
            W.Cell wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            W.Range wRange = wCell.Range;
            wRange.Rows.Delete();
        }
        public string GetValue(int rowIndex, int columnIndex)
        {
            return Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text;
        }
        public WordRange GetCellRange(int rowIndex, int columnIndex)
        {
            W.Range wRange = Table.Cell(rowIndex + 1, columnIndex + 1).Range;
            return new WordRange(wRange);
        }
        public WordRange GetCellRange(int rowIndex, int columnIndex, int rowsCount, int columnsCount)
        {
            int wCell1 = Table.Cell(rowIndex + 1, columnIndex + 1).Range.Start;
            int wCell2 = Table.Cell(rowIndex + rowsCount, columnIndex + columnsCount).Range.End;
            W.Range wRange = Document.Range(wCell1, wCell2);
            return new WordRange(wRange);
        }
        public void AutoFitContent()
        {
            Table.AutoFitBehavior(W.WdAutoFitBehavior.wdAutoFitContent);
        }
        public void AutoFitWindow()
        {
            Table.AutoFitBehavior(W.WdAutoFitBehavior.wdAutoFitWindow);
        }
        public void AutoFitFixed()
        {
            Table.AutoFitBehavior(W.WdAutoFitBehavior.wdAutoFitFixed);
        }
    }
}
