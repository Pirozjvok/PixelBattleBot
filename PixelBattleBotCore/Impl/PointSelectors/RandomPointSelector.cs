using PixelBattleBotCore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl.PointSelectors
{
    public class RandomPointSelector : IPointSelector
    {
        private Random _random;
        public RandomPointSelector()
        {
            _random = new Random((int)DateTime.Now.Ticks);
        }

        public IEnumerable<(int, int)> Select(IEnumerable<(int, int)> values)
        {
            return values.OrderBy(x => _random.Next());
        }
    }
}
