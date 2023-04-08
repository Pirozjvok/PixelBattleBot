using PixelBattleBotCore.Impl;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Model
{
    public class PixelBattleCaptcha
    {
        public string Path { get; set; }

        public PixelBattleCaptcha(string path)
        {
            Path = path;
        }
        public static PixelBattleCaptcha ParseSvg(string code)
        {
            var byteArray = Encoding.ASCII.GetBytes(code);
            using (var stream = new MemoryStream(byteArray))
            {
                var svgDocument = SvgDocument.Open<SvgDocument>(stream);
                var bitmap = svgDocument.Draw();
                string path = "captcha/" + DateTime.UtcNow.ToString("dd-MM-yyyy-HH-mm-ss") + ".jpg";
                bitmap.Save(path);
                return new PixelBattleCaptcha(path);
            }
        }
    }
}
