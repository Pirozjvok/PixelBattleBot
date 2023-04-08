using PixelBattleBotCore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl
{
    public class CompositeImage : IImage
    {
        private List<IImage> _images;
        public IImage MainImage { get; }
        public CompositeImage(IImage main)
        {
            MainImage = main;
            _images = new List<IImage>();
        }
        public IDisposable AddImage(IImage image)
        {
            if (image.Width != MainImage.Width || image.Height != MainImage.Height)
                throw new Exception("Size not equals");
            _images.Add(image);
            return new Remover(this, image);
        }
        public int Width => MainImage.Width;
        public int Height => MainImage.Height;
        public sbyte GetPixel(int x, int y)
        {
            return MainImage.GetPixel(x, y);
        }
        public void SetPixel(int x, int y, sbyte color)
        {
            MainImage.SetPixel(x, y, color);
            foreach (var image in _images)
            {
                image.SetPixel(x, y, color);
            }
        }

        private class Remover : IDisposable
        {
            private readonly CompositeImage _image;

            private readonly IImage _item;
            public Remover(CompositeImage image, IImage item)
            {
                _image = image;
                _item = item;
            }
            public void Dispose()
            {
                _image._images.Remove(_item);
            }
        }
    }
}
