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
        public Point? org { get; set; }
        public Point? drawn { get; set; }
        public bool hasValueOrg { get; set; }
        public bool hasValueDrawn { get; set; }

        public DiffData(string name, Point org)
        {
            this.name = name;
            this.org = org;
            this.drawn = null;
            this.hasValueOrg = true;
            this.hasValueDrawn = false;
        }

        public DiffData(string name, Point? org, Point? drawn)
        {
            this.name = name;
            if (org.HasValue)
            {
                this.org = org.Value;
                this.hasValueOrg = true;
            }
            else
            {
                this.org = null;
                this.hasValueOrg = false;
            }

            if (drawn.HasValue)
            {
                this.drawn = drawn.Value;
                this.hasValueDrawn = true;
            }
            else
            {
                this.drawn = null;
                this.hasValueDrawn = false;
            }

        }

        public double getDistance()
        {
            if (!hasValueOrg || !hasValueDrawn)
                return -1;

            return Math.Sqrt((org.Value.X - drawn.Value.X) * (org.Value.X - drawn.Value.X) + (org.Value.Y - drawn.Value.Y) * (org.Value.Y - drawn.Value.Y));
        }
    }
}
