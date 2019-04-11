using System.ComponentModel;
using System.Windows.Forms;
using System;

namespace MyLibrary.Controls
{
    //[System.Diagnostics.DebuggerStepThrough]
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
                for (int i = 0; i < Columns.Count; i++)
                {
                    var column = Columns[i];
                    if (column == dataGridViewColumn)
                    {
                        column.HeaderCell.SortGlyphDirection = (direction == ListSortDirection.Ascending) ? SortOrder.Ascending : SortOrder.Descending;
                    }
                    else
                    {
                        column.HeaderCell.SortGlyphDirection = SortOrder.None;
                    }
                }







                OnSorted(EventArgs.Empty);
            }
            //dataGridViewColumn.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.Descending;
        }
    }
}
