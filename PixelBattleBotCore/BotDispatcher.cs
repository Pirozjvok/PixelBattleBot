using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Extensions;
using PixelBattleBotCore.Model;
using VkNet.Utils;
using PixelBattleBotCore.Impl;
using System.Threading;
using PixelBattleBotCore.Vk;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Collections.Generic;

namespace PixelBattleBotCore
{

    //Задача бот диспатчера это следить за состоянием аккаунтов и прокси
    public class BotDispatcher
    {
        private List<BotInfo> Bots;

        public IReadOnlyList<BotInfo> BotsList
        {
            get
            {
                IReadOnlyList<BotInfo> list;
                lock (_botListLock)
                    list = Bots.AsReadOnly();

                return list;
                    
            }
        }

        public object _tasksListLock = new object();

        public object _botListLock = new object();

        private IProxyFactory _proxyFactory;
        public IProxyFactory ProxyFactory { get => _proxyFactory; set => _proxyFactory = value; }
        public IVkCaptchaSolver? VkCaptchaSolver { get; set; }

        public IPBCaptchaSolver? IPBCaptchaSolver { get; set; }
        public IImage PBImage { get; }

        public event EventHandler<BotInfo>? BotInited;
        public BotDispatcher(IImage image, IProxyFactory proxyFactory)
        {
            PBImage = image;
            _proxyFactory = proxyFactory;
            Bots = new List<BotInfo>();
        }
        public BotDispatcher(IImage image, IProxyFactory proxyFactory, IEnumerable<BotInfo> bots)
        {
            PBImage = image;
            _proxyFactory = proxyFactory;
            Bots = new List<BotInfo>();
        }

        private List<Task> _tasks = new List<Task>();

        private bool _started = false;
        public async Task StartBots(CancellationToken cancellationToken)
        {
            if (_started)
                return;
            try
            {
                _started = true;
                Uri data = await InitBots(true, true, cancellationToken) ?? throw new Exception();
                Console.WriteLine("Running place updater");
                Task task = await StartImageUpdating(data, cancellationToken);
                Console.WriteLine("Place updater runned");
                _tasks.Add(InitBots(false, false, cancellationToken));
                _tasks.Add(PlaceUpdater(cancellationToken));
                _tasks.Add(CheckBots(cancellationToken));
                _tasks.Add(ProxyCheck(cancellationToken));
                _tasks.Add(CheckWs(cancellationToken));
                _tasks.Add(TaskListGC(cancellationToken));
            }
            finally
            {
                
            }
        }   

