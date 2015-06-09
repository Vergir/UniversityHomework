using System;
using System.Net;
using System.Reflection;
using System.Threading;
using BnC;

namespace consoleServer
{
    class Server
    {
        private string pathToCatalogue = System.IO.Directory.GetCurrentDirectory() + "/";
        public enum Mode { Threads, APM };
        private static readonly int modesCount = Enum.GetNames(typeof(Mode)).Length;
        private bool isAlive = true;
        private string host;
        private int hostLength;
        private BookCatalogue catalogue = new BookCatalogue();
        private HttpListener listener = new HttpListener();
        private Mode mode;

        public Server(string host = "http://127.0.0.1:1270/", string pathToCatalogue = null)
        {
            this.host = host;
            this.hostLength = host.Length;
            if (!String.IsNullOrEmpty(pathToCatalogue))
                this.pathToCatalogue = pathToCatalogue;
            listener.Prefixes.Add(this.host);
        }
        public Server(Mode mode, string host = "http://127.0.0.1:1270/", string pathToCatalogue = null)
            : this(host, pathToCatalogue)
        {
            this.mode = mode;
        }

        private static string AskForMode()
        {
            byte input;
            while (true)
            {
                if ((byte.TryParse(Console.ReadLine(), out input)) && (input < modesCount))
                    break;
                else
                    Console.Write("Incorrect input, try again...");
            }
            return input.ToString();
        }
        private static void WriteAvailableModes()
        {
            Console.WriteLine("PLease Choose Server Mode. Available Modes:");

            for (int i = 0; i < modesCount; i++)
                Console.WriteLine("{0}: {1}", i, Enum.GetNames(typeof(Mode))[i]);
        }
        public void ChooseMode()
        {
            WriteAvailableModes();
            this.mode = (Mode)Enum.Parse(typeof(Mode), AskForMode());
        }

        private bool TryToLoadCatalogue()
        {

            try
            {
                catalogue = BookSerialization.LoadFromFile(this.pathToCatalogue);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public void LoadCatalogue(string pathToCatalogue = null)
        {
            if (pathToCatalogue == null)
                this.pathToCatalogue += "default.ini";
            else
                this.pathToCatalogue = pathToCatalogue + "default.ini";
            Console.WriteLine("Trying to Load catalog...");
            if (TryToLoadCatalogue())
                Console.WriteLine("Catalogue successfully loaded.");
            else
                Console.WriteLine("Catalogue could not be loaded, making empty one.");
        }

        private bool KeyIsPressed(ConsoleKey key)
        {
            if (Console.KeyAvailable)
                if (Console.ReadKey(true).Key == key)
                    return true;
            return false;
        }
        public void GracefulShutDown(IAsyncResult result)
        {
            isAlive = false;
            catalogue.SaveToFile(pathToCatalogue);
        }
        private void StartListening()
        {
            if (mode == Mode.APM)
                while (isAlive)
                {
                    IAsyncResult result = listener.BeginGetContext(HandleRequestAPM, listener);
                    while ((!result.IsCompleted) && (isAlive))
                        if (KeyIsPressed(ConsoleKey.Q))
                            GracefulShutDown(result);
                }
            else
                while (isAlive)
                {
                    object context = listener.GetContext();
                    new Thread(HandleRequestThread).Start(context);
                }
            this.Stop();
        }
        public void Start()
        {
            listener.Start();
            StartListening();
        }
        public void Stop()
        {
            listener.Stop();
        }


        private void HandleRequestThread(object HttpContext)
        {
            HttpListenerContext context = (HttpListenerContext)HttpContext;
            HandleClient(context);
        }
        private void HandleRequestAPM(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = null;
            try
            {
                context = listener.EndGetContext(result);
            }
            catch { }
            HandleClient(context);

        }

        private string GetActionLink(HttpListenerRequest request)
        {
            return request.Url.ToString().Substring(hostLength);
        }
        private static byte[] ReadRequestBody(HttpListenerRequest request)
        {
            if (request == null)
                return null;
            if (request.ContentLength64 == 0)
                return new byte[] { };

            byte[] result = new byte[request.ContentLength64];
            request.InputStream.Read(result, 0, result.Length);
            return result;
        }
        private static string[] ExtractSearchWords(byte[] requestBody)
        {
            if (requestBody == null)
                return null;
            string searchValuesRaw = System.Text.Encoding.UTF8.GetString(requestBody);
            string[] searchValuesPrepared = searchValuesRaw.Split(new char[] { ';' });
            return searchValuesPrepared;
        }
        private static Book.fields StringToField(string field)
        {
            if (String.IsNullOrEmpty(field))
                throw new ArgumentNullException("field");

            return (Book.fields)Enum.Parse(typeof(Book.fields), field);
        }
        private byte[] WorkOnRequest(HttpListenerRequest request)
        {
            if (request == null)
                return null;

            byte[] requestBody = ReadRequestBody(request);
            byte[] response = null;

            switch (GetActionLink(request))
            {
                case ("AddBook"):
                    Book book = BookSerialization.GetBookFromByteArray(requestBody);
                    catalogue.AddBook(book);
                    response = book.ToByteArray();
                    Console.WriteLine("Got Request to ADD following Book: {0}", book);
                    break;
                case ("GetCatalogue"):
                    response = catalogue.ToByteArray();
                    Console.WriteLine("Got request to SHOW catalogue.");
                    break;
                case ("RemoveBook"):
                    int index = BitConverter.ToInt32(requestBody, 0);
                    catalogue.RemoveBookByIndex(index);
                    response = catalogue.ToByteArray();
                    Console.WriteLine("Got request to REMOVE book by following index: {0}", index);
                    break;
                case ("SearchField"):
                    string[] searchWords = ExtractSearchWords(requestBody);
                    Book.fields fieldToSearchWithin = StringToField(searchWords[0]);
                    response = catalogue.SearchWithinAField(fieldToSearchWithin, searchWords[1]).ToByteArray();
                    Console.WriteLine("Got request to SEARCH WITHIN A FIELD. Field to searh within: {0}. String to search: {1}", fieldToSearchWithin, searchWords[1]);
                    break;
                case ("SearchKeywords"):
                    string[] searchKeyWords = ExtractSearchWords(requestBody);
                    response = catalogue.SearchThroughKeyWords(searchKeyWords).ToByteArray();
                    Console.WriteLine("Got request to SEARCH THROUGH KEYWORDS: {0}", searchKeyWords);
                    break;
                case ("Save"):
                    catalogue.SaveToFile(pathToCatalogue + "default.ini");
                    Console.WriteLine("Got request to SAVE");
                    break;
                case ("Quit"):
                    isAlive = false;
                    catalogue.SaveToFile(pathToCatalogue + "default.ini");
                    Console.WriteLine("Got request to QUIT");
                    break;
                default:
                    response = new byte[] { };
                    Console.WriteLine("Got unknown (or dummy) request...");
                    break;
            }
            return response;
        }
        private void HandleClient(HttpListenerContext context)
        {
            byte[] responseMessage;

            lock (catalogue)
            {
                responseMessage = WorkOnRequest(context.Request);
            }
            using (HttpListenerResponse response = context.Response)
            {
                response.ContentLength64 = responseMessage.Length;
                using (var output = response.OutputStream)
                    output.Write(responseMessage, 0, responseMessage.Length);
            }
            Console.WriteLine("Client Handled");
        }


    }
    class MainClass
    {
        public static void Main()
        {
            Server server = new Server();
            server.ChooseMode();
            server.LoadCatalogue();
            server.Start();
        }
    }
}

