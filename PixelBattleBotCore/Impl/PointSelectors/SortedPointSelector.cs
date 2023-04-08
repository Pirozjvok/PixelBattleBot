using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Impl.PointSelectors
{
    public class SortedPointSelector : IPointSelector
    {
        public bool Descending { get; set; }

        private PointComparer _comparer = new PointComparer();
        public SortedPointSelector()
        { 
        
        }
        public IEnumerable<(int, int)> Select(IEnumerable<(int, int)> values)
        {
            if (Descending)
                return values.OrderByDescending(x => x, _comparer);
            return values.OrderBy(x => x, _comparer);
        }
    }
}
