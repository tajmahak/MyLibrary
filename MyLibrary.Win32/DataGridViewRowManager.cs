using System.Collections.Generic;
using System.Windows.Forms;

namespace MyLibrary.Win32
{
    public class DataGridViewRowManager
    {
        public DataGridViewRowManager(DataGridView grid)
        {
            DataGridView = grid;
        }

        public DataGridView DataGridView { get; private set; }
        public int Count => _gridRows.Count;
        private readonly List<DataGridViewRow> _gridRows = new List<DataGridViewRow>();

        public DataGridViewRow Add(params object[] values)
        {
            DataGridViewRow gridRow = new DataGridViewRow();
            gridRow.CreateCells(DataGridView, values);

            // Применение шаблона
            DataGridViewRow template = (DataGridViewRow)DataGridView.RowTemplate.Clone();
            gridRow.ContextMenuStrip = template.ContextMenuStrip;
            gridRow.DefaultCellStyle = template.DefaultCellStyle;
            gridRow.DividerHeight = template.DividerHeight;
            gridRow.ErrorText = template.ErrorText;
            gridRow.Height = DataGridView.RowTemplate.Height;
            gridRow.ReadOnly = template.ReadOnly;
            gridRow.Resizable = template.Resizable;

            _gridRows.Add(gridRow);
            return gridRow;
        }

        public void Commit(bool crearExistRows = true)
        {
            if (crearExistRows)
            {
                DataGridView.Rows.Clear();
            }
            DataGridView.Rows.AddRange(_gridRows.ToArray());
            _gridRows.Clear();
        }
    }
}
