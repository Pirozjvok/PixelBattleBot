using PixelBattleBotCore.Abstractions;

namespace PixelBattleBotCore.Model
{
    public class PixelBattleImage : IImage
    {
        public static readonly string[] PaletteString = new[] { "#FFFFFF", "#C2C2C2", "#858585", "#474747", "#000000", "#3AAFFF", "#71AAEB", "#4A76A8", "#074BF3", "#5E30EB", "#FF6C5B", "#FE2500", "#FF218B", "#99244F", "#4D2C9C", "#FFCF4A", "#FEB43F", "#FE8648", "#FF5B36", "#DA5100", "#94E044", "#5CBF0D", "#C3D117", "#FCC700", "#D38301" };
        public int Width { get; }
        public int Height { get; }
        public sbyte[,] Image { get; }

        private object _lock = new object();
        public PixelBattleImage(int width, int height)
        {
            Width = width;
            Height = height;
            Image = new sbyte[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Image[x, y] = -1;
                }
            }

        }
        public virtual sbyte GetPixel(int x, int y)
        {
            lock (_lock)
                return Image[x, y];
        }
        public virtual void SetPixel(int x, int y, sbyte color)
        {
             lock (_lock)
                Image[x, y] = color;
        }
    }
}
