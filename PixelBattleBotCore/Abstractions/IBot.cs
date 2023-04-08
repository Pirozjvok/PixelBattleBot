using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IBot
    {
        Task ExecuteTask(BotTask botTask, CancellationToken cancellationToken);
        DateTime NextTryAllowAt { get; }
        bool BotReady { get; }
        bool BotEnabled { get; set; }
        bool BotBusy { get; }     
        int Bomb { get; }
        int Freeze { get; }
        int Pixel { get; }
        int SinglePixel { get; }
    }
}
