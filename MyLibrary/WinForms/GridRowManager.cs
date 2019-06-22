using System.Collections.Generic;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    public class GridRowManager
    {
        public int Count => _gridRows.Count;
        private readonly DataGridView _grid;
        private readonly List<DataGridViewRow> _gridRows = new List<DataGridViewRow>();

        public GridRowManager(DataGridView grid)
        {
            _grid = grid;
        }

        public DataGridViewRow Add(params object[] values)
        {
            var row = _grid.CreateRow(values);
            _gridRows.Add(row);
            return row;
        }
        public void Commit()
        {
            _grid.Rows.AddRange(_gridRows.ToArray());
            _gridRows.Clear();
        }
        public void Clear()
        {
            _grid.Rows.Clear();
        }
    }
}
