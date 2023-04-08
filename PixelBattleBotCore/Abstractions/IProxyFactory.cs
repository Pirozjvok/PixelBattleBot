using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IProxyFactory
    {
        IWebProxy GetProxy(uint user_id);
    }
}
