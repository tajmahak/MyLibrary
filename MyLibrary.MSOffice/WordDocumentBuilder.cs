using MyLibrary.Threading;
using System;
using System.Threading;

namespace MyLibrary.MSOffice
{
    public class WordDocumentBuilder
    {
        public WordDocumentBuilder(string documentPath)
        {
            this.documentPath = documentPath;
        }

        public event EventHandler<WordDocumentBuilderMessageEventArgs> Message;


        private WordInterop word;
        private readonly string documentPath;


        public void Build(Action<WordInterop> prepareDocumentAction)
        {
            try
            {
                OnMessage("Подготовка документа...", false);

                word = new WordInterop();
                word.OpenDocument(documentPath);

                prepareDocumentAction(word);

                OnMessage(string.Empty, false);
                word?.Dispose();
            }
            catch (Exception ex)
            {
                OnMessage($"Вывод документа в Microsoft Word невозможен.\r\n{ex.Message}", true);
                word?.Dispose();
            }
        }

        public Thread BuildAsync(Action<WordInterop> action)
        {
            return ThreadExtension.StartBackgroundThread(() =>
            {
                Build(action);
            });
        }

        public void Open()
        {
            WriteMessage("Открытие документа...");
            word.SetVisibleMode(true);
        }

        public void Print()
        {
            WriteMessage("Вывод документа на печать...");
            word.Print();
            word.CloseApplication(true); // без сохранения док-та Word закрывается раньше, чем успевает вывести док-т на печать
        }

        public void WriteMessage(string message)
        {
            OnMessage(message, false);
        }

        public void WriteTableMessage(int index, int count)
        {
            OnMessage($"Подготовка таблицы документа ({index + 1}/{count} строк)...", false);
        }


        private void OnMessage(string message, bool isError)
        {
            Message?.Invoke(this, new WordDocumentBuilderMessageEventArgs()
            {
                MessageText = message,
                IsError = isError,
            });
        }
    }

    public class WordDocumentBuilderMessageEventArgs : EventArgs
    {
        public bool IsError { get; internal set; }
        public string MessageText { get; internal set; }
    }
}