using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace MIDAS_BAT.Data
{
    class DiffData
    {
        public string name { get; set; }
        public Point org { get; set; }
        public Point drawn { get; set; }
        public bool hasValue { get; set; }

        public DiffData(string name, Point org)
        {
            this.name = name;
            this.org = org;
            this.hasValue = false;
        }

        public DiffData(string name, Point org, Point drawn)
        {
            this.name = name;
            this.org = org;
            this.drawn = drawn;
            this.hasValue = true;
        }

        public double getDistance()
        {
            if (!hasValue)
                return -1;

            return Math.Sqrt((org.X - drawn.X) * (org.X - drawn.X) + (org.Y - drawn.Y) * (org.Y - drawn.Y));
        }
    }
}
