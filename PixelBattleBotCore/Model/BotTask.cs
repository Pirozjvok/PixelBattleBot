using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Model
{
    public class BotTask
    {
        public IEnumerable<PixelData> Pixels { get; }
        public BotTask(IEnumerable<PixelData> pixels)
        {
            Pixels = pixels;   
        }
        public static BotTask FromSingle(PixelData pixelData) => new BotTask(new[] { pixelData });
    }
}
