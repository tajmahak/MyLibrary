using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace MyLibrary.Win32.Controls
{
    [DebuggerStepThrough]
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

        /// <summary>
        /// используется для вызова события CellValuePushed/CellValueChanged сразу после изменения значения ячейки
        /// </summary>
        [DefaultValue(false)]
        public bool CommitAfterInput { get; set; }

        public bool MoveToNextCell()
        {
            return ProcessTabKey(Keys.Tab);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (NextTabOnEnterButton && keyData == Keys.Enter)
            {
                ProcessTabKey(Keys.Tab);
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            if (ContextMenuStrip != null)
            {
                if (e.RowIndex != -1)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        DataGridViewRow gridRow = Rows[e.RowIndex];
                        if (!gridRow.Selected)
                        {
                            ClearSelection();
                            gridRow.Selected = true;
                        }
                    }
                }
            }
            base.OnCellMouseDown(e);
        }

        protected override void OnCellContentClick(DataGridViewCellEventArgs e)
        {
            base.OnCellContentClick(e);
            if (CommitAfterInput)
            {
                CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
            {
                return false;
            }

            if (e.KeyCode == Keys.Escape)
            {
                return false;
            }

            if (e.Shift && e.KeyCode == Keys.Space)
            {
                return false;
            }

            if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
            {
                return false;
            }

            if (NextTabOnEnterButton && e.KeyCode == Keys.Enter)
            {
                ProcessTabKey(Keys.Tab);
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
                SortOrder sortOrder = (direction == ListSortDirection.Ascending) ? SortOrder.Ascending : SortOrder.Descending;

                ReflectionHelper.SetValue(this, "sortedColumn", dataGridViewColumn);
                ReflectionHelper.SetValue(this, "sortOrder", sortOrder);
                for (int i = 0; i < Columns.Count; i++)
                {
                    DataGridViewColumn column = Columns[i];
                    if (column == dataGridViewColumn)
                    {
                        column.HeaderCell.SortGlyphDirection = sortOrder;
                    }
                    else
                    {
                        column.HeaderCell.SortGlyphDirection = SortOrder.None;
                    }
                }

                DataGridViewRow[] rows = new DataGridViewRow[Rows.Count];
                for (int i = 0; i < Rows.Count; i++)
                {
                    rows[i] = Rows[i];
                }

                Sorting.StableInsertionSort(rows, (row1, row2) =>
                {
                    object cellValue1 = row1.Cells[dataGridViewColumn.Index].Value;
                    object cellValue2 = row2.Cells[dataGridViewColumn.Index].Value;

                    DataGridViewSortCompareEventArgs e = new DataGridViewSortCompareEventArgs(dataGridViewColumn, cellValue1, cellValue2, row1.Index, row2.Index);
                    OnSortCompare(e);
                    if (e.Handled)
                    {
                        return e.SortResult;
                    }
                    else
                    {
                        return Data.Compare(cellValue1, cellValue2);
                    }
                });

                if (sortOrder == SortOrder.Descending)
                {
                    Array.Reverse(rows);
                }

                int firstDisplayedScrollingRowIndex = FirstDisplayedScrollingRowIndex;

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
