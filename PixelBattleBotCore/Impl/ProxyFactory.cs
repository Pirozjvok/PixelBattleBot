using PixelBattleBotCore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl
{
    public class ProxyFactory : IProxyFactory
    {
        public IWebProxy[] Proxies { get; }
        private int _pointer { get; set; }
        public ProxyFactory(IEnumerable<IWebProxy> webProxies) 
        { 
            Proxies = webProxies.ToArray();
        }
        public IWebProxy GetProxy(uint user_id)
        {
            if (Proxies.Length == 0)
                throw new Exception("Pustota");

            int idx = _pointer % Proxies.Length;
            _pointer = idx;
            return Proxies[_pointer++];
        }
    }
}
