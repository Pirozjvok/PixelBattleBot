using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore
{
    public class WsSession : IDisposable
    {
        private ClientWebSocket? _client;
        public IWebProxy? WebProxy { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
        public Uri Uri { get; set; }

        private readonly Subject<WsMsg> _onMsg;
        public IObservable<WsMsg> OnMessage => _onMsg;
        public WsSession(Uri uri)
        {
            RequestHeaders = new Dictionary<string, string>();
            Uri = uri;
            _onMsg = new Subject<WsMsg>();
        }
        private void Init()
        {
            _client = new ClientWebSocket();
            foreach (var item in RequestHeaders)
            {
                _client?.Options.SetRequestHeader(item.Key, item.Value);
            }
        }

        public async Task<bool> SendBinary(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_client == null)
                return false;

            var arr = new ArraySegment<byte>(buffer, offset, count);
            await _client.SendAsync(arr, WebSocketMessageType.Binary, true, cancellationToken);
            return true;
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            Init();
            if (_client == null)
                throw new NullReferenceException(nameof(_client));
            await _client.ConnectAsync(Uri, cancellationToken);
        }
        public async Task Start(CancellationToken cancellationToken)
        {
            try
            {
                if (_client == null)
                    throw new NullReferenceException(nameof(_client));
                byte[] buffer = new byte[16556];
                while (!cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _client.ReceiveAsync(buffer, cancellationToken);
                    _onMsg.OnNext(new WsMsg { Buffer = new ArraySegment<byte>(buffer, 0, result.Count), MessageType = result.MessageType });
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                }
            }
            finally
            {
                _client?.Dispose();
            }
            
        }
        public void Dispose()
        {
            _client?.Dispose();
            _onMsg?.Dispose();
            Console.WriteLine("Disposed");
        }
    }

    public class WsMsg
    {
        public ArraySegment<byte> Buffer { get; init; }
        public WebSocketMessageType MessageType { get; init; }
    }
}
