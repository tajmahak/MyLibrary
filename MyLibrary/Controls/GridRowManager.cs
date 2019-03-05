using System.Collections.Generic;
using System.Windows.Forms;

namespace MyLibrary.Controls
{
    public class GridRowManager
    {
        public int Count
        {
            get { return _rows.Count; }
        }
        private DataGridView _grid;
        private List<DataGridViewRow> _rows;
        private Dictionary<string, int> _columnsIndex;

        public GridRowManager(DataGridView grid, int? capacity = null)
        {
            _grid = grid;
            _rows = (capacity != null) ?
                new List<DataGridViewRow>(capacity.Value) :
                new List<DataGridViewRow>();
        }

        public DataGridViewRow Add(params object[] values)
        {
            var row = _grid.CreateRow(values);
            _rows.Add(row);
            return row;
        }
        public DataGridViewRow Add(Row row)
        {
            return Add(row.Values);
        }
        public void Add(DataGridViewRow gridRow)
        {
            _rows.Add(gridRow);
        }
        public Row Create()
        {
            _columnsIndex = new Dictionary<string, int>();
            for (int i = 0; i < _grid.Columns.Count; i++)
                _columnsIndex.Add(_grid.Columns[i].Name, i);
            return new Row(this);
        }

        public void Commit(bool SuspendLayot = true)
        {
            if (SuspendLayot)
                _grid.SuspendLayout();
            _grid.Rows.AddRange(_rows.ToArray());
            _rows.Clear();
            if (SuspendLayot)
                _grid.ResumeLayout();
        }

        #region [class] Row
        public class Row
        {
            private GridRowManager GridRows { get; set; }
            internal object[] Values { get; set; }

            public Row(GridRowManager gridRows)
            {
                GridRows = gridRows;
                Values = new object[gridRows._grid.Columns.Count];
            }
            public object this[string columnName]
            {
                get
                {
                    int index = GridRows._columnsIndex[columnName];
                    return Values[index];
                }
                set
                {
                    int index = GridRows._columnsIndex[columnName];
                    Values[index] = value;
                }
            }
            public object this[int columnIndex]
            {
                get
                { return Values[columnIndex]; }
                set
                { Values[columnIndex] = value; }
            }
        }
        #endregion
    }
}
