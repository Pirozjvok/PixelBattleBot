using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Utils;

namespace PixelBattleBotCore.Vk
{
    public class PbUrls
    {
        public Uri Origin { get; set; }
        public Uri Ws { get; set; }
        public Uri Data { get; set; }
        public PbUrls(Uri origin, Uri ws, Uri data)
        {
            Origin = origin;
            Ws = ws;
            Data = data;
        }
    }
    public class PixelBattle
    {
        private static readonly Regex regex = new Regex(@"var options = ({""aid"":7148888,.*?});", RegexOptions.Compiled);
        public HttpClient HttpClient { get; set; }
        public IPBCaptchaSolver? IPBCaptchaSolver { get; set; }
        public PixelBattle(HttpClient httpClient) 
        {
            HttpClient = httpClient;
        }
        public async Task<string> GetUrl()
        {
            string result4 = await HttpClient.GetStringAsync("https://vk.com/app7148888");
            string test = regex.Match(result4).Groups[1].Value;
            JsonDocument jsonDocument = JsonDocument.Parse(test);
            string url = jsonDocument.RootElement.GetProperty("vk_app_url").GetString() ?? throw new Exception();
            return url;
        }
        public static string ParseSign(string url)
        {
            Uri uri = new Uri(url);
            string sign = uri.Query.Split('&').FirstOrDefault(x => x.StartsWith("sign")) ?? throw new Exception();
            return sign.Split('=')[1];
        }


        public async Task<PbUrls> GetUrls(CancellationToken cancellationToken)
        {
            string url = await GetUrl();
            Uri uri = new Uri(url);
            string start0 = await Start(uri.Host, "https://pixel-dev.w84.vkforms.ru/api/start", uri.Query, cancellationToken);
            //await GetJs(uri, cancellationToken);
            await Task.Delay(2500);
            await GetCaptchaUri(uri.Host, uri, cancellationToken);
            await Task.Delay(2500);
            string data = await Start(uri.Host, "https://pixel-dev.w84.vkforms.ru/api/start?view=0", uri.Query, cancellationToken);
            using JsonDocument jsonDocument = JsonDocument.Parse(data);
            string wsUrl = jsonDocument.RootElement.GetProperty("response").GetProperty("url").GetString() ?? throw new Exception("");
            string dataUrl = jsonDocument.RootElement.GetProperty("response").GetProperty("data").GetString() ?? throw new Exception("");
            Uri wsUri = GetWsUri(wsUrl, uri);
            return new PbUrls(uri, wsUri, new Uri(dataUrl));
        }

