using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stamper.DataAccess
{
    public class Layer
    {
        public string Name { get; set; }
        public string File { get; set; }
        public string Mask { get; set; }
        public LayerType Type { get; set; }

        public enum LayerType
        {
            Border, Overlay
        }
    }
}
