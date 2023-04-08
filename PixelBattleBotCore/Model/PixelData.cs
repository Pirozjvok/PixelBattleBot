namespace PixelBattleBotCore.Model
{
    public class PixelData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Color { get; set; }
        public int Flag { get; set; }
        public int Pack(PlaceInfo e)
        {
            return Pack(e, X, Y, Color, Flag);
        }
        public static int Pack(PlaceInfo e, int x, int y, int colorId, int flag)
        {
            var t = colorId + flag * e.MaxColorId;
            return x + y * e.Width + e.Size * t;
        }
        public static PixelData Parse(PlaceInfo e, int t)
        {
            var n = (int)Math.Floor((float)t / e.Size);
            var r = (t -= n * e.Size) % e.Width;
            return new PixelData()
            {
                X = r,
                Y = (t - r) / e.Width,
                Color = n % e.MaxColorId,
                Flag = (int)Math.Floor((float)n / e.MaxColorId)
            };
        }

        public static PixelData Parse(PlaceInfo e, byte[] buffer, int offset)
        {
            int t = BitConverter.ToInt32(buffer, offset);
            return Parse(e, t);
        }
    }
}