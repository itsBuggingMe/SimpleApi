using Newtonsoft.Json;

namespace SimpleApi
{
    internal static class ApiGetter
    {
        private static HttpClient client = new();

        public static async void GetString(string url, Action<string> GetResponseCallback)
        {
            string response = await client.GetAsync(url).Result.Content.ReadAsStringAsync();
            GetResponseCallback(response);
        }

        public static async void GetObject<T>(string url, Action<T> GetResponseCallback)
        {
            string response = await client.GetAsync(url).Result.Content.ReadAsStringAsync();
            var t = JsonConvert.DeserializeObject<T>(response);

            if (t != null)
                GetResponseCallback(t);
        }

        public static async void UrlToBmp(string url, Action<Image> onGetBitmap, Action<Exception>? onError = null)
        {
            try
            {
                byte[] data = await client.GetByteArrayAsync(url);
                using var stream = new MemoryStream(data);
                onGetBitmap(Image.Load(stream));
            }
            catch (Exception ex)
            {
                if (onError == null)
                    throw;
                onError(ex);
            }
            return;
        }
    }

}
