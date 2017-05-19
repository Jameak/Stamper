using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stamper.UI.Filters
{
    public enum Filters
    {
        Normal, Multiply, Screen, Add, Difference, Subtract, Darken, Lighten, None, Sepia, Grayscale, Overlay
    }
    
    public class TintFilter
    {
        public Filters Name { get; set; }
        public FilterMethods.BlendFilterDelegate Method { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class SpecialFilter
    {
        public Filters Name { get; set; }
        public FilterMethods.BlendFilterDelegate Method { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    
}
