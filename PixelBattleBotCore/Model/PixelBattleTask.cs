namespace PixelBattleBotCore.Model
{
    public class PixelBattleTask
    {
        public PixelBattleImage Image { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public PixelBattleTask(PixelBattleImage image, int x, int y)
        {
            X = x;
            Y = y;
            Image = image;
        }
    }
}
