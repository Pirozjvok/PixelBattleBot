using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Vk
{
    public class VkAuthHelper
    {
        private static string auth_pattern = @"<script>      window.init = (.*);      window.promo";

        public static Regex VkRegex = new Regex(auth_pattern, RegexOptions.Compiled);
        public static CookieCollection DeviceCookie()
        {
            CookieCollection cookies = new CookieCollection()
            {
                new Cookie()
                {
                    Name = "remixscreen_width",
                    Value = "1920",
                    Domain = ".vk.com",
                    Path = "/",
                    Expires = DateTime.Today.AddMonths(3),
                    Secure = true,
                    HttpOnly = false
                },
                new Cookie()
                {
                    Name = "remixscreen_height",
                    Value = "1080",
                    Domain = ".vk.com",
                    Path = "/",
                    Expires = DateTime.Today.AddMonths(3),
                    Secure = true,
                    HttpOnly = false
                },
                new Cookie()
                {
                    Name = "remixscreen_depth",
                    Value = "24",
                    Domain = ".vk.com",
                    Path = "/",
                    Expires = DateTime.Today.AddMonths(3),
                    Secure = true,
                    HttpOnly = false
                }
            };
            return cookies;
        }

        private static readonly Regex Test = new Regex(@"need_captcha");
        private async Task<(string, int)> GetToken(HttpClient httpClient)
        {
            using var rm1 = new HttpRequestMessage(HttpMethod.Get, "https://id.vk.com/promo");

            var resp = await httpClient.SendAsync(rm1);

            string result = await resp.Content.ReadAsStringAsync();

            string supermega = result.Replace("\n", "");

            string json = VkRegex.Match(supermega).Groups[1].Value;

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement auth = document.RootElement.GetProperty("auth");

            int app_id = auth.GetProperty("host_app_id").GetInt32();
            string? access_token = auth.GetProperty("access_token").GetString();
            string? anonymous_token = auth.GetProperty("anonymous_token").GetString();
            return (access_token ?? throw new Exception("pizda"), app_id);
        }
        public async Task<string> Authorize(HttpClient httpClient, string login, string password, string? captcha_sid = null, string? captcha_key= null)
        {
            var token = await GetToken(httpClient);
            string access_token = token.Item1;
            int app_id = token.Item2;

            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("username", login),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("app_id", $"{app_id}"),
                new KeyValuePair<string, string>("auth_token", access_token!)
            };

            if (captcha_sid != null)
            {
                keyValuePairs.Add(new KeyValuePair<string, string>("captcha_sid", captcha_sid));
                keyValuePairs.Add(new KeyValuePair<string, string>("captcha_key", captcha_key!));
            }

            using HttpContent content = new FormUrlEncodedContent(keyValuePairs);

            using var rm = new HttpRequestMessage(HttpMethod.Post, "https://login.vk.com/?act=connect_authorize");
            rm.Content = content;
            rm.Headers.Add("origin", "https://id.vk.com");

            var resp2 = await httpClient.SendAsync(rm);

            string result2 = resp2.Content.ReadAsStringAsync().Result;

            if (Test.IsMatch(result2))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Captcha");
                Console.ForegroundColor = ConsoleColor.Green;
                File.WriteAllText(Random.Shared.Next() + ".txt", result2);
                throw new Exception("CaptchaNeeded");
            }

            return result2;
        }

    }
}
