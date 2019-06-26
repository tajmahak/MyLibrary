using MyLibrary.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    public static class DataGridViewExtension
    {
        public static void Refresh(this DataGridView grid, Action updateListAction)
        {
            var selectedRowIndex = grid.GetSelectedRow()?.Index;
            var firstRowIndex = grid.FirstDisplayedScrollingRowIndex;

            updateListAction();

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
        public static DataGridViewRowManager CreateRowManager(this DataGridView grid)
        {
            var manager = new DataGridViewRowManager(grid);
            return manager;
        }
        public static void SelectNextRow(this DataGridView grid)
        {
            var index = grid.GetSelectedRowIndex();
            if (index == -1)
            {
                return;
            }

            index++;
            if (index < grid.Rows.Count)
            {
                grid.CurrentCell = grid[0, index];
            }
        }
        public static void SelectPrevRow(this DataGridView grid)
        {
            var index = grid.GetSelectedRowIndex();
            if (index == -1)
            {
                return;
            }

            index--;
            if (index >= 0)
            {
                grid.CurrentCell = grid[0, index];
            }
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
            var index = GetColumnIndex(grid, columnNames);
            SetColumnDataType(grid, type, format, index);
        }

        public static void SetColumnSortComparer(this DataGridView grid, int columnIndex, Comparison<DataGridViewCell> comparer)
        {
            grid.SortCompare += (sender, e) =>
            {
                if (e.Column.Index != columnIndex)
                {
                    return;
                }

                var gridCell1 = grid[columnIndex, e.RowIndex1];
                var gridCell2 = grid[columnIndex, e.RowIndex2];
                e.SortResult = comparer(gridCell1, gridCell2);
                e.Handled = true;
            };
        }
        public static void SetColumnSortComparer(this DataGridView grid, string columnName, Comparison<DataGridViewCell> comparer)
        {
            var index = GetColumnIndex(grid, columnName);
            SetColumnSortComparer(grid, index, comparer);
        }

        public static void Sort(this DataGridView grid, int columnIndex, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            grid.Sort(grid.Columns[columnIndex], sortDirection);
        }
        public static void Sort(this DataGridView grid, string columnName, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var index = GetColumnIndex(grid, columnName);
            Sort(grid, index, sortDirection);
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
        public static void SelectElement(this DataGridView grid, int rowIndex, string selectedColumnName)
        {
            var index = GetSelectedRowIndex(grid);
            SelectElement(grid, rowIndex, index);
        }

        public static void RefreshCellValue(this DataGridView grid)
        {
            var gridCell = grid.CurrentCell;
            if (gridCell == null)
            {
                return;
            }

            var editingControl = grid.EditingControl;
            if (editingControl != null)
            {
                var value = gridCell.Value;
                value = value ?? string.Empty;

                if (!Format.IsEquals(editingControl.Text, value.ToString()))
                {
                    editingControl.Text = value.ToString();
                }
            }
        }

        public static bool Search(this DataGridView grid, int columnIndex, string value, int startRowIndex = 0, bool precision = false)
        {
            var pattern = value.Trim().ToUpperInvariant();
            #region 1) Поиск по совпадению целой строки

            for (var i = startRowIndex; i < grid.Rows.Count; i++)
            {
                var gridValue = Get<string>(grid.Rows[i], columnIndex, false);
                gridValue = gridValue.ToUpperInvariant();
                if (gridValue == pattern)
                {
                    grid.CurrentCell = grid.Rows[i].Cells[columnIndex];
                    return true;
                }
            }

            #endregion
            if (!precision)
            {
                #region 2) Поиск строки по начальному вхождению
                for (var i = startRowIndex; i < grid.Rows.Count; i++)
                {
                    var gridValue = Get<string>(grid.Rows[i], columnIndex, false);
                    gridValue = gridValue.ToUpperInvariant();
                    if (gridValue.IndexOf(pattern) == 0)
                    {
                        grid.CurrentCell = grid.Rows[i].Cells[columnIndex];
                        return true;
                    }
                }
                #endregion
            }
            return false;
        }
        public static bool Search(this DataGridView grid, string columnName, string value, int startRowIndex = 0, bool precision = false)
        {
            var index = grid.Columns[columnName].Index;
            return Search(grid, index, value, startRowIndex, precision);
        }

        public static DataGridViewRow FirstRow(this DataGridView grid)
        {
            if (grid.Rows.Count == 0)
            {
                return null;
            }

            return grid.Rows[0];
        }
        public static DataGridViewRow LastRow(this DataGridView grid)
        {
            if (grid.Rows.Count == 0)
            {
                return null;
            }

            return grid.Rows[grid.Rows.Count - 1];
        }

        public static int GetSelectedRowIndex(this DataGridView grid)
        {
            if (grid.SelectedCells.Count == 0)
            {
                return -1;
            }

            return grid.SelectedCells[0].RowIndex;
        }
        public static DataGridViewRow GetSelectedRow(this DataGridView grid)
        {
            var index = GetSelectedRowIndex(grid);
            if (index == -1)
            {
                return null;
            }

            return grid.Rows[index];
        }
        public static DataGridViewCell GetSelectedCell(this DataGridView grid)
        {
            return grid.CurrentCell;
        }
        public static DataGridViewColumn GetColumn(this DataGridViewCell gridCell)
        {
            return gridCell.DataGridView.Columns[gridCell.ColumnIndex];
        }
        public static DataGridViewRow GetRow(this DataGridViewCell gridCell)
        {
            if (gridCell.RowIndex == -1)
            {
                return null;
            }

            return gridCell.DataGridView.Rows[gridCell.RowIndex];
        }
        public static DataGridViewTextBoxColumn GetTextBoxColumn(this DataGridView grid, string columnName)
        {
            return (DataGridViewTextBoxColumn)grid.Columns[columnName];
        }
        public static DataGridViewTextBoxColumn GetTextBoxColumn(this DataGridView grid, int columnIndex)
        {
            return (DataGridViewTextBoxColumn)grid.Columns[columnIndex];
        }
        public static DataGridViewRow[] GetSelectedRows(this DataGridView grid)
        {
            var hashSet = new HashSet<int>();
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

        public static T GetSelectedTag<T>(this DataGridView grid)
        {
            var gridRow = grid.GetSelectedRow();
            if (gridRow == null)
            {
                return default;
            }

            return GetTag<T>(gridRow);
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

        public static T Get<T>(this DataGridViewCell gridCell, bool allowNullString = true)
        {
            var value = Format.Convert<T>(gridCell.Value);
            if (!allowNullString && typeof(T) == typeof(string))
            {
                value = (T)((object)Format.GetNotEmptyString(value));
            }
            return value;
        }
        public static T Get<T>(this DataGridViewRow gridRow, int columnIndex, bool allowNullString = true)
        {
            return Get<T>(gridRow.Cells[columnIndex], allowNullString);
        }
        public static T Get<T>(this DataGridViewRow gridRow, string columnName, bool allowNullString = true)
        {
            return Get<T>(gridRow.Cells[columnName], allowNullString);
        }

        public static void TryDataError(this DataGridView grid, DataGridViewDataErrorEventArgs e)
        {
            var caption = string.Format("Колонка \"{0}\"", grid.Columns[e.ColumnIndex].HeaderText);

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

        public static bool CheckEmptyCellsFilter(this DataGridView grid, Predicate<int> filter, params int[] columnIndexes)
        {
            for (var i = 0; i < grid.Rows.Count; i++)
            {
                #region [if] Фильтр

                if (filter != null)
                {
                    if (filter(i))
                    {
                        continue;
                    }
                }

                #endregion

                for (var j = 0; j < columnIndexes.Length; j++)
                {
                    var columnIndex = columnIndexes[j];

                    var gridCell = grid[columnIndex, i];
                    if (Format.IsEmpty(gridCell.Value))
                    {
                        MsgBox.ShowError(string.Format("Не заполнено значение ячейки \"{0}\".", grid.Columns[columnIndex].HeaderText));
                        grid.ClearSelection();
                        grid.CurrentCell = gridCell;
                        grid.Parent.Focus();
                        grid.Focus();
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool CheckEmptyCellsFilter(this DataGridView grid, Predicate<int> filter, params string[] columnNames)
        {
            var index = GetColumnIndex(grid, columnNames);
            return CheckEmptyCellsFilter(grid, filter, index);
        }
        public static bool CheckEmptyCellsFilter(this DataGridView grid, Predicate<DataGridViewRow> filter, params int[] columnIndexes)
        {
            bool overrideFilter(int x)
            {
                return filter(grid.Rows[x]);
            }

            return CheckEmptyCellsFilter(grid, overrideFilter, columnIndexes);
        }
        public static bool CheckEmptyCellsFilter(this DataGridView grid, Predicate<DataGridViewRow> filter, params string[] columnNames)
        {
            var index = GetColumnIndex(grid, columnNames);
            return CheckEmptyCellsFilter(grid, filter, index);
        }


        public static int AddRow(this DataGridView grid, DataGridViewRow gridRow, int InsertIndex = -1, int EditColumnIndex = -1)
        {
            if (gridRow.Index == -1)
            {
                if (InsertIndex == -1)
                {
                    grid.Rows.Add(gridRow);
                }
                else
                {
                    grid.Rows.Insert(InsertIndex, gridRow);
                }
            }

            if (EditColumnIndex != -1)
            {
                grid.CurrentCell = gridRow.Cells[EditColumnIndex];
                grid.BeginEdit(true);
            }
            grid.FirstDisplayedScrollingRowIndex = gridRow.Index;

            return gridRow.Index;
        }

        private static int[] GetColumnIndex(DataGridView grid, string[] columnNameArray)
        {
            var indexArray = new int[columnNameArray.Length];
            for (var i = 0; i < indexArray.Length; i++)
            {
                var columnName = columnNameArray[i];
                indexArray[i] = grid.Columns[columnName].Index;
            }
            return indexArray;
        }
        private static int GetColumnIndex(DataGridView grid, string columnName)
        {
            return grid.Columns[columnName].Index;
        }
    }
}