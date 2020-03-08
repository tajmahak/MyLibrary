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
        public int Timeout { get; set; }
        public string Referer { get; set; }
        public string UserAgent { get; set; }
        public HttpWebRequest Request { get; private set; }
        public HttpWebResponse Response { get; private set; }
        public CookieContainer Cookies { get; private set; } = new CookieContainer();

        public event EventHandler BeforeGetResponse;
        public event EventHandler AfterGetResponse;
        public event EventHandler<HttpConnectionDataReceived> DataReceived;

        public HttpConnection()
        {
            Timeout = 100000;
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

        public void AddHeader(string name, string value)
        {
            if (_webHeaderCollection == null)
            {
                _webHeaderCollection = new WebHeaderCollection();
            }
            _webHeaderCollection.Add(name, value);
        }
        public void AddRange(long? from = null, long? to = null)
        {
            _rangeFrom = from;
            _rangeTo = to;
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

                var args = new HttpConnectionDataReceived
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

                        if (DataReceived != null)
                        {
                            args.BytesReceived = bytesReceived;
                            args.TotalBytesToReceive = totalBytesToReceive;
                            DataReceived(this, args);
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
                Request.Headers["Accept-Encoding"] = "gzip, deflate";
                Request.Headers["Accept-Language"] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";

                if (PostDataContent == null)
                {
                    Request.Method = UseHeadRequest ? "HEAD" : "GET";
                }
                else
                {
                    Request.Method = "POST";
                }

                if (_webHeaderCollection != null)
                {
                    foreach (string name in _webHeaderCollection)
                    {
                        var value = _webHeaderCollection[name];
                        Request.Headers.Add(name, value);
                    }
                }
                if (_rangeFrom != null || _rangeTo != null)
                {
                    if (_rangeTo != null)
                    {
                        Request.AddRange(_rangeFrom.Value, _rangeTo.Value);
                    }
                    else
                    {
                        Request.AddRange(_rangeFrom.Value);
                    }
                }

                #endregion

                BeforeGetResponse?.Invoke(this, EventArgs.Empty);

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

                AfterGetResponse?.Invoke(this, EventArgs.Empty);

                getDataAction?.Invoke();

                PostDataContent = null;
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
        private WebHeaderCollection _webHeaderCollection;
        private long? _rangeFrom, _rangeTo;
    }

    public class HttpConnectionDataReceived : EventArgs
    {
        public bool Cancel { get; set; }
        public int BytesReceived { get; internal set; }
        public long TotalBytesToReceive { get; internal set; }
        public long ContentLength { get; internal set; }
    }
}
