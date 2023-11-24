using System.Net;
using System.Text;

namespace SimpleApi
{
    public abstract class Server : IDisposable
    {
        private readonly string Url;
        private readonly HttpListener _listener;

        public Prefix[] Prefixes => prefixes;
        private readonly Prefix[] prefixes;

        private readonly Action<string> output = Console.WriteLine;

        public Server(Action<string>? consoleOutput = null)
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
                File.WriteAllLines(txtPath, new string[] { "http://localhost:8080/" });

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

        public void Start()
        {
            _listener.Start();
            output($"Server Started on {Url}");
            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                HandleRequest(context);
            }
        }

        public void Dispose() => ((IDisposable)_listener).Dispose();

        protected void HandleRequest(HttpListenerContext context)
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

                        // Get the output stream and write the response
                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

                        // Close the output stream
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

        protected abstract IEnumerable<(string, Func<string, string>)> GetPrefixes(List<(string, Func<string, string>)> prefixes);
    }
}
