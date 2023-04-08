using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IImage
    {
        public int Width { get; }
        public int Height { get; }
        public sbyte GetPixel(int x, int y);
        public void SetPixel(int x, int y, sbyte color);
    }
}
