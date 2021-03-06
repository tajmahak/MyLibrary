﻿using MyLibrary.Win32.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace MyLibrary.MSOffice
{
    public sealed class ExcelInterop : IDisposable
    {
        public ExcelInterop()
        {
            Application = new Excel.Application();
            SetVisibleMode(false);
            Application.UserControl = true;
            Application.DisplayAlerts = false;
        }

        public Excel.Application Application { get; private set; }
        public Excel.Workbook Workbook { get; private set; }
        public Excel.Worksheet Worksheet { get; private set; }
        public int SheetsCount => Workbook.Sheets.Count;
        public int SheetRowsCount => Worksheet.UsedRange.Rows.Count;
        public int SheetColumnsCount => Worksheet.UsedRange.Columns.Count;
        private bool disposed;

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
            Worksheet = (Excel.Worksheet)Workbook.Sheets[index + 1];
        }

        public void CloseApplication(bool saveChanges)
        {
            Process process = GetApplicationProcess();

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

                Process process = GetApplicationProcess();
                if (process != null)
                {
                    Native.SetForegroundWindow(process.Handle);
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
            dynamic eCell1 = Worksheet.Cells[rowIndex + 1, columnIndex + 1];
            if (rowsCount == 1 && columnsCount == 1)
            {
                return new ExcelRange(eCell1);
            }
            else
            {
                dynamic eCell2 = Worksheet.Cells[rowIndex + rowsCount, columnIndex + columnsCount];
                dynamic eRange = Worksheet.Range[eCell1, eCell2];
                return new ExcelRange(eRange);
            }
        }

        public ExcelRange GetWorksheetUsedRange()
        {
            return new ExcelRange(Worksheet.UsedRange);
        }

        public Process GetApplicationProcess()
        {
            IntPtr handle = (IntPtr)Application.Hwnd;
            Process[] processes = Process.GetProcessesByName("excel");
            processes = Array.FindAll(processes, x => x.MainWindowHandle == handle);
            if (processes.Length > 0)
            {
                return processes[0];
            }
            return null;
        }

        public void Dispose()
        {
            if (!disposed)
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
                disposed = true;
            }
        }


        ~ExcelInterop()
        {
            Dispose();
        }
    }
}
