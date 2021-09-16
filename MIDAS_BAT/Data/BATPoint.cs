using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace MIDAS_BAT.Data
{
    class BATPoint
    {
        public Point point { get; set; }
        public float pressure { get; set; }
        public ulong timestamp { get; set; }

        public bool isEnd { get; set; }
        public BATPoint(Point point, float pressure, ulong timestamp)
        {
            this.point = point;
            this.pressure = pressure;
            this.timestamp = timestamp;
            this.isEnd = false;
        }

        public static double getTimeDiffMs(BATPoint a, BATPoint b)
        {
            return Convert.ToDouble((a.timestamp - b.timestamp)) / 1000.0;
        }
    }
}
