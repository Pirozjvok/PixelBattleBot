using System.Net;

namespace PixelBattleBotCore
{
    public class ProxyChecker : IDisposable
    {
        private static readonly Uri DefaultCheckUrl = new Uri("https://vk.com");

        private HttpClient _client;
        public int CheckCount { get; set; }
        public int MaxMissCount { get; set; }
        public Uri CheckUri { get; set; }
        public TimeSpan TimeOut { get; set; }

        private HttpClientHandler _httpClientHandler;
        public IWebProxy WebProxy { get => _httpClientHandler.Proxy!; set => _httpClientHandler.Proxy = value; }
        public ProxyChecker(IWebProxy webProxy)
        {
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.UseProxy = true;
            _client = new HttpClient(_httpClientHandler);
            CheckCount = 10;
            MaxMissCount = 2;
            CheckUri = DefaultCheckUrl;
            WebProxy = webProxy;
            TimeOut = TimeSpan.FromSeconds(10);
        }
        public void Dispose() => _client?.Dispose();
        public async Task<bool> Check(CancellationToken cancellationToken)
        {
            _client.Timeout = TimeOut;
            int miss = 0;
            for (int i = 0; i < CheckCount; i++)
            {
                try
                {
                    using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, DefaultCheckUrl);
                    using HttpResponseMessage response = _client.Send(requestMessage);
                    string text = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        miss++;
                }
                catch
                {
                    miss++;
                }
            }
            return miss <= MaxMissCount;
        }
    }
}
