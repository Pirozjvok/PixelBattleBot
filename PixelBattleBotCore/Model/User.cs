using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace PixelBattleBotCore.Model
{
    public class User
    {
        public uint Id { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? AccessToken { get; set; }
        [JsonIgnore]
        public Uri? WsUri { get; set; }
        [JsonIgnore]
        public Uri? Origin { get; set; }
        public bool IncorrectPassword { get; set; }
        public CookieCollection? CookieCollection { get; set; }
    }
}
