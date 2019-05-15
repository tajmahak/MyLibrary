using System;
using System.ComponentModel;
using System.Windows.Forms;
using MyLibrary.Data;

namespace MyLibrary.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyDataGridView : DataGridView
    {
        public MyDataGridView()
        {
            base.DoubleBuffered = true;
        }

        [DefaultValue(false)]
        public bool NextTabOnEnterButton { get; set; }

        [DefaultValue(false)]
        public bool StableSort { get; set; }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (NextTabOnEnterButton && keyData == Keys.Enter)
            {
                base.ProcessTabKey(Keys.Tab);
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
                return false;

            if (e.KeyCode == Keys.Escape)
                return false;
            if (e.Shift && e.KeyCode == Keys.Space)
                return false;
            if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
                return false;
            if (NextTabOnEnterButton && e.KeyCode == Keys.Enter)
            {
                base.ProcessTabKey(Keys.Tab);
                return true;
            }

            return base.ProcessDataGridViewKey(e);
        }

        public override void Sort(DataGridViewColumn dataGridViewColumn, ListSortDirection direction)
        {
            if (!StableSort)
            {
                base.Sort(dataGridViewColumn, direction);
            }
            else
            {
                var sortOrder = (direction == ListSortDirection.Ascending) ? SortOrder.Ascending : SortOrder.Descending;

                Format.SetValue(this, "sortedColumn", dataGridViewColumn);
                Format.SetValue(this, "sortOrder", sortOrder);
                for (int i = 0; i < Columns.Count; i++)
                {
                    var column = Columns[i];
                    if (column == dataGridViewColumn)
                    {
                        column.HeaderCell.SortGlyphDirection = sortOrder;
                    }
                    else
                    {
                        column.HeaderCell.SortGlyphDirection = SortOrder.None;
                    }
                }

                var rows = new DataGridViewRow[Rows.Count];
                for (int i = 0; i < Rows.Count; i++)
                {
                    rows[i] = Rows[i];
                }

                Sorting.StableInsertionSort(rows, (x, y) =>
                {
                    var value1 = x.Cells[dataGridViewColumn.Index].Value;
                    var value2 = y.Cells[dataGridViewColumn.Index].Value;
                    return Format.Compare(value1, value2);
                });

                if (sortOrder == SortOrder.Descending)
                {
                    Array.Reverse(rows);
                }

                var firstDisplayedScrollingRowIndex = FirstDisplayedScrollingRowIndex;

                Rows.Clear();
                Rows.AddRange(rows);

                if (firstDisplayedScrollingRowIndex != -1)
                {
                    FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
                }

                OnSorted(EventArgs.Empty);
            }
        }
    }
}
