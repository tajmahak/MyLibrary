using MyLibrary.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    public static class DataGridViewExtension
    {
        public static int GetSelectedRowIndex(this DataGridView grid)
        {
            return grid.SelectedCells.Count == 0 ? -1 : grid.SelectedCells[0].RowIndex;
        }
        public static DataGridViewCell GetSelectedCell(this DataGridView grid)
        {
            return grid.CurrentCell;
        }
        public static DataGridViewRow GetSelectedRow(this DataGridView grid)
        {
            var index = GetSelectedRowIndex(grid);
            return index == -1 ? null : grid.Rows[index];
        }
        public static DataGridViewRow[] GetSelectedRows(this DataGridView grid)
        {
            var hashSet = new HashSet<int>(); // для сортировки получаемого списка согласно сортировке грида
            foreach (DataGridViewCell cell in grid.SelectedCells)
            {
                hashSet.Add(cell.RowIndex);
            }

            var list = new List<DataGridViewRow>(hashSet.Count);
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (hashSet.Contains(row.Index))
                {
                    list.Add(row);
                }
            }

            return list.ToArray();
        }

        public static T GetTag<T>(this DataGridViewRow gridRow)
        {
            return (T)gridRow.Tag;
        }
        public static T GetSelectedTag<T>(this DataGridView grid)
        {
            var gridRow = grid.GetSelectedRow();
            return gridRow == null ? (default) : GetTag<T>(gridRow);
        }
        public static List<T> GetTags<T>(this DataGridViewRowCollection collection)
        {
            var tags = new List<T>(collection.Count);
            for (var i = 0; i < collection.Count; i++)
            {
                var row = collection[i];
                tags.Add(row.GetTag<T>());
            }
            return tags;
        }
        public static List<T> GetTags<T>(this IList<DataGridViewRow> list)
        {
            var tags = new List<T>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var row = list[i];
                tags.Add(row.GetTag<T>());
            }
            return tags;
        }
        public static List<T> GetSelectedTags<T>(this DataGridView grid)
        {
            return grid.GetSelectedRows().GetTags<T>();
        }
        public static List<T> GetSelectedVirtualTags<T>(this DataGridView grid, List<T> srcList)
        {
            var gridRows = grid.GetSelectedRows();
            var list = new List<T>(gridRows.Length);
            for (var i = 0; i < gridRows.Length; i++)
            {
                var gridRow = gridRows[i];
                list.Add(srcList[gridRow.Index]);
            }
            return list;
        }
        public static DataGridViewRow GetRowFromTag<T>(this DataGridView grid, T item)
        {
            for (var i = 0; i < grid.Rows.Count; i++)
            {
                var gridRow = grid.Rows[i];
                var tag = (T)gridRow.Tag;
                if (tag.Equals(item))
                {
                    return gridRow;
                }
            }
            return null;
        }

        public static DataGridViewTextBoxColumn GetTextBoxColumn(this DataGridView grid, int columnIndex)
        {
            return (DataGridViewTextBoxColumn)grid.Columns[columnIndex];
        }
        public static void SetColumnDataType(this DataGridView grid, Type type, string format, params int[] columnIndexes)
        {
            foreach (var colIndex in columnIndexes)
            {
                var column = grid.Columns[colIndex];
                var tbColumn = (column as DataGridViewTextBoxColumn);
                if (tbColumn == null)
                {
                    continue;
                }

                tbColumn.ValueType = type;
                if (format != null)
                {
                    tbColumn.DefaultCellStyle.Format = format;
                }
            }
        }
        public static void SetColumnDataType(this DataGridView grid, Type type, string format, params string[] columnNames)
        {
            var index = GetColumnIndexes(grid, columnNames);
            SetColumnDataType(grid, type, format, index);
        }

        public static DataGridViewRow GetRow(this DataGridViewCell gridCell)
        {
            if (gridCell.RowIndex == -1)
            {
                return null;
            }

            return gridCell.DataGridView.Rows[gridCell.RowIndex];
        }
        public static DataGridViewColumn GetColumn(this DataGridViewCell gridCell)
        {
            return gridCell.DataGridView.Columns[gridCell.ColumnIndex];
        }
        public static DataGridViewRow GetFirstRow(this DataGridView grid)
        {
            if (grid.Rows.Count == 0)
            {
                return null;
            }
            return grid.Rows[0];
        }
        public static DataGridViewRow GetLastRow(this DataGridView grid)
        {
            if (grid.Rows.Count == 0)
            {
                return null;
            }
            return grid.Rows[grid.Rows.Count - 1];
        }

        public static T GetValue<T>(this DataGridViewCell gridCell)
        {
            var value = Format.Convert<T>(gridCell.Value);
            return value;
        }
        public static T GetValue<T>(this DataGridViewRow gridRow, int columnIndex)
        {
            return GetValue<T>(gridRow.Cells[columnIndex]);
        }
        public static T GetValue<T>(this DataGridViewRow gridRow, string columnName)
        {
            return GetValue<T>(gridRow.Cells[columnName]);
        }

        public static void SelectElement(this DataGridView grid, int rowIndex, int? selectedColumn = null)
        {
            grid.ClearSelection();
            if (selectedColumn == null)
            {
                grid.Rows[rowIndex].Selected = true;
                var gridCell = grid[0, rowIndex];
                if (gridCell.Visible)
                {
                    grid.CurrentCell = gridCell;
                }
            }
            else
            {
                var gridCell = grid[selectedColumn.Value, rowIndex];
                if (gridCell.Visible)
                {
                    gridCell.Selected = true;
                    grid.CurrentCell = gridCell;
                }
            }
            grid.FirstDisplayedScrollingRowIndex = rowIndex;
        }
        public static void SelectNextRow(this DataGridView grid)
        {
            var index = grid.GetSelectedRowIndex();
            if (index != -1)
            {
                index++;
                if (index < grid.Rows.Count)
                {
                    grid.CurrentCell = grid[0, index];
                }
            }
        }
        public static void SelectPrevRow(this DataGridView grid)
        {
            var index = grid.GetSelectedRowIndex();
            if (index != -1)
            {
                index--;
                if (index >= 0)
                {
                    grid.CurrentCell = grid[0, index];
                }
            }
        }

        public static DataGridViewRowManager CreateRowManager(this DataGridView grid)
        {
            var manager = new DataGridViewRowManager(grid);
            return manager;
        }
        public static void Refresh(this DataGridView grid, Action updateListAction)
        {
            var selectedRowIndex = grid.GetSelectedRow()?.Index;
            var firstRowIndex = grid.FirstDisplayedScrollingRowIndex;

            grid.SuspendLayout();
            updateListAction();
            grid.ResumeLayout();

            if (selectedRowIndex != null && selectedRowIndex > 0)
            {
                if (selectedRowIndex >= grid.Rows.Count)
                {
                    selectedRowIndex = grid.Rows.Count - 1;
                }
                if (selectedRowIndex != -1)
                {
                    grid.SelectElement(selectedRowIndex.Value);
                }
            }
            if (firstRowIndex != -1 && firstRowIndex < grid.Rows.Count)
            {
                grid.FirstDisplayedScrollingRowIndex = firstRowIndex;
            }
        }
        public static void RefreshEditingControl(this DataGridView grid)
        {
            var gridCell = grid.CurrentCell;
            if (gridCell != null)
            {
                var editingControl = grid.EditingControl;
                if (editingControl != null)
                {
                    var value = gridCell.Value;
                    var text = (value == null) ? string.Empty : value.ToString();
                    if (!Format.IsEquals(editingControl.Text, text))
                    {
                        editingControl.Text = text;
                    }
                }
            }
        }
        public static bool CommitEdit(this DataGridView grid)
        {
            var gridCell = grid.GetSelectedCell();
            if (gridCell == null)
            {
                return false;
            }

            var editingControl = grid.EditingControl;
            if (editingControl == null)
            {
                return false;
            }

            var text = editingControl.Text;
            if (text == Format.ConvertToNotNullString(gridCell.Value))
            {
                return false;
            }

            var needCommit = true;
            if (text.Length > 0)
            {
                try
                {
                    Convert.ChangeType(text, gridCell.ValueType);
                }
                catch
                {
                    needCommit = false;
                }
            }
            if (needCommit)
            {
                var commit = grid.CommitEdit(DataGridViewDataErrorContexts.Commit);

                // Замена цвета ячейки
                var color = gridCell.Style.BackColor;
                if (color == Color.Empty)
                {
                    if (gridCell.RowIndex % 2 == 0)
                    {
                        color = grid.DefaultCellStyle.BackColor;
                    }
                    else
                    {
                        color = grid.AlternatingRowsDefaultCellStyle.BackColor;
                        if (color == Color.Empty)
                        {
                            color = grid.DefaultCellStyle.BackColor;
                        }
                    }
                }
                grid.EditingPanel.BackColor = color;
                editingControl.BackColor = color;

                return commit;
            }
            return false;
        }
        public static void ProcessDataError(this DataGridView grid, DataGridViewDataErrorEventArgs e)
        {
            var caption = $"Колонка \"{grid.Columns[e.ColumnIndex].HeaderText}\"";

            string text;
            if (e.Exception is FormatException)
            {
                text = "Введённое значение не соответствует формату ячейки.";
            }
            else
            {
                text = e.Exception.Message;
            }

            MsgBox.ShowError(text, caption);
        }


        private static int[] GetColumnIndexes(DataGridView grid, string[] columnNameArray)
        {
            var indexArray = new int[columnNameArray.Length];
            for (var i = 0; i < indexArray.Length; i++)
            {
                var columnName = columnNameArray[i];
                indexArray[i] = grid.Columns[columnName].Index;
            }
            return indexArray;
        }
    }
}