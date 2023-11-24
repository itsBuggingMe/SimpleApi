namespace SimpleApi
{
    /// <summary>
    /// Represents a prefix and a response
    /// </summary>
    public class Prefix
    {
        public string PrefixString => prefix;

        private readonly string prefix;
        private readonly Func<string, string> OnMessageReceived;

        /// <summary>
        /// string prefix is appended to ip or url specified
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="OnMessageReceived"></param>
        public Prefix(string prefix, Func<string, string> OnMessageReceived)
        {
            this.prefix = prefix;
            this.OnMessageReceived = OnMessageReceived;
        }

        public string MessageReceived(string msg) => OnMessageReceived(msg);
    }
}
