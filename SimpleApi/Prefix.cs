namespace SimpleApi
{
    public class Prefix
    {
        public string PrefixString => prefix;

        private readonly string prefix;
        private readonly Func<string, string> OnMessageReceived;
        public Prefix(string prefix, Func<string, string> OnMessageReceived)
        {
            this.prefix = prefix;
            this.OnMessageReceived = OnMessageReceived;
        }

        public string MessageReceived(string msg) => OnMessageReceived(msg);
    }
}
