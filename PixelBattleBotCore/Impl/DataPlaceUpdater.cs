using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
using SixLabors.ImageSharp.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl
{
    public class DataPlaceUpdater : IPlaceUpdater
    {

        private static string _map = "0123456789abcdefghijklmno";

        private static byte[] _asciiMap = Encoding.ASCII.GetBytes(_map);

        private IImage? _image;
        public TimeSpan UpdateInterval { get; set; }

        private CancellationTokenSource? _cts;
        public Uri DataUri { get; set; }

        private HttpClient _httpClient;

        private PlaceInfo _placeInfo;
        public DataPlaceUpdater(Uri dataUri, HttpClient httpClient)
        {
            UpdateInterval = TimeSpan.FromSeconds(30);
            DataUri = dataUri;
            _httpClient = httpClient;
            _placeInfo = new PlaceInfo();
        }
        public void Dispose()
        {
            _cts?.Cancel();
            _image = null;
            _httpClient?.Dispose();
        }
        public IDisposable SetImage(IImage battleImage)
        {
            _image = battleImage;
            
            return new Disp(this);
        }
        public async Task Update(CancellationToken cancellationToken)
        {
            if (_image == null)
                throw new NullReferenceException();
            byte[] buffer = new byte[1590 * 20];
            using HttpResponseMessage resp = await _httpClient.GetAsync(DataUri, cancellationToken);
            using Stream stream = resp.Content.ReadAsStream();
            long? cl = resp.Content.Headers.ContentLength;
            int image_index = 0;
            bool br = false;
            while (true)
            {
                int readed = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (readed == 0)
                    break;
                for (int i = 0; i < readed; i++)
                {
                    byte data = buffer[i];
                    if (!_asciiMap.Contains(data))
                    {
                        br = true;
                        break;
                    }

                    int x = image_index % _placeInfo.Width;
                    int y = image_index / _placeInfo.Width;
                    sbyte color = (sbyte)Array.IndexOf(_asciiMap, data);
                    _image.SetPixel(x, y, color);
                    image_index++;
                    if (image_index >= _placeInfo.Width * _placeInfo.Height)
                    {
                        br = true;
                        break;
                    }
                }
                if (br)
                    break;
            }
        }
        public async Task Start(CancellationToken ct)
        {
            if (_image == null)
                return;
            _cts = new CancellationTokenSource();
            ct.Register(() => _cts.Cancel());
            CancellationToken cancellationToken = _cts.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Update(cancellationToken);
                }
                catch
                {

                }                
                await Task.Delay(UpdateInterval, cancellationToken);
            }
        }
        private class Disp : IDisposable 
        {
            DataPlaceUpdater placeUpdater;
            public Disp(DataPlaceUpdater placeUpdater) { this.placeUpdater = placeUpdater; }
            public void Dispose()
            {
                placeUpdater._image = null;
            }
        }
    }
}
