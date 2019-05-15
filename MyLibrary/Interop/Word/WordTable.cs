using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.Word
{
    public class WordTable
    {
        public W.Table Table { get; private set; }

        public WordTable(W.Table wTable)
        {
            Table = wTable;
        }

        public int RowsCount
        {
            get
            {
                return Table.Rows.Count;
            }
        }
        public int ColumnsCount
        {
            get
            {
                return Table.Columns.Count;
            }
        }
        public void SetValue(int rowIndex, int columnIndex, string text)
        {
            text = text ?? string.Empty;
            Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text = text;
        }
        public string GetValue(int rowIndex, int columnIndex)
        {
            return Table.Cell(rowIndex + 1, columnIndex + 1).Range.Text;
        }
        public void MergeCells(int rowIndex, int columnIndex, int mergeRows, int mergeCells)
        {
            var wCell1 = Table.Cell(rowIndex + 1, columnIndex + 1);
            var wCell2 = Table.Cell(rowIndex + mergeRows, columnIndex + mergeCells);
            wCell1.Merge(wCell2);
        }
        public WordRange GetCellRange(int rowIndex, int columnIndex)
        {
            var wRange = Table.Cell(rowIndex + 1, columnIndex + 1).Range;
            return new WordRange(wRange);
        }

        public void InsertRow(int rowIndex, int columnIndex = 0)
        {
            var wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            var wRange = wCell.Range;
            wRange.Rows.Add(wCell);
        }
        public void AddRow(int rowIndex, int columnIndex = 0)
        {
            var wCell = Table.Cell(rowIndex + 1, columnIndex + 1);
            var wRange = wCell.Range;
            wRange.Rows.Add();
        }
    }
}
