using PixelBattleBotCore.Impl;
using PixelBattleBotCore.Vk;
using System.Net;
using System.Text.Json.Serialization;

namespace PixelBattleBotCore.Model
{
    public class BotInfo
    {
        [JsonIgnore]
        public IWebProxy? WebProxy { get => VkHtmlApi.Proxy; set => VkHtmlApi.Proxy = value; }
        public uint BotId => User.Id;

        [JsonIgnore]
        public Bot? Bot { get; set; }
        public User User { get; set; } = null!;
        public bool ProxyRequired { get; set; }
        public bool ReplaceProxyIfDied { get; set; } = true;

        [JsonIgnore]
        public bool FirstFlag { get; set; } = true;

        [JsonIgnore]
        public bool Busy { get; set; }

        [JsonIgnore]
        public WsSession? WsSession { get; set; }

        private VkHtmlApi? vkApi;

        private PixelBattle? pb;

        [JsonIgnore]
        public PixelBattle PixelBattle
        {
            get
            {
                return pb ??= new PixelBattle(VkHtmlApi.HttpClient);
            }
            set
            {
                pb = value;
            }

        }

        [JsonIgnore]
        public VkHtmlApi VkHtmlApi
        {
            get
            {            
                return vkApi ??= new VkHtmlApi(User);
            }
            set
            {
                vkApi = value;
            }
        }
        private BotInfo() 
        {
            
        }
        public BotInfo(User user) : this()
        {
            User = user;
            VkHtmlApi = new VkHtmlApi(User);
        }

        public static BotInfo CrateByPassword(string login, string password)
        {
            User user = new User()
            {
                Login = login,
                Password = password
            };
            return new BotInfo(user);
        }
    }
}
