using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Impl;
using PixelBattleBotCore.Impl.PointSelectors;
using PixelBattleBotCore.Model;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelBattleBotCore
{
    public class PixelBattlePrinter
    {
        public IImage Place { get; set; }

        private object _lock = new object();

        private PixelBattleTask? _current;

        public PixelBattleTask? Current
        {
            get
            {
                return _current;
            }
            set
            {
                lock (_lock)
                    _current = value;
            }
        }
        public IPointSelector PointSelector { get; set; }

        private SuperWorkers<PixelsTask> _workers;

        public int Progress { get; private set; }
        public List<IBot> Bots { get; set; }

        /// <summary>
        /// -1 - Хоть сколько
        /// 0 - Запрещено
        /// Остальное это количество молний
        /// </summary>
        public int UseSuperPixels { get; set; }
        private int _maxSetPixelsInSuper = 30;

        private Dictionary<IBot, bool> _botStates = new Dictionary<IBot, bool>();
        public PixelBattlePrinter(IImage place, IEnumerable<IBot> bots)
        {
            Place = place;
            PointSelector = new SortedPointSelector();
            Bots = bots.ToList();
            _workers = new SuperWorkers<PixelsTask>(Work);
        }

        private object _listLsock = new object();
        public void AddBot(IBot bot)
        {
            lock (_listLsock)
                Bots.Add(bot);
        }
        public async Task Start(int worker, CancellationToken cancellationToken)
        {
            Task workers = _workers.Start(worker, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                int index = 0;
                var points = PointSelector.Select(Difference()).ToArray();

                Progress = points.Count();

                if (points.Length == 0)
                    await Task.Delay(100);

                List<IBot> bots;
                lock (_listLsock)
                    bots = Bots.ToList();

                foreach (IBot bot in bots.Where(x => x.BotEnabled && x.BotReady && x.BotBusy == false && x.NextTryAllowAt <= DateTime.Now))
                {
                    if (!_botStates.ContainsKey(bot))
                        _botStates[bot] = false;
                    else if (_botStates[bot])
                        continue;
                     

                    if (index >= points.Length)
                        break;

                    if (bot.Pixel > 0 && points.Length >= _maxSetPixelsInSuper && (UseSuperPixels == -1 || UseSuperPixels > 0))
                    {
                        IEnumerable<PixelData> pixels;
                        lock (_lock)
                        {
                            if (_current == null)
                                throw new NullReferenceException();
                            pixels = points.Take(_maxSetPixelsInSuper)
                            .Select(x => new PixelData()
                            {
                                X = x.Item1,
                                Y = x.Item2,
                                Color = Current!.Image.GetPixel(x.Item1 - Current.X, x.Item2 - Current.Y),
                                Flag = 3,
                            }).ToList();
                            index += _maxSetPixelsInSuper;
                        }
                        foreach (PixelData pixel in pixels)
                        {
                            Place.SetPixel(pixel.X, pixel.Y, (sbyte)pixel.Color);
                        }
                        UseSuperPixels--;
                        await _workers.AddTask(new PixelsTask(bot, new BotTask(pixels)), cancellationToken);
                    } 
                    else
                    {
                        var x = points[index++];
                        PixelData pixelData = new PixelData()
                        {
                            X = x.Item1,
                            Y = x.Item2,
                            Color = Current!.Image.GetPixel(x.Item1 - Current.X, x.Item2 - Current.Y),
                            Flag = 0,
                        };
                        Place.SetPixel(pixelData.X, pixelData.Y, (sbyte)pixelData.Color);
                        await _workers.AddTask(new PixelsTask(bot, BotTask.FromSingle(pixelData)), cancellationToken);
                    }
                    _botStates[bot] = true;
                    await Task.Delay(2500);
                }
                await Task.Delay(100);
            }
            await workers;
        }

        private async Task Work(PixelsTask pixelsTask, CancellationToken cancellationToken)
        {
            await pixelsTask.Bot.ExecuteTask(pixelsTask.BotTask, cancellationToken);
            _botStates[pixelsTask.Bot] = false;
        }
        public IEnumerable<(int, int)> Difference()
        {
            if (Current == null)
                throw new NullReferenceException();

            int xmax = Current.X + Current.Image.Width;
            int ymax = Current.Y + Current.Image.Height;

            for (int x = Current.X; x < xmax; x++)
            {
                for (int y = Current.Y; y < ymax; y++)
                {
                    var a = Place.GetPixel(x, y);
                    var b = Current.Image.GetPixel(x - Current.X, y - Current.Y);
                    if (a != b && b != -1)
                        yield return (x, y);
                }
            }
            yield break;
        } 
    }
}
