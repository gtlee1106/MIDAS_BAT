using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace MIDAS_BAT.Data
{
    class TImePoint
    {
        public double time { get; set; }
        public Point point { get; set; }
        public TImePoint(double time, Point point)
        {
            this.time = time;
            this.point = point;
        }

    }
}
