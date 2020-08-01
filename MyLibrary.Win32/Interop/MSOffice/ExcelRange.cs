﻿using System;
using E = Microsoft.Office.Interop.Excel;

namespace MyLibrary.Win32.Interop.MSOffice
{
    public sealed class ExcelRange
    {
        public E.Range Range { get; private set; }
        public int RowsCount => Range.Rows.Count;
        public int ColumnsCount => Range.Columns.Count;

        public ExcelRange(E.Range eRange)
        {
            Range = eRange;
        }

        public void SetBorder(int weight = 2)
        {
            E.Borders eBorder = Range.Borders;
            eBorder.Weight = weight;
        }
        public void SetBorder(int weight, ExcelBorderEnum border)
        {
            E.Borders eBorder = Range.Borders;
            if (border.HasFlag(ExcelBorderEnum.Top))
            {
                eBorder[E.XlBordersIndex.xlEdgeTop].LineStyle = E.XlLineStyle.xlContinuous;
                eBorder[E.XlBordersIndex.xlEdgeTop].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Bottom))
            {
                eBorder[E.XlBordersIndex.xlEdgeBottom].LineStyle = E.XlLineStyle.xlContinuous;
                eBorder[E.XlBordersIndex.xlEdgeBottom].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Left))
            {
                eBorder[E.XlBordersIndex.xlEdgeLeft].LineStyle = E.XlLineStyle.xlContinuous;
                eBorder[E.XlBordersIndex.xlEdgeLeft].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Right))
            {
                eBorder[E.XlBordersIndex.xlEdgeRight].LineStyle = E.XlLineStyle.xlContinuous;
                eBorder[E.XlBordersIndex.xlEdgeRight].Weight = weight;
            }
        }
        public void SetFont(int size = -1, bool bold = false)
        {
            Range.Font.Bold = bold;
            if (size != -1)
            {
                Range.Font.Size = size;
            }
        }
        public void SetAlignment(HorizontalAlignmentEnum horizontalAlignment = HorizontalAlignmentEnum.Left, VerticalAlignmentEnum verticalAlignment = VerticalAlignmentEnum.Top)
        {
            E.Style eStyle = (E.Style)Range.Style;
            switch (horizontalAlignment)
            {
                case HorizontalAlignmentEnum.Left:
                    eStyle.HorizontalAlignment = E.XlHAlign.xlHAlignLeft; break;
                case HorizontalAlignmentEnum.Center:
                    eStyle.HorizontalAlignment = E.XlHAlign.xlHAlignCenter; break;
                case HorizontalAlignmentEnum.Justify:
                    eStyle.HorizontalAlignment = E.XlHAlign.xlHAlignJustify; break;
                case HorizontalAlignmentEnum.Right:
                    eStyle.HorizontalAlignment = E.XlHAlign.xlHAlignRight; break;
            }
            switch (verticalAlignment)
            {
                case VerticalAlignmentEnum.Top:
                    eStyle.VerticalAlignment = E.XlVAlign.xlVAlignTop; break;
                case VerticalAlignmentEnum.Center:
                    eStyle.VerticalAlignment = E.XlVAlign.xlVAlignCenter; break;
                case VerticalAlignmentEnum.Bottom:
                    eStyle.VerticalAlignment = E.XlVAlign.xlVAlignBottom; break;
            }
        }
        public void Merge()
        {
            Range.Merge();
        }
        public void SetValues(object[,] values)
        {
            Range.set_Value(E.XlRangeValueDataType.xlRangeValueDefault, values);
        }
        public void SetCellValueFormat(ExcelCellValueFormatEnum format)
        {
            switch (format)
            {
                case ExcelCellValueFormatEnum.Text:
                    Range.Cells.NumberFormat = "@"; break;
            }
        }
        public object[,] GetValues()
        {
            object[,] eValues = (object[,])Range.Value2;

            // Тип полученного массива отличается от стандартного  типа object[,]
            int length0 = eValues.GetLength(0);
            int length1 = eValues.GetLength(1);
            object[,] values = new object[length0, length1];
            Array.Copy(eValues, values, length0 * length1);

            return values;
        }
    }
}