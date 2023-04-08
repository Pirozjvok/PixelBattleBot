using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Comparers
{
    public class PointComparer : IComparer<(int, int)>
    {
        public int Compare((int, int) a, (int, int) b)
        {
            if (a.Item1 == b.Item1 && a.Item2 == b.Item2)
                return 0;

            if (a.Item2 < b.Item2)
                return -1;

            if (a.Item2 == b.Item2)
            {
                if (a.Item1 < b.Item1)
                    return -1;
                else
                    return 1;
            }

            return 1;
        }
    }
}
