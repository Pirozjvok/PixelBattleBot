using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBot
{
    public class Config
    {
        public int ImageX { get; set; }
        public int ImageY { get; set; }
        public string? Image { get; set; }
        public string? RuCaptchaApiKey { get; set; }
        public string? RuCaptchaProxy { get; set; }
        public string? RuCaptchaProxyUser { get; set; }
        public string? RuCaptchaProxyPassw { get; set; }
        public int? Workers { get; set; }
    }
}
