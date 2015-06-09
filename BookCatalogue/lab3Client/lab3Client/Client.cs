using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lab3Client
{
    public class Client
    {
        public enum Request
        {
            AddBook,
            GetCatalogue,
            RemoveBook,
            SearchField,
            SearchKeywords,
            Quit,
            Dummy
        }

        public readonly string host;

        public Client(string host)
        {
            this.host = host;
        }

        private void WriteMessageToRequestStream(HttpWebRequest request, byte[] message)
        {
            using (Stream requestStream = request.GetRequestStream())
                requestStream.Write(message, 0, message.Length);
        }
        private byte[] ObtainResponse(HttpWebRequest request, byte[] message)
        {
            var response = request.GetResponse();
            byte[] result = new byte[response.ContentLength];

            using (var responseStream = response.GetResponseStream())
            {
                int bytesRead = 0;
                while (bytesRead < response.ContentLength)
                    bytesRead += responseStream.Read(result, bytesRead, result.Length - bytesRead);
            }
            response.Close();
            request.KeepAlive = false;
            return result;
        }

        private static HttpWebRequest CreatePostRequest(byte[] message, string host)
        {
            HttpWebRequest request = WebRequest.CreateHttp(host);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = true;
            request.ContentLength = message.Length;
            return request;
        }
        public static bool TryConnection(string host)
        {
            if (String.IsNullOrEmpty(host))
                return false;

            var request = CreatePostRequest(new byte[] { }, host+"dummy");
            try
            {
                var stream = request.GetRequestStream();
                return true;
            }
            catch 
            {
                return false;
            }
        }

        public bool TryByteRequest(ref byte[] message, Request requestType)
        {
            if (message == null)
                return false;
            if (String.IsNullOrEmpty(host))
                return false;

            HttpWebRequest request = CreatePostRequest(message, host + requestType.ToString());

            if (!TryConnection(host))
                return false;
            WriteMessageToRequestStream(request, message);
            message = ObtainResponse(request, message);
            return true;

        }
    }
}
