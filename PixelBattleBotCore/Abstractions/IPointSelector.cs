using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IPointSelector
    {
        IEnumerable<(int, int)> Select(IEnumerable<(int, int)> values);
    }
}
