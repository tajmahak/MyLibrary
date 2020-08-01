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
        public string Accept { get; set; } = "*/*";
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
            GetWebData(() =>
            {
                data = GetStringFromResponse(Response);
            });
            return data;
        }
        public void GetData(Stream outputStream)
        {
            GetWebData(() =>
            {
                long contentLength = Response.ContentLength;
                bool knownContentLength = (contentLength != -1);

                byte[] buffer = new byte[BUFFER_SIZE];
                using (Stream stream = GetResponseStream())
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
                            ResponseDataReceivedEventArgs args = new ResponseDataReceivedEventArgs
                            {
                                ContentLength = contentLength,
                                ReceivedBytesCount = bytesReceived,
                                TotalBytesToReceive = totalBytesToReceive,
                            };
                            ResponseDataReceived.Invoke(this, args);
                            if (args.Cancel)
                            {
                                break;
                            }
                        }
                    } while ((knownContentLength && totalBytesToReceive < contentLength) || (!knownContentLength && bytesReceived > 0));
                }
            });
        }
        public HttpWebResponse GetResponse()
        {
            GetWebData(null);
            return Response;
        }
        public Stream GetResponseStream()
        {
            return GetStreamFromResponse(Response);
        }

        public static string GetStringFromResponse(HttpWebResponse response)
        {
            Encoding encoding = string.IsNullOrEmpty(response.CharacterSet) ?
                   Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet);

            using (Stream stream = GetStreamFromResponse(response))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                return reader.ReadToEnd();
            }
        }
        public static Stream GetStreamFromResponse(HttpWebResponse response)
        {
            Stream stream = response.GetResponseStream();

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
            using (HttpConnection connection = new HttpConnection(requestUri))
            {
                connection.PostDataContent = postData;
                return connection.GetString();
            }
        }
        public static void GetData(Stream outputStream, string requestUri, IPostDataContent postData = null)
        {
            using (HttpConnection connection = new HttpConnection(requestUri))
            {
                connection.PostDataContent = postData;
                connection.GetData(outputStream);
            }
        }

        private void GetWebData(Action getDataAction)
        {
            Request = null;
            Response?.Close();
            Response = null;
            try
            {
                Request = (HttpWebRequest)WebRequest.Create(RequestUri);
                Request.CookieContainer = Cookies;
                Request.KeepAlive = true;
                Request.Timeout = Timeout;
                Request.Referer = Referer;
                Request.UserAgent = UserAgent;
                Request.Accept = Accept;
                Request.Method = PostDataContent == null ? (UseHeadRequest ? "HEAD" : "GET") : "POST";

                foreach (string headerName in Headers)
                {
                    string headerValue = Headers[headerName];
                    Request.Headers.Add(headerName, headerValue);
                }

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

                CreatingRequest?.Invoke(this, EventArgs.Empty);

                if (PostDataContent != null)
                {
                    // Отправка POST-данных запроса на сервер
                    Request.ContentType = PostDataContent.GetContentType();
                    byte[] content = PostDataContent.GetContent();
                    Request.ContentLength = content.Length;
                    using (Stream requestStream = Request.GetRequestStream())
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

        private const int BUFFER_SIZE = 0x40000; // 256 кб
    }

    public class ResponseDataReceivedEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
        public int ReceivedBytesCount { get; internal set; }
        public long TotalBytesToReceive { get; internal set; }
        public long ContentLength { get; internal set; }
    }
}
