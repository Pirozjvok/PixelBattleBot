using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Impl;
using PixelBattleBotCore.Impl.PointSelectors;
using PixelBattleBotCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelBattleBotCore
{

    //Нужно сделать так чтобы таски можно было менять на лету
    public class PixelBattleBotMaster
    {
        public PixelBattleTask? PixelBattleTask => _printer.Current;
        public bool Started { get; private set; }

        private CompositeImage _place;

        private BotDispatcher _botDispatcher;

        private PixelBattlePrinter _printer;
        public IProxyFactory ProxyFactory { get => _botDispatcher.ProxyFactory; set => _botDispatcher.ProxyFactory = value; }
        public IReadOnlyList<BotInfo> Bots => _botDispatcher.BotsList;
        public IVkCaptchaSolver? VkCaptchaSolver { get => _botDispatcher.VkCaptchaSolver; set => _botDispatcher.VkCaptchaSolver = value; }
        public IPBCaptchaSolver? PBCaptchaSolver { get => _botDispatcher.IPBCaptchaSolver; set => _botDispatcher.IPBCaptchaSolver = value; }
        public string Data { get; set; } = "data.json";
        public int Progress { get => _printer.Progress; }
        public int SuperPixels { get => _printer.UseSuperPixels; set => _printer.UseSuperPixels = value; }

        private BotPersistnse _botPersistnse = new BotPersistnse();
        public PixelBattleBotMaster()
        {
            _place = new CompositeImage(new PixelBattleImage(1590, 400));
            _botDispatcher = new BotDispatcher(_place, new ProxyFactory(new IWebProxy[] { }));
            _printer = new PixelBattlePrinter(_place, new IBot[] { });
            _botDispatcher.BotInited += (object? s, BotInfo a) => _printer.AddBot(a.Bot ?? throw new ArgumentNullException("BotInfo"));
            _printer.UseSuperPixels = 1;
        }
        public void SetTask(PixelBattleTask pixelBattleTask)
        {
            _printer.Current = pixelBattleTask;
        }
        public IDisposable SetPlace(IImage place)
        {
            return _place.AddImage(place);
        }
        public async Task AddBots(BotInfo[] bots)
        {
            await _botDispatcher.AddBots(bots, CancellationToken.None);
        }
        public async Task Start(int workers, CancellationToken cancellationToken)
        {
            if (File.Exists(Data))
                await Load();

            Console.WriteLine("Starting bots...");
            Task autoSave = AutoSave(cancellationToken);
            await _botDispatcher.StartBots(cancellationToken);
            await Save();
            Console.WriteLine("Starting printer...");
            Task printer = _printer.Start(workers, cancellationToken);
            Task wait = _botDispatcher.Wait(cancellationToken);
            await Task.WhenAll(printer, wait, autoSave);
            await Save();
        }

        private async Task Load()
        {
            string data = await File.ReadAllTextAsync(Data);
            try
            {
                _botPersistnse = JsonSerializer.Deserialize<BotPersistnse>(data) ?? new BotPersistnse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
            await AddBots(_botPersistnse.Bots.ToArray());
        }
        private async Task Save()
        {
            _botPersistnse.Bots = Bots.ToList();
            string data = JsonSerializer.Serialize(_botPersistnse);
            await File.WriteAllTextAsync(Data, data);
        }

        private async Task AutoSave(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Autosave...");
                await Save();
                Console.WriteLine("Saved...");
                await Task.Delay(30000, cancellationToken);
            }
        }
    }
}
