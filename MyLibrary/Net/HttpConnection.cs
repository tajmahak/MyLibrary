using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace MyLibrary.Net
{
    public class HttpConnection : IDisposable
    {
        public string RequestUri { get; set; }
        public IPostDataContent PostDataContent { get; set; }
        public bool UseHeadRequest { get; set; }
        public int Timeout { get; set; } = 100000;
        public string Referer { get; set; }
        public string UserAgent { get; set; }
        public long? StartRange { get; set; }
        public long? EndRange { get; set; }
        public HttpWebRequest Request { get; private set; }
        public HttpWebResponse Response { get; private set; }
        public WebHeaderCollection Headers { get; private set; } = new WebHeaderCollection();
        public CookieContainer Cookies { get; private set; } = new CookieContainer();

        public event EventHandler CreatingRequest;
        public event EventHandler ResponseReceived;
        public event EventHandler<ResponseDataReceivedEventArgs> ResponseDataReceived;

        public HttpConnection()
        {
            Headers.Add("Accept-Encoding", "gzip, deflate");
            Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
        }
        public HttpConnection(string requestUri)
            : this()
        {
            RequestUri = requestUri;
        }
        public void Dispose()
        {
            Response?.Close();
        }

        public string GetString()
        {
            string data = null;
            GetWebData(() => data = GetStringFromResponse(Response));
            return data;
        }
        public void GetData(Stream outputStream)
        {
            GetWebData(() =>
            {
                var contentLength = Response.ContentLength;
                var knownContentLength = (contentLength != -1);

                var args = new ResponseDataReceivedEventArgs
                {
                    ContentLength = contentLength
                };

                var buffer = new byte[0x40000]; // размер буфера 256 КБ
                using (var stream = GetResponseStream())
                {
                    long totalBytesToReceive = 0;
                    int bytesReceived;
                    do
                    {
                        bytesReceived = stream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, bytesReceived);
                        totalBytesToReceive += bytesReceived;

                        if (ResponseDataReceived != null)
                        {
                            args.ReceivedBytesCount = bytesReceived;
                            args.TotalBytesToReceive = totalBytesToReceive;
                            ResponseDataReceived(this, args);
                            if (args.Cancel)
                            {
                                break;
                            }
                        }
                    } while ((knownContentLength && totalBytesToReceive < contentLength) || (!knownContentLength && bytesReceived > 0));
                }
            });
        }
        public void GetResponse()
        {
            GetWebData(null);
        }
        public Stream GetResponseStream()
        {
            return GetStreamFromResponse(Response);
        }

        #region Статические сущности

        public static string GetStringFromResponse(HttpWebResponse response)
        {
            var encoding = string.IsNullOrEmpty(response.CharacterSet) ?
                   Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet);

            using (var stream = GetStreamFromResponse(response))
            using (var reader = new StreamReader(stream, encoding))
            {
                return reader.ReadToEnd();
            }
        }
        public static Stream GetStreamFromResponse(HttpWebResponse response)
        {
            var stream = response.GetResponseStream();

            // Выбор метода распаковки потока
            switch (response.ContentEncoding)
            {
                case "gzip":
                    stream = new GZipStream(stream, CompressionMode.Decompress, false); break;
                case "deflate":
                    stream = new DeflateStream(stream, CompressionMode.Decompress, false); break;
            }
            return stream;
        }
        public static string GetString(string requestUri, IPostDataContent postData = null)
        {
            using (var connection = new HttpConnection(requestUri))
            {
                connection.PostDataContent = postData;
                return connection.GetString();
            }
        }
        public static void GetData(Stream outputStream, string requestUri, IPostDataContent postData = null)
        {
            using (var connection = new HttpConnection(requestUri))
            {
                connection.PostDataContent = postData;
                connection.GetData(outputStream);
            }
        }

        #endregion

        private void GetWebData(Action getDataAction)
        {
            Request = null;
            Response?.Close();
            Response = null;
            try
            {
                Request = (HttpWebRequest)WebRequest.Create(RequestUri);

                #region Настройка Web-запроса

                Request.CookieContainer = Cookies;
                Request.KeepAlive = true;
                Request.Timeout = Timeout;
                Request.Referer = Referer;
                Request.UserAgent = UserAgent;
                Request.Accept = "*/*";
                foreach (string name in Headers)
                {
                    var value = Headers[name];
                    Request.Headers.Add(name, value);
                }
                Request.Method = PostDataContent == null ? (UseHeadRequest ? "HEAD" : "GET") : "POST";

                if (StartRange != null || EndRange != null)
                {
                    if (EndRange != null)
                    {
                        Request.AddRange(StartRange.Value, EndRange.Value);
                    }
                    else
                    {
                        Request.AddRange(StartRange.Value);
                    }
                }

                #endregion

                CreatingRequest?.Invoke(this, EventArgs.Empty);

                if (PostDataContent != null)
                {
                    // Отправка POST-данных запроса на сервер
                    var content = PostDataContent.GetContent();
                    Request.ContentType = PostDataContent.GetContentType();
                    Request.ContentLength = content.Length;
                    using (var requestStream = Request.GetRequestStream())
                    {
                        requestStream.Write(content, 0, content.Length);
                    }
                }

                Response = (HttpWebResponse)Request.GetResponse();

                ResponseReceived?.Invoke(this, EventArgs.Empty);

                getDataAction?.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    Response = (HttpWebResponse)(ex as WebException).Response;
                }
                throw;
            }
        }
    }

    public class ResponseDataReceivedEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
        public int ReceivedBytesCount { get; internal set; }
        public long TotalBytesToReceive { get; internal set; }
        public long ContentLength { get; internal set; }
    }
}
