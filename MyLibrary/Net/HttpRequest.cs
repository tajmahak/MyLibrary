using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace MyLibrary.Net
{
    public class HttpRequest : IDisposable
    {
        public string RequestUri { get; set; }
        public object UploadData { get; set; }
        public bool UseHeadRequest { get; set; }
        public int Timeout { get; set; }
        public HttpWebRequest Request { get; private set; }
        public HttpWebResponse Response { get; private set; }

        #region Конструктор

        public HttpRequest()
        {
            Timeout = 100000;
        }
        public HttpRequest(string requestUri)
            : this()
        {
            RequestUri = requestUri;
        }

        #endregion
        public void Dispose()
        {
            if (Response != null)
                Response.Close();
        }

        public string GetString()
        {
            string data = null;
            GetWebData(() =>
            {
                var encoding = string.IsNullOrEmpty(Response.CharacterSet) ?
                    Encoding.UTF8 : Encoding.GetEncoding(Response.CharacterSet);

                using (var stream = GetResponseStream())
                using (var reader = new StreamReader(stream, encoding))
                    data = reader.ReadToEnd();
            });
            return data;
        }
        public void GetData(Stream outputStream)
        {
            GetWebData(() =>
            {
                var contentLength = Response.ContentLength;
                var knownContentLength = (contentLength != -1);

                var e = new DownloadProgressChangedEventArgs();
                e.ContentLength = contentLength;

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

                        if (DownloadProgressChanged != null)
                        {
                            e.BytesReceived = bytesReceived;
                            e.TotalBytesToReceive = totalBytesToReceive;
                            DownloadProgressChanged(this, e);
                            if (e.Cancel)
                                break;
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
            var stream = Response.GetResponseStream();

            // Выбор метода распаковки потока
            switch (Response.ContentEncoding)
            {
                case "gzip":
                    stream = new GZipStream(stream, CompressionMode.Decompress, false); break;
                case "deflate":
                    stream = new DeflateStream(stream, CompressionMode.Decompress, false); break;
            }

            return stream;
        }

        public event Action<HttpRequest> BeforeGetResponse;
        public event Action<HttpRequest> AfterGetResponse;
        public event Action<HttpRequest, DownloadProgressChangedEventArgs> DownloadProgressChanged;

        private void GetWebData(Action getDataAction)
        {
            Request = null;
            Response = null;
            try
            {
                Request = (HttpWebRequest)HttpWebRequest.Create(RequestUri);

                #region Настройка Web-запроса

                Request.KeepAlive = true;
                Request.Timeout = Timeout;
                Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:54.0) Gecko/20100101 Firefox/54.0";
                Request.Headers["Accept-Encoding"] = "gzip, deflate";
                Request.Headers["Accept-Language"] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";

                if (UploadData == null)
                    Request.Method = UseHeadRequest ? "HEAD" : "GET";
                else Request.Method = "POST";

                #endregion

                if (BeforeGetResponse != null)
                    BeforeGetResponse(this);

                if (UploadData != null)
                {
                    #region Отправка POST-данных запроса на сервер

                    byte[] uploadData = null;
                    if (UploadData is string)
                    {
                        Request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        uploadData = Encoding.UTF8.GetBytes((string)UploadData);
                    }
                    else if (UploadData is byte[])
                    {
                        Request.ContentType = "application/x-www-form-urlencoded";
                        uploadData = (byte[])UploadData;
                    }

                    Request.ContentLength = uploadData.Length;
                    using (var requestStream = Request.GetRequestStream())
                        requestStream.Write(uploadData, 0, uploadData.Length);

                    #endregion
                }

                Response = (HttpWebResponse)Request.GetResponse();

                if (AfterGetResponse != null)
                    AfterGetResponse(this);

                if (getDataAction != null)
                    getDataAction();

                UploadData = null;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                    Response = (HttpWebResponse)(ex as WebException).Response;
                throw;
            }
            finally
            {
                Dispose();
            }
        }

        #region Статические сущности

        public static string GetString(string requestUri)
        {
            return GetString(requestUri, null);
        }
        public static string GetString(string requestUri, object uploadData)
        {
            using (var request = new HttpRequest(requestUri))
            {
                request.UploadData = uploadData;
                return request.GetString();
            }
        }
        public static void GetData(Stream outputStream, string requestUri)
        {
            GetData(outputStream, requestUri, null);
        }
        public static void GetData(Stream outputStream, string requestUri, object uploadData)
        {
            using (var request = new HttpRequest(requestUri))
            {
                request.UploadData = uploadData;
                request.GetData(outputStream);
            }
        }

        #endregion
    }

    public class DownloadProgressChangedEventArgs
    {
        public bool Cancel;
        public int BytesReceived;
        public long TotalBytesToReceive;
        public long ContentLength;
    }

    public class RequestParameterBuilder
    {
        private StringBuilder _str;

        public RequestParameterBuilder()
        {
            _str = new StringBuilder();
        }
        public void Add(string name, object value)
        {
            if (_str.Length > 0)
                _str.Append("&");
            _str.Append(name);
            _str.Append("=");
            _str.Append(value);
        }
        public override string ToString()
        {
            return _str.ToString();
        }
    }
}