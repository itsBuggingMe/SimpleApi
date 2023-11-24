namespace SimpleApi
{
    internal class Client : IDisposable
    {
        private readonly HttpClient _client = new();
        private readonly string Url;

        /// <summary>
        /// Generates config file
        /// </summary>
        public Client(string defaultHost = "http://localhost:8080/")
        {
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
        }
        /// <summary>
        /// Posts a request to the specified URL
        /// </summary>
        /// <param name="message">the string that is send to the end point</param>
        /// <param name="OnReturn">called as the output of the request</param>
        /// <param name="url">default is default host provided in constructor</param>
        public void PostRequestString(string message, Action<string> OnReturn, string? url = null) => PostRequestStringInternal(message, OnReturn, url);

        private async void PostRequestStringInternal(string message, Action<string> OnReturn, string? url)
        {
            if(url == null && Url == string.Empty)
            {
                throw new NullReferenceException("Url has not been provided");
            }

            HttpResponseMessage response = await _client.PostAsync(url == null ? Url : url, new StringContent(message));

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                OnReturn?.Invoke(result);
            }
        }

        /// <summary>
        /// Deserialises json file into object before returning
        /// </summary>
        public void PostRequestObject<T>(string message, Action<T?> OnReturn, string? url = null) => PostRequestStringInternal(message, (s) =>
        {
            T? gettedObject = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(message);
            OnReturn(gettedObject);
        }, url);


        public void Dispose() => ((IDisposable)_client).Dispose();
    }
}
