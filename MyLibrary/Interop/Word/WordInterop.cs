using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.Word
{
    public class WordInterop : IDisposable
    {
        public W.Application Application { get; set; }
        public W.Document Document { get; set; }

        public WordInterop()
        {
            _caption = Path.GetRandomFileName();

            Application = new W.Application();
            Application.Caption = _caption;
            Application.DisplayAlerts = W.WdAlertLevel.wdAlertsNone;
        }
        public void Dispose()
        {
            if (Document != null)
            {
                Marshal.ReleaseComObject(Document);
                Document = null;
            }
            if (Application != null)
            {
                Marshal.ReleaseComObject(Application);
                Application = null;
            }
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
                Document.Activate();
                Application.Activate();

                var processes = Process.GetProcessesByName("winword");
                processes = Array.FindAll(processes, x => x.MainWindowTitle.Contains(_caption));
                if (processes.Length > 0)
                {
                    NativeMethods.SetForegroundWindow(processes[0].Handle);
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

            var wFind = Document.Range().Find;
            wFind.ClearFormatting();
            wFind.Replacement.ClearFormatting();
            wFind.Forward = true;
            wFind.Wrap = W.WdFindWrap.wdFindContinue;
            wFind.Text = text;
            wFind.Replacement.Text = replaceText;
            wFind.Execute(
                Replace: W.WdReplace.wdReplaceAll);
        }

        public WordTable GetTable(int index)
        {
            var wTable = Document.Tables[index + 1];
            return new WordTable(wTable);
        }
        public int GetDocumentPagesCount()
        {
            return Document.ComputeStatistics(W.WdStatistic.wdStatisticPages, false);
        }

        private string _caption; // для идентификации процесса при установке фокуса на окно
    }
}
