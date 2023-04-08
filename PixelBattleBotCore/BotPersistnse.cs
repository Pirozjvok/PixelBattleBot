using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore
{
    public class BotPersistnse
    {
        public List<BotInfo> Bots { get; set; }
        public BotPersistnse()
        {
            Bots = new List<BotInfo>();
        }
    }
}
