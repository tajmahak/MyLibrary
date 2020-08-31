using System;
using Excel = Microsoft.Office.Interop.Excel;

namespace MyLibrary.MSOffice
{
    public sealed class ExcelRange
    {
        public ExcelRange(Excel.Range eRange)
        {
            Range = eRange;
        }

        public Excel.Range Range { get; private set; }
        public int RowsCount => Range.Rows.Count;
        public int ColumnsCount => Range.Columns.Count;

        public void SetBorder(int weight = 2)
        {
            Excel.Borders eBorder = Range.Borders;
            eBorder.Weight = weight;
        }

        public void SetBorder(int weight, ExcelBorderEnum border)
        {
            Excel.Borders eBorder = Range.Borders;
            if (border.HasFlag(ExcelBorderEnum.Top))
            {
                eBorder[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlContinuous;
                eBorder[Excel.XlBordersIndex.xlEdgeTop].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Bottom))
            {
                eBorder[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
                eBorder[Excel.XlBordersIndex.xlEdgeBottom].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Left))
            {
                eBorder[Excel.XlBordersIndex.xlEdgeLeft].LineStyle = Excel.XlLineStyle.xlContinuous;
                eBorder[Excel.XlBordersIndex.xlEdgeLeft].Weight = weight;
            }
            if (border.HasFlag(ExcelBorderEnum.Right))
            {
                eBorder[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
                eBorder[Excel.XlBordersIndex.xlEdgeRight].Weight = weight;
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
            Excel.Style eStyle = (Excel.Style)Range.Style;
            switch (horizontalAlignment)
            {
                case HorizontalAlignmentEnum.Left:
                    eStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft; break;
                case HorizontalAlignmentEnum.Center:
                    eStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter; break;
                case HorizontalAlignmentEnum.Justify:
                    eStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignJustify; break;
                case HorizontalAlignmentEnum.Right:
                    eStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight; break;
            }
            switch (verticalAlignment)
            {
                case VerticalAlignmentEnum.Top:
                    eStyle.VerticalAlignment = Excel.XlVAlign.xlVAlignTop; break;
                case VerticalAlignmentEnum.Center:
                    eStyle.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter; break;
                case VerticalAlignmentEnum.Bottom:
                    eStyle.VerticalAlignment = Excel.XlVAlign.xlVAlignBottom; break;
            }
        }

        public void Merge()
        {
            Range.Merge();
        }

        public void SetValues(object[,] values)
        {
            Range.set_Value(Excel.XlRangeValueDataType.xlRangeValueDefault, values);
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
