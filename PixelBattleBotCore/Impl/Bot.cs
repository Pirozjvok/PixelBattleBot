using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
using PixelBattleBotCore.Vk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl
{
    public enum BotState : int
    {
        Disbled = 0,
        Starting = 1,
        Ready = 2,
        Busy = 3,
        Died = 4,
    }

    public class Bot : IBot
    {
        private WsSession _session;
        public TimeSpan SendInterval { get; set; }
        public PlaceInfo PlaceInfo { get; set; }
        public DateTime NextTryAllowAt { get; private set; }
        public int Bomb { get; private set; }
        public int Freeze { get; private set; }
        public int Pixel { get; private set; }
        public int SinglePixel { get; private set; }

        private TimeSpan _ttl = TimeSpan.FromSeconds(60);

        //Бот жив
        public bool BotAlive { get; private set; }

        //Бот запущен
        public bool BotStarted { get; private set; }
        public IPlaceUpdater PlaceUpdater => new BotPlaceUpdater(this, PlaceInfo);

        private Lazy<BotPlaceUpdater> _packetObserver;
        public IObservable<PixelBattlePacket> PacketObserver => _packetObserver.Value.OnMessage;
        
        //Бот занят
        public bool BotBusy { get; private set; }

        //Бот готов
        public bool BotReady { get; private set; }
        //Бот активирован
        public bool BotEnabled { get; set; } = true;
        public Bot(WsSession wsSession)
        {
            _session = wsSession;
            _session.OnMessage.Where(x => x.MessageType == WebSocketMessageType.Text).Subscribe(x => ProccessWsMessage(x));
            SendInterval = TimeSpan.FromMilliseconds(500);
            PlaceInfo = new PlaceInfo();
            _packetObserver = new Lazy<BotPlaceUpdater>(() => new BotPlaceUpdater(this, PlaceInfo));
            NextTryAllowAt = DateTime.Now;
        }

        public void UpdateWsUri(Uri wsUri)
        {
            _session.Uri = wsUri;
        }

        private object _lock = new object();

        public async Task Start(CancellationToken cancellationToken)
        {
            lock (_lock)
                if (BotStarted)
                    return;
            lock (_lock)
            {
                BotAlive = true;
                BotStarted = true;
            }           
            try
            {
                await _session.Connect(cancellationToken);
                await _session.Start(cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ws error, {e.Message}");
                lock (_lock)
                    BotAlive = false;
            }
            finally
            {
                lock (_lock)
                {
                    BotReady = false;
                    BotStarted = false;
                }              
            }
        }
        public async Task ExecuteTask(BotTask botTask, CancellationToken cancellationToken)
        {
            if (!BotEnabled)
                return;
            try
            {
                BotBusy = true;
                foreach (PixelData pixel in botTask.Pixels)
                {
                    int data = pixel.Pack(PlaceInfo);
                    byte[] buffer = BitConverter.GetBytes(data);
                    Console.WriteLine($"Bot try send pixel {pixel.X}, {pixel.Y}, {pixel.Color}, {pixel.Flag}");
                    await _session.SendBinary(buffer, 0, buffer.Length, cancellationToken);
                    Console.WriteLine($"Bot sended pixel {pixel.X}, {pixel.Y}, {pixel.Color}, {pixel.Flag}");
                    await Task.Delay(SendInterval);
                }
            }
            catch
            {

            }
            finally
            {
                NextTryAllowAt = DateTime.Now.Add(_ttl);
                BotBusy = false;
            }
            
        }
        private void ProccessWsMessage(WsMsg wsMsg)
        {
            string text = Encoding.UTF8.GetString(wsMsg.Buffer);
            JsonDocument jsonDocument;
            try
            {
                jsonDocument = JsonDocument.Parse(text);
                BotReady = true;
            }
            catch
            {
                throw;
            }
            
            JsonElement v = jsonDocument.RootElement.GetProperty("v");

            switch (jsonDocument.RootElement.GetProperty("t").GetUInt32())
            {
                case 12:
                    Process12(v);
                    break;
                case 2:
                    Process2(v);
                    break;
                case 8:
                    Process8(v);
                    break;
            }
        }

        private void Process12(JsonElement jsonElement)
        {
            foreach (JsonElement item in jsonElement.EnumerateArray())
            {
                int t = item.GetProperty("t").GetInt32();
                JsonElement v = item.GetProperty("v");
                switch (t)
                {
                    case 2:
                        Process2(v);
                        break;
                    case 8:
                        Process8(v);
                        break;
                }
            }
        }

        private void Process2(JsonElement jsonElement)
        {
            _ttl = TimeSpan.FromMilliseconds(jsonElement.GetProperty("ttl").GetInt32());
            if (jsonElement.TryGetProperty("wait", out JsonElement wait))
            {               
                int val = wait.GetInt32();
                if (val != 0)
                    NextTryAllowAt = DateTime.Now.AddMilliseconds(val);
            }
        }
        private void Process8(JsonElement jsonElement)
        {
            Bomb = jsonElement.GetProperty("bomb").GetInt32();
            Freeze = jsonElement.GetProperty("freeze").GetInt32();
            Pixel = jsonElement.GetProperty("pixel").GetInt32();
            SinglePixel = jsonElement.GetProperty("singlePixel").GetInt32();
        }

        private class BotPlaceUpdater : IPlaceUpdater
        {
            private readonly Bot _bot;

            private PlaceInfo _placeInfo;

            private IImage? _pixelBattleImage;

            private IDisposable _observer;

            private Subject<PixelBattlePacket> _subject;
            public IObservable<PixelBattlePacket> OnMessage => _subject;
            public BotPlaceUpdater(Bot bot, PlaceInfo placeInfo)
            {
                _bot = bot;
                _observer = _bot._session.OnMessage.Where(x => x.MessageType == WebSocketMessageType.Binary).Subscribe(Update);
                _placeInfo = placeInfo;
                _subject = new Subject<PixelBattlePacket>();
            }
            public void Dispose()
            {
                _observer?.Dispose();
                _subject?.Dispose();
                _pixelBattleImage = null;
            }
            public IDisposable SetImage(IImage battleImage)
            {
                _pixelBattleImage = battleImage;
                return new Disp(this);
            }
            private void Update(WsMsg update)
            {
                for (int i = 0; i < update.Buffer.Count; i+=12)
                {
                    int index = i + update.Buffer.Offset;
                    PixelBattlePacket packet = PixelBattlePacket.Parse(_placeInfo, update.Buffer.Array ?? throw new NullReferenceException(), index);
                    PixelData pixelData = packet.PixelData;
                    _subject.OnNext(packet);
                    _pixelBattleImage?.SetPixel(pixelData.X, pixelData.Y, (sbyte)pixelData.Color);
                }
            }

            private class Disp : IDisposable
            {
                private BotPlaceUpdater botPlaceUpdater;

                public Disp(BotPlaceUpdater _botPlaceUpdater)
                {
                    botPlaceUpdater = _botPlaceUpdater;  
                }
                public void Dispose()
                {
                    botPlaceUpdater._pixelBattleImage = null;
                }
            }
        }
    }    
}
