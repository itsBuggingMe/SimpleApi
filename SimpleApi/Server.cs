using System.Net;
using System.Text;

namespace SimpleApi
{
    /// <summary>
    /// Server class. should be inherited
    /// </summary>
    public abstract class Server : IDisposable
    {
        private readonly string Url;
        private readonly HttpListener _listener;
        /// <summary>
        /// The existing prefixes
        /// </summary>
        public Prefix[] Prefixes => prefixes;
        private readonly Prefix[] prefixes;

        private readonly Action<string> output = Console.WriteLine;


        private bool stop = false;

        /// <summary>
        /// Creates a server
        /// </summary>
        /// <param name="consoleOutput">Default is Console.WriteLine</param>
        /// <param name="defaultHost">IP that is written into config file</param>
        public Server(Action<string>? consoleOutput = null, string defaultHost = "http://localhost:8080/")
        {
            if(consoleOutput != null)
                output = consoleOutput;

            IEnumerable<(string, Func<string, string>)> prefixes = GetPrefixes(new List<(string, Func<string, string>)>());

            string configFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\config";

            bool exists = Directory.Exists(configFolderPath);
            if (!exists)
                Directory.CreateDirectory(configFolderPath);

            string txtPath = $"{configFolderPath}\\config.txt";
            bool txtExists = File.Exists(txtPath);
            if (!txtExists)
                File.WriteAllLines(txtPath, new string[] { defaultHost });

            //GET CONFIG
            Url = File.ReadAllText(txtPath).TrimEnd();
            output($"Config: {Url}");

            List<Prefix> thisPrefixes = new();

            _listener = new HttpListener();

            foreach (var prefix in prefixes)
            {
                var po = new Prefix($"{Url}{prefix.Item1}", prefix.Item2);
                thisPrefixes.Add(po);
                _listener.Prefixes.Add(po.PrefixString);
                output($"Listening on prefix {po.PrefixString}");
            }

            this.prefixes = thisPrefixes.ToArray();
        }

        /// <summary>
        /// Starts the server, and beings to listen on the specified contexts
        /// </summary>
        public void Start()
        {
            _listener.Start();
            output($"Server Started on {Url}");
            while (true)
            {
                if (stop)
                    break;
                HttpListenerContext context = _listener.GetContext();
                HandleRequest(context);
            }
            OnStop();
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)_listener).Dispose();
            GC.SuppressFinalize(this);
        }

        private void HandleRequest(HttpListenerContext context)
        {
            if (context.Request.Url == null)
                return;


            foreach (Prefix p in prefixes)
            {
                if (p.PrefixString == context.Request.Url.OriginalString)
                {
                    string responseString = p.MessageReceived(HttpContextToString(context));
                    if (!string.IsNullOrEmpty(responseString))
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

                        context.Response.ContentType = "text/plain";
                        context.Response.ContentLength64 = responseBytes.Length;

                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

                        context.Response.OutputStream.Close();
                    }
                    break;
                }
            }
        }

        private static string HttpContextToString(HttpListenerContext context)
        {
            using Stream body = context.Request.InputStream;
            using (StreamReader reader = new(body, context.Request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Server stops before next request
        /// </summary>
        public void Stop()
        {
            stop = true;
        }

        /// <summary>
        /// Called when server is constructed. 
        /// </summary>
        /// <param name="prefixes">Empty List of prefixes</param>
        /// <returns>Behavior of all possible prefixes</returns>
        protected abstract IEnumerable<(string, Func<string, string>)> GetPrefixes(List<(string, Func<string, string>)> prefixes);

        /// <summary>
        /// Empty method that is called when the server stops
        /// </summary>
        protected virtual void OnStop() { }
    }
}
