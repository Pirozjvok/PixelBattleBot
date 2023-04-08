using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoCaptcha;
using TwoCaptcha.Exceptions;

namespace PixelBattleBot
{
    public class SuperApiClient : ApiClient
    {
        /**
         * API server
         */
        private string baseUrl = "https://2captcha.com/";

        /**
         * Network client
         */
        private readonly HttpClient client;

        public SuperApiClient(HttpClient httpClient)
        {
            client = httpClient;
            client.BaseAddress = new Uri(baseUrl);
        }

        public override async Task<string> In(Dictionary<string, string> parameters, Dictionary<string, FileInfo> files)
        {
            var content =
                new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture));

            foreach (KeyValuePair<string, string> p in parameters)
            {
                content.Add(new StringContent(p.Value), p.Key);
            }

            foreach (KeyValuePair<string, FileInfo> f in files)
            {
                var fileStream = new StreamContent(new MemoryStream(File.ReadAllBytes(f.Value.FullName)));
                content.Add(fileStream, f.Key, f.Value.Name);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "in.php")
            {
                Content = content
            };

            return await Execute(request);
        }

        public override async Task<string> Res(Dictionary<string, string> parameters)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "res.php?" + BuildQuery(parameters));

            return await Execute(request);
        }

        private string BuildQuery(Dictionary<string, string> parameters)
        {
            string query = "";

            foreach (KeyValuePair<string, string> p in parameters)
            {
                if (query.Length > 0)
                {
                    query += "&";
                }

                query += p.Key + "=" + Uri.EscapeDataString(p.Value);
            }

            return query;
        }

        private async Task<string> Execute(HttpRequestMessage request)
        {
            var response = await client.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new NetworkException("Unexpected response: " + body);
            }

            if (body.StartsWith("ERROR_"))
            {
                throw new ApiException(body);
            }

            return body;
        }
    }
}
