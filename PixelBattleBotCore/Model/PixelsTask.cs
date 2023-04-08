using PixelBattleBotCore.Abstractions;

namespace PixelBattleBotCore.Model
{
    public class PixelsTask
    {
        public IBot Bot { get; set; }
        public BotTask BotTask { get; set; }
        public PixelsTask(IBot bot, BotTask botTask)
        {
            Bot = bot;
            BotTask = botTask;
        }
    }
}