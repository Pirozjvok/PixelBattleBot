using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using VkNet.Utils;

namespace PixelBattleBotCore.Extensions
{
    public static class WsSessionExtensions
    {
        public static void AddPixelBattleHeaders(this WsSession wsSession, Uri host, Uri origin)
        {
            wsSession.RequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36 Edg/111.0.1661.62");
            wsSession.RequestHeaders.Add("Host", host.Host);
            wsSession.RequestHeaders.Add("Origin", origin.Host);
            wsSession.RequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            wsSession.RequestHeaders.Add("Cache-Control", "no-cache");
            wsSession.RequestHeaders.Add("Pragma", "no-cache");
            wsSession.RequestHeaders.Add("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
        }
    }
}
