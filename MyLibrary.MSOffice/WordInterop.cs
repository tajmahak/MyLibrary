using MyLibrary.Win32.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

namespace MyLibrary.MSOffice
{
    public sealed class WordInterop : IDisposable
    {
        public WordInterop()
        {
            caption = Path.GetRandomFileName();

            Application = new Word.Application();
            Application.Caption = caption;
            Application.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;
        }

        public Word.Application Application { get; private set; }
        public Word.Document Document { get; private set; }
        private readonly string caption; // для идентификации процесса при установке фокуса на окно
        private bool disposed;

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
            Application.ActiveWindow.View.Type = Word.WdViewType.wdPrintView;
            Application.ScreenUpdating = visible;
            Application.Visible = visible;
            if (visible)
            {
                Document?.Activate();
                Application.Activate();

                Process process = GetApplicationProcess();
                if (process != null)
                {
                    Native.SetForegroundWindow(process.Handle);
                }
            }
        }

        public void Print(string pageRange = null)
        {
            if (pageRange == null)
            {
                Application.PrintOut(
                    Range: Word.WdPrintOutRange.wdPrintAllDocument);
            }
            else
            {
                Application.PrintOut(
                    Range: Word.WdPrintOutRange.wdPrintRangeOfPages,
                    Pages: pageRange);
            }
        }

        public void ReplaceText(string text, string replaceText)
        {
            replaceText = replaceText ?? string.Empty;

            Word.Find wFind = Document.Range().Find;
            wFind.ClearFormatting();
            wFind.Replacement.ClearFormatting();
            wFind.Forward = true;
            wFind.Wrap = Word.WdFindWrap.wdFindContinue;
            wFind.Text = text;
            wFind.Replacement.Text = replaceText;
            wFind.Execute(
                Replace: Word.WdReplace.wdReplaceAll);
        }

        public void ReplaceText(object text, object replaceText)
        {
            ReplaceText(Data.Convert<string>(text), Data.Convert<string>(replaceText));
        }

        public WordTable GetTable(int index)
        {
            Word.Table wTable = Document.Tables[index + 1];
            return new WordTable(wTable, Document);
        }

        public int GetDocumentPagesCount()
        {
            return Document.ComputeStatistics(Word.WdStatistic.wdStatisticPages, false);
        }

        public Process GetApplicationProcess()
        {
            Process[] processes = Process.GetProcessesByName("winword");
            processes = Array.FindAll(processes, x => x.MainWindowTitle.Contains(caption));
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
                    Marshal.ReleaseComObject(Application);
                    Application = null;
                }
                if (Document != null)
                {
                    Marshal.ReleaseComObject(Document);
                    Document = null;
                }
                disposed = true;
            }
        }


        ~WordInterop()
        {
            Dispose();
        }
    }
}
