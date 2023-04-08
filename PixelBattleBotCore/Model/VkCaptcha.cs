using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Model
{
    public class VkCaptcha
    {
        public string Sid { get; set; }
        public string Base64 { get; set; }
        public VkCaptcha(string sid, string base64)
        {
            Sid = sid;
            Base64 = base64;
        }
    }
}