        public async Task TaskListGC(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_tasksListLock)
                    _tasks.RemoveAll(x => x.IsCompleted);
                await Task.Delay(60000, cancellationToken);
            }      
        }
        private async Task CheckWs(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                BotInfo[] bots;
                lock (_botListLock)
                    bots = Bots.Where(x => x.Bot != null && x.Bot.BotEnabled && !x.Bot.BotStarted).ToArray();
                foreach (BotInfo bot in bots)
                {
                    try
                    {
                        Console.WriteLine($"Restart bot {bot.BotId}");
                        PbUrls pbUrls = await InitUrls(bot, cancellationToken);
                        bot.Bot!.UpdateWsUri(pbUrls.Ws);
                        Task task = bot.Bot.Start(cancellationToken);
                        _tasks.Add(task);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ws url update to bot {bot.BotId} {ex.Message}");
                    }
                }
                await Task.Delay(30000, cancellationToken);
            }
        }
        public async Task AddBot(BotInfo bot, CancellationToken cancellationToken)
        {
            lock (_botListLock)
            {
                Bots.RemoveAll(x => x.User.Login == bot.User.Login);
                Bots.Add(bot);
            }
               
            if (_started)
                try
                {
                    await InitBot(false, bot, cancellationToken);
                }
                catch (HttpRequestException)
                {

                }        
        }

        public async Task AddBots(IEnumerable<BotInfo> bots, CancellationToken cancellationToken)
        {
            foreach (BotInfo bot in bots)
                await AddBot(bot, cancellationToken);
        }
        private async Task CheckBots(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {

                BotInfo[] bots = new BotInfo[Bots.Count];
                lock (_botListLock)
                    Bots.CopyTo(bots);
                foreach (BotInfo bot in bots)
                {
                    try
                    {
                        await InitBot(false, bot, token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Init eror {ex.Message}");
                    }
                    
                    await Task.Delay(1000, token);
                }
            }
        }
        public async Task Wait(CancellationToken? cancellation = default)
        {
            while ((!cancellation?.IsCancellationRequested) ?? true)
            {
                Task[] tasks;
                lock (_tasksListLock)
                    tasks = _tasks.Where(x => x.IsCompleted == false).ToArray();
                if (tasks.Length == 0)
                    break;
                foreach (Task task in tasks)
                    await task;
            }
        }
        private async Task<Uri?> InitBots(bool one, bool getUri, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Uri? data = null;
                    bool getDataUrl = getUri;
                    BotInfo[] bots;
                    lock (_botListLock)
                        bots = Bots.ToArray();
                    List<Task> tasks = new List<Task>();
                    foreach (BotInfo bot in bots)
                    {
                        Uri? url = null;
                        if (getUri)
                            url = await InitBot(getDataUrl, bot, cancellationToken);
                        else
                            tasks.Add(InitBot(getDataUrl, bot, cancellationToken));
                        if (data == null)
                            data = url;
                        if (data != null)
                            getDataUrl = false;
                        if (one && data != null)
                            return data;
                    }
                    await Task.WhenAll(tasks);
                    if (data != null)
                        return data;
                    if (getDataUrl)
                    {
                        Console.WriteLine("Data Url retry...");
                        await Task.Delay(1000, cancellationToken);
                    }            
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Super mega error. Reconnect...");
                    await Task.Delay(1000, cancellationToken);
                }                    
            }
            return null;
        }
        private async Task<Uri?> InitBot(bool getDataUrl, BotInfo bot, CancellationToken cancellationToken)
        {
            if (bot.Busy)
                return null;
            if (bot.User.IncorrectPassword)
                return null;
            try
            {
                bot.Busy = true;
                if (VkCaptchaSolver != null)
                    bot.VkHtmlApi.VkCaptchaSolver = VkCaptchaSolver;
                if (!await bot.VkHtmlApi.IsAuthorized())
                {
                    try
                    {
                        await bot.VkHtmlApi.Authorize();
                    }
                    catch (VkAuthException e)
                    {
                        Console.WriteLine($"Bot {bot.User.Login} auth error: {e.Code} {e.Message}");
                    }
                }          

                Uri? data = null;
                try
                {
                    if (getDataUrl || bot.User.WsUri == null || bot.User.Origin == null)
                    {
                        PbUrls urls = await InitUrls(bot, cancellationToken);
                        data = urls.Data;
                    }
                    if (bot.Bot == null)
                    {
                        WsSession wsSession = new WsSession(bot.User.WsUri!.ReplaceTs());
                        bot.WsSession = wsSession;
                        wsSession.AddPixelBattleHeaders(bot.User.WsUri!, bot.User.Origin!);
                        if (bot.WebProxy != null)
                            wsSession.WebProxy = bot.WebProxy;
                        bot.Bot = new Bot(wsSession);
                        BotInited?.Invoke(this, bot);
                        _tasks.Add(bot.Bot.Start(cancellationToken));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Init bot error. Bot {bot.BotId}, {e.Message}");
                }
                return data;
            }
            finally
            {
                bot.Busy = false;
            }      
        }

        private async Task<Task> StartImageUpdating(Uri dataUri, CancellationToken cancellationToken)
        {
            HttpClient httpClient = new HttpClient();
            DataPlaceUpdater placeUpdater = new DataPlaceUpdater(dataUri, httpClient);
            placeUpdater.SetImage(PBImage);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await placeUpdater.Update(cancellationToken);
                    break;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("Error get place image, reconnect... {0}", e.Message);
                    await Task.Delay(10000);             
                }
            }
           
            return placeUpdater.Start(cancellationToken);
        }
        private async Task<PbUrls> InitUrls(BotInfo bot, CancellationToken cancellationToken)
        {
            if (IPBCaptchaSolver != null)
                bot.PixelBattle.IPBCaptchaSolver = IPBCaptchaSolver;

            PbUrls urls = await bot.PixelBattle.GetUrls(cancellationToken);
            bot.User.WsUri = urls.Ws;
            bot.User.Origin = urls.Origin;
            return urls;
        }

        private Bot? _placeUpdaterBot;

        private IPlaceUpdater? _currentPlaceUpdater;

        private IDisposable? _disposable;
        private async Task PlaceUpdater(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if ((!_placeUpdaterBot?.BotAlive) ?? true)
                {
                    BotInfo? botInfo;
                    lock (_botListLock)
                        botInfo = Bots.FirstOrDefault(x => x.Bot?.BotAlive ?? false);
                    if (botInfo == null)
                        return;
                    _placeUpdaterBot = botInfo.Bot!;
                    _disposable?.Dispose();
                    _currentPlaceUpdater = botInfo.Bot!.PlaceUpdater;
                    _disposable = _currentPlaceUpdater.SetImage(PBImage);
                }
                await Task.Delay(10000, cancellationToken);
            }
        }
        private async Task ProxyCheck(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    BotInfo[] bots;
                    lock (_botListLock)
                        bots = Bots.Where(x => x.WebProxy is not null).ToArray();

                    foreach (var proxy in bots.GroupBy(x => x.WebProxy))
                    {
                        using ProxyChecker checker = new ProxyChecker(proxy.Key!);
                        bool result = await checker.Check(cancellationToken);
                        if (!result)
                            foreach (BotInfo bot in proxy.Where(x => x.ReplaceProxyIfDied))
                            {
                                try
                                {
                                    bot.WebProxy = _proxyFactory.GetProxy(bot.BotId);
                                    if (bot.WsSession != null)
                                        bot.WsSession.WebProxy = _proxyFactory.GetProxy(bot.BotId);
                                }
                                catch
                                {

                                }
                            }
                    }
                }
                catch
                {

                }                
                await Task.Delay(10000, cancellationToken);
            }    
        }
    }
}
