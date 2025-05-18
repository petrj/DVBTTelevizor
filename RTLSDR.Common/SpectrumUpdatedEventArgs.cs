using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    public class SpectrumUpdatedEventArgs : EventArgs
    {
        public Point[] Data { get; set; }
        public int ymax;
        public int ymin;
        public int xmax;
        public int xmin;
    }
}
