using ColorMine.ColorSpaces.Comparisons;
using ColorMine.ColorSpaces;

namespace PixelBattleBotCore.Model
{
    public class ImageSharpPBImage : PixelBattleImage
    {
        public static readonly Color[] Palette = PaletteString.Select(x => Color.ParseHex(x)).ToArray();
        public static Color GetColor(sbyte id) => id == -1 ? Color.Transparent : Palette[id];

        public static Rgb[] RGB = Palette.Select(x => { var px = x.ToPixel<Rgba32>(); return new Rgb() { R = px.R, B = px.B, G = px.G }; }).ToArray();

        private bool[,] _changeMap;

        public Image<Rgba32> Bitmap;

        private IColorSpaceComparison _colorSpaceComparison = new CieDe2000Comparison();
        public ImageSharpPBImage(int width, int height) : base(width, height)
        {
            Bitmap = new Image<Rgba32>(width, height);
            _changeMap = new bool[width, height];
        }
        public ImageSharpPBImage(Image<Rgba32> bitmap) : base(bitmap.Width, bitmap.Height)
        {
            _changeMap = new bool[bitmap.Width, bitmap.Height];
            Bitmap = bitmap;

        }
        public override sbyte GetPixel(int x, int y)
        {
            return base.GetPixel(x, y);
        }
        public override void SetPixel(int x, int y, sbyte color)
        {
            _changeMap[x, y] = true;
            base.SetPixel(x, y, color);
        }
        public Color GetPixel(Point point)
        {
            sbyte color = Image[point.X, point.Y];
            return GetColor(color);
        }

        //Pb -> ImageSharp
        public void Render()
        {
            Bitmap.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < Width; x++)
                    {
                        if (!_changeMap[x, y])
                            continue;
                        _changeMap[x, y] = false;
                        pixelRow[x] = GetColor(Image[x, y]).ToPixel<Rgba32>();
                    }
                }
            });
        }

        //ImageSharp -> Pb
        public void Commit()
        {
            Bitmap.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < Width; x++)
                    {
                        _changeMap[x, y] = false;
                        ref Rgba32 color = ref pixelRow[x];
                        if (color.A == 0)
                        {
                            Image[x, y] = -1;
                            break;
                        }
                        sbyte set = -2;
                        for (int i = 0; i < Palette.Length; i++)
                        {
                            if (Palette[i].Equals(color))
                            {
                                set = (sbyte)i;
                                break;
                            } 
                        }
                        if (set != -2)
                            Image[x, y] = set;
                        else
                            throw new Exception();
                    }
                }
            });
        }
        public void Constraint()
        {
            Bitmap.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < Width; x++)
                    {
                        pixelRow[x] = GetBest(pixelRow[x]);
                    }
                }
            });
        }

        private Rgba32 GetBest(Rgba32 color)
        {
            var startingRgb = new Rgb { R = color.R, G = color.G, B = color.B };
            Rgb? min = RGB.MinBy(x => x.Compare(startingRgb, _colorSpaceComparison));
            if (min == null)
                return Color.Transparent;
            return new Rgba32((byte)min.R, (byte)min.G, (byte)min.B, 255);
        }
    }
}
