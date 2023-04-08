namespace PixelBattleBotCore.Model
{
    public class PlaceInfo
    {
        public int Width { get; init; } = 1590;
        public int Height { get; init; } = 400;
        public int Size => Width * Height;
        public int MaxColorId { get; init; } = 25;
    }
}