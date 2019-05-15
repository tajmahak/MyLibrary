using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using E = Microsoft.Office.Interop.Excel;

namespace MyLibrary.Interop.Excel
{
    public class ExcelInterop : IDisposable
    {
        public E.Application Application { get; private set; }
        public E.Workbook Workbook { get; private set; }
        public E.Worksheet Worksheet { get; private set; }
        public int SheetsCount
        {
            get
            {
                return Workbook.Sheets.Count;
            }
        }
        public int SheetRowsCount
        {
            get
            {
                return Worksheet.UsedRange.Rows.Count;
            }
        }
        public int SheetColumnsCount
        {
            get
            {
                return Worksheet.UsedRange.Columns.Count;
            }
        }

        public ExcelInterop()
        {
            Application = new E.Application();
            SetVisibleMode(false);
            Application.UserControl = true;
            Application.DisplayAlerts = false;
        }
        public void Dispose()
        {
            if (Application != null)
            {
                Marshal.FinalReleaseComObject(Application);
            }
            if (Workbook != null)
            {
                Marshal.FinalReleaseComObject(Workbook);
            }
            if (Worksheet != null)
            {
                Marshal.FinalReleaseComObject(Worksheet);
            }
        }

        public void OpenWorkbook(string path, bool readOnly = false)
        {
            Workbook = Application.Workbooks.Open(
                Filename: Path.GetFullPath(path),
                ReadOnly: readOnly);
            OpenSheet(0);
        }
        public void CreateWorkbook(int sheetsCount = 1)
        {
            Application.SheetsInNewWorkbook = sheetsCount;
            Workbook = Application.Workbooks.Add();
            OpenSheet(0);
        }
        public void OpenSheet(int index)
        {
            Worksheet = (E.Worksheet)Workbook.Sheets[index + 1];
        }
        public void CloseApplication(bool saveChanges)
        {
            var process = GetApplicationProcess();

            Workbook?.Close(SaveChanges: saveChanges);
            Application.Workbooks.Close();
            Application.Quit();

            process?.Kill(); // процесс не завершается после закрытия приложения
        }
        public void SetSheetName(string name)
        {
            if (name.Length > 31)
            {
                name = name.Substring(0, 31);
            }
            Worksheet.Name = name;
        }
        public void SetVisibleMode(bool visible)
        {
            Application.Visible = visible;
            Application.ScreenUpdating = visible;

            if (visible)
            {
                Worksheet?.Activate();
                Workbook?.Activate();

                var process = GetApplicationProcess();
                if (process != null)
                {
                    NativeMethods.SetForegroundWindow(process.Handle);
                }
            }
        }
        public void SetCellValueFormat(ExcelCellValueFormatEnum format)
        {
            switch (format)
            {
                case ExcelCellValueFormatEnum.Text:
                    Worksheet.Cells.NumberFormat = "@"; break;
            }
        }
        public void AutoFitColumns()
        {
            Worksheet.Columns.AutoFit();
        }
        public ExcelRange GetRange(int rowIndex, int columnIndex, int rowsCount = 1, int columnsCount = 1)
        {
            var eCell1 = Worksheet.Cells[rowIndex + 1, columnIndex + 1];
            if (rowsCount == 1 && columnsCount == 1)
            {
                return new ExcelRange(eCell1);
            }
            else
            {
                var eCell2 = Worksheet.Cells[rowIndex + rowsCount, columnIndex + columnsCount];
                var eRange = Worksheet.Range[eCell1, eCell2];
                return new ExcelRange(eRange);
            }
        }
        public ExcelRange GetWorksheetUsedRange()
        {
            return new ExcelRange(Worksheet.UsedRange);
        }
        public Process GetApplicationProcess()
        {
            var handle = (IntPtr)Application.Hwnd;
            var processes = Process.GetProcessesByName("excel");
            processes = Array.FindAll(processes, x => x.MainWindowHandle == handle);
            if (processes.Length > 0)
            {
                return processes[0];
            }
            return null;
        }
    }
}
