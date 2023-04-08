using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl.PointSelectors
{
    public class SuperPointerSelector : IPointSelector
    {
        public int Step { get; set; }

        private PointComparer _comparer = new PointComparer();
        public SuperPointerSelector()
        {
            Step = 1;
        }
        public IEnumerable<(int, int)> Select(IEnumerable<(int, int)> values)
        {
            int lastX = 0;
            return values.OrderBy(x => x, _comparer)
                  .Where(x =>
                  {
                      bool res = Math.Abs(x.Item1 - lastX) > Step;
                      if (res)
                        lastX = x.Item1;
                      return res;
                  });
        }
    }
}
