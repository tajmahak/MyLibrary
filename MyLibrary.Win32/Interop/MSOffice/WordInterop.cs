using MyLibrary.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.MSOffice
{
    public sealed class WordInterop : IDisposable
    {
        public W.Application Application { get; private set; }
        public W.Document Document { get; private set; }

        public WordInterop()
        {
            _caption = Path.GetRandomFileName();

            Application = new W.Application();
            Application.Caption = _caption;
            Application.DisplayAlerts = W.WdAlertLevel.wdAlertsNone;
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                if (Application != null)
                {
                    Marshal.ReleaseComObject(Application);
                    Application = null;
                }
                if (Document != null)
                {
                    Marshal.ReleaseComObject(Document);
                    Document = null;
                }
                _disposed = true;
            }
        }
        ~WordInterop()
        {
            Dispose();
        }

        public void OpenDocument(string path)
        {
            Document = Application.Documents.Open(
                FileName: Path.GetFullPath(path),
                ReadOnly: false);
        }
        public void CloseApplication(bool saveChanges)
        {
            Application.Quit(
                SaveChanges: saveChanges);
        }
        public void SetVisibleMode(bool visible)
        {
            Application.ActiveWindow.View.Type = W.WdViewType.wdPrintView;
            Application.ScreenUpdating = visible;
            Application.Visible = visible;
            if (visible)
            {
                Document?.Activate();
                Application.Activate();

                Process process = GetApplicationProcess();
                if (process != null)
                {
                    NativeMethods.SetForegroundWindow(process.Handle);
                }
            }
        }
        public void Print(string pageRange = null)
        {
            if (pageRange == null)
            {
                Application.PrintOut(
                    Range: W.WdPrintOutRange.wdPrintAllDocument);
            }
            else
            {
                Application.PrintOut(
                    Range: W.WdPrintOutRange.wdPrintRangeOfPages,
                    Pages: pageRange);
            }
        }
        public void ReplaceText(string text, string replaceText)
        {
            replaceText = replaceText ?? string.Empty;

            W.Find wFind = Document.Range().Find;
            wFind.ClearFormatting();
            wFind.Replacement.ClearFormatting();
            wFind.Forward = true;
            wFind.Wrap = W.WdFindWrap.wdFindContinue;
            wFind.Text = text;
            wFind.Replacement.Text = replaceText;
            wFind.Execute(
                Replace: W.WdReplace.wdReplaceAll);
        }
        public void ReplaceText(object text, object replaceText)
        {
            ReplaceText(Format.Convert<string>(text), Format.Convert<string>(replaceText));
        }
        public WordTable GetTable(int index)
        {
            W.Table wTable = Document.Tables[index + 1];
            return new WordTable(wTable, Document);
        }
        public int GetDocumentPagesCount()
        {
            return Document.ComputeStatistics(W.WdStatistic.wdStatisticPages, false);
        }
        public Process GetApplicationProcess()
        {
            Process[] processes = Process.GetProcessesByName("winword");
            processes = Array.FindAll(processes, x => x.MainWindowTitle.Contains(_caption));
            if (processes.Length > 0)
            {
                return processes[0];
            }
            return null;
        }

        private readonly string _caption; // для идентификации процесса при установке фокуса на окно
        private bool _disposed;
    }
}
