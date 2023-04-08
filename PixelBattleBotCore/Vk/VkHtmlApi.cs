using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
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


    [Serializable]
    public class VkAuthException : Exception
    {
        public VkAuthException() { }
        public string? Code { get; set; }
        public VkAuthException(string message) : base(message) { }

        public VkAuthException(string message, string? code) : base(message) 
        {
            Code = code;
        }
        public VkAuthException(string message, Exception inner) : base(message, inner) { }
        protected VkAuthException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class VkHtmlApi : IDisposable
    {
        private VkAuthHelper vkAuthHelper;

        private HttpClientHandler _handler;
        public HttpClient HttpClient { get; }
        public User User { get; set; }
        public IVkCaptchaSolver? VkCaptchaSolver { get; set; }
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(15);
        public IWebProxy? Proxy
        {
            get
            {
                return _handler.Proxy;
            }
            set
            {
                _handler.UseProxy = true;
                _handler.Proxy = value;
            }
        }
        private static readonly Regex authRegex = new Regex("top_profile_link", RegexOptions.Compiled);
        public VkHtmlApi(User user)
        {
            vkAuthHelper = new VkAuthHelper();
            _handler = new HttpClientHandler();
            HttpClient = new HttpClient(_handler);
            HttpClient.Timeout = TimeOut;
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36 Edg/111.0.1661.62");
            User = user;
            CookieCollection cookieCollection = User.CookieCollection ?? new CookieCollection();
            if (!cookieCollection.Any(x => x.Name == "remixscreen_width"))
            {
                cookieCollection.Add(VkAuthHelper.DeviceCookie());
            }
            _handler.CookieContainer.Add(cookieCollection);
        }
        public async Task<bool> IsAuthorized()
        {
            string data = await HttpClient.GetStringAsync("http://vk.com");
            return authRegex.IsMatch(data);
        }
        public Task<uint> GetUserId()
        {
            return Task.FromResult(User.Id);
        }
        public async Task Authorize()
        {
            if (User.Login is null || User.Password is null)
                throw new NullReferenceException();
            string text = await vkAuthHelper.Authorize(HttpClient, User.Login, User.Password);
            JsonDocument jsonDocument = JsonDocument.Parse(text);
            JsonElement root = jsonDocument.RootElement;


            if (root.TryGetProperty("error", out JsonElement value))
            {
                int code = value.GetProperty("error_code").GetInt32();
                if (code == 14)
                {
                    if (VkCaptchaSolver == null)
                        throw new VkAuthException("Captcha needed", "14");
                    string captcha_sid = value.GetProperty("captcha_sid").GetString()!;
                    string captcha_img = value.GetProperty("captcha_img").GetString()!;
                    byte[] img = await HttpClient.GetByteArrayAsync(captcha_img);
                    VkCaptcha vkCaptcha = new VkCaptcha(captcha_sid, Convert.ToBase64String(img));
                    string capt = (await VkCaptchaSolver.Solve(vkCaptcha)).Code;
                    text = await vkAuthHelper.Authorize(HttpClient, User.Login, User.Password, captcha_sid, capt);
                    Console.WriteLine(text);
                }
            }

            string type = root.GetProperty("type").GetString() ?? throw new Exception("Ebal");
            if (type == "error")
            {
                User.IncorrectPassword = true;
                throw new VkAuthException(root.GetProperty("error_info").GetString() ?? "Error", root.GetProperty("error_code").GetString());
            }
            JsonElement data = root.GetProperty("data");
            JsonElement user = data.GetProperty("auth_info").GetProperty("user");
            string token = data.GetProperty("access_token").ToString();
            uint user_id = user.GetProperty("id").GetUInt32();
            User.AccessToken = token;
            User.Id = user_id;
            Save();
        }
        public void Save()
        {
            User.CookieCollection = _handler.CookieContainer.GetAllCookies();
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            _handler.Dispose();
        }
    }
}