        /*

        private static Regex js1 = new Regex(@"<script src=""\.(/static/js/main\.(.*?)\.chunk\.js)""></script>");

        private static Regex js2 = new Regex(@"https://pixel-dev\.w84\.vkforms\.ru/js/main\.(.*?)\.chunk\.js");
        private async Task GetJs(Uri url, CancellationToken cancellationToken)
        {
            string data = await HttpClient.GetStringAsync(url);
            string js = "https://prod-app7148888-8b5afcc25ad6.pages-ac.vk-apps.com" + js1.Match(data).Groups[1].Value;
            string data2 = await HttpClient.GetStringAsync(js);
            string js3 = js2.Match(data2).Value;
            UriBuilder uriBuilder = new UriBuilder(js3);
            uriBuilder.Query = "?" + "vks=?" + url.Query.Substring(1);
            string data4 = await HttpClient.GetStringAsync(uriBuilder.Uri);
        }
        */
        private async Task<string> Start(string origin, string url, string sign, CancellationToken cancellationToken)
        {
            int retries = 10;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    httpRequestMessage.Headers.Add("x-vk-sign", sign);
                    httpRequestMessage.Headers.Add("Origin", "https://" + origin);
                    httpRequestMessage.Headers.Add("Referer","https://" + origin + "/");
                    string data = await (await HttpClient.SendAsync(httpRequestMessage, cancellationToken)).Content.ReadAsStringAsync(cancellationToken);
                    return data;
                }
                catch (HttpRequestException)
                {
                    retries--;
                    Console.WriteLine($"Get urls http error. Retries: {retries}");
                    if (retries == 0)
                        throw;
                }
            }
            throw new Exception();
        }
        public static string GetWsUrl(string user_id, string sign)
        {
            string temp = @"wss://pixel-dev.w84.vkforms.ru/ws?vk_access_token_settings=friends,photos&vk_app_id=7148888&vk_are_notifications_enabled=0&vk_is_app_user=1&vk_is_favorite=0&vk_language=ru&vk_platform=desktop_web&vk_ref=other&vk_ts={2}&vk_user_id={0}&sign={1}";
            return string.Format(temp, user_id, sign, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
        }

        public static Uri GetWsUri(string WsUrl, Uri app_uri)
        {
            UriBuilder uriBuilder = new UriBuilder(WsUrl);
            uriBuilder.Query = app_uri.Query;
            return uriBuilder.Uri;
        }

        public async Task GetCaptchaUri(string origin, Uri uri, CancellationToken cancellationToken)
        {
            int attempts = 5;
            int retries = 10;
            while (!cancellationToken.IsCancellationRequested) 
            {
                try
                {
                    using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://pixel-dev.w84.vkforms.ru/api/captcha/get");
                    httpRequestMessage.Headers.Add("Origin", "https://" + origin);
                    httpRequestMessage.Headers.Add("Referer", "https://" + origin + "/");
                    httpRequestMessage.Headers.Add("x-vk-sign", uri.Query);
                    string data = await (await HttpClient.SendAsync(httpRequestMessage, cancellationToken)).Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument jsonDocument = JsonDocument.Parse(data);
                    if (jsonDocument.RootElement.GetProperty("response").GetProperty("show").GetBoolean() == true)
                    {
                        if (IPBCaptchaSolver == null)
                            throw new Exception();
                        Console.WriteLine($"Pixel battle CAPTCHA");
                        var captcha = PixelBattleCaptcha.ParseSvg(jsonDocument.RootElement.GetProperty("response").GetProperty("captcha").GetString()!);
                        ICaptchaSolverResponse captchaSolverResponse = await IPBCaptchaSolver.Solve(captcha);
                        bool correct = await CaptchaVerify(captchaSolverResponse.Code, uri, cancellationToken);
                        await captchaSolverResponse.Report(correct);
                        Console.WriteLine($"Pixel battle CAPTCHA Solved. Result {(correct ? "Correct" : "Error")}");
                        if (!correct)
                            attempts--;
                        else
                            return;
                        if (attempts == 0)
                            throw new IncorrectCaptchaException("Incorrect captcha");
                        await Task.Delay(10000);
                    }

                }
                catch (HttpRequestException)
                {
                    retries--;
                    if (retries == 0)
                        throw;
                    await Task.Delay(5000);
                }

            }
        }

        public async Task<bool> CaptchaVerify(string data, Uri uri, CancellationToken cancellationToken)
        {
            int tries = 10;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://pixel-dev.w84.vkforms.ru/api/captcha/verify");
                    httpRequestMessage.Headers.Add("x-vk-sign", uri.Query);
                    FormUrlEncodedContent formUrlEncoded = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("text", data),
                    });
                    httpRequestMessage.Content = formUrlEncoded;
                    HttpResponseMessage responseMessage = await HttpClient.SendAsync(httpRequestMessage, cancellationToken);
                    string resp = await responseMessage.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(resp);
                    bool is_correct = jsonDocument.RootElement.GetProperty("response").GetProperty("isCorrect").GetBoolean();
                    return is_correct;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Captcha verify http error {e.Message}. Tries: {tries}");
                    if (tries == 0)
                        throw;
                    tries--;
                }
            }
            return true;
        }
    } 

    public static class UriPBeXT
    {
        private static Regex rts = new Regex("vk_ts=(.*?)&");
        public static Uri ReplaceTs(this Uri uri)
        {
            return uri;
        }
    }
}
