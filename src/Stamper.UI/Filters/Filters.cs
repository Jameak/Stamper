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
        Normal, Multiply, Screen, Divide, Add, Difference, Subtract, Darken, Lighten, None, Sepia, Grayscale, Overlay
    }

    public class TintFilter
    {
        public Filters Name { get; set; }
        public FilterMethods.TintFilterDelegate Method { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class SpecialFilter
    {
        public Filters Name { get; set; }
        public FilterMethods.SpecialFilterDelegate Method { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public static class FilterMethods
    {
        public static ICollection<TintFilter> TintFilters => new List<TintFilter>
        {
            new TintFilter {Name = Filters.Normal, Method = Normal},
            new TintFilter {Name = Filters.Multiply, Method = Multiply},
            new TintFilter {Name = Filters.Screen, Method = Screen},
            new TintFilter {Name = Filters.Divide, Method = Divide},
            new TintFilter {Name = Filters.Add, Method = Add},
            new TintFilter {Name = Filters.Difference, Method = Difference},
            new TintFilter {Name = Filters.Subtract, Method = Subtract},
            new TintFilter {Name = Filters.Darken, Method = Darken},
            new TintFilter {Name = Filters.Lighten, Method = Lighten},
            new TintFilter {Name = Filters.Overlay, Method = Overlay},
            new TintFilter {Name = Filters.None, Method = None}
        };

        public static ICollection<SpecialFilter> SpecialFilters => new List<SpecialFilter>
        {
            new SpecialFilter {Name = Filters.None, Method = None},
            new SpecialFilter {Name = Filters.Sepia, Method = Sepia},
            new SpecialFilter {Name = Filters.Grayscale, Method = Grayscale}
        };

        /// <summary>
        /// A delegate used for tinting an image with any color.
        /// </summary>
        /// <param name="or">The red-component of the pixel to tint.</param>
        /// <param name="ob">The blue-component of the pixel to tint.</param>
        /// <param name="og">The green-component of the pixel to tint.</param>
        /// <param name="oa">The alpha-component of the pixel to tint.</param>
        /// <param name="tr">The red-component of the color that will be used for tinting.</param>
        /// <param name="tb">The blue-component of the color that will be used for tinting.</param>
        /// <param name="tg">The green-component of the color that will be used for tinting.</param>
        /// <param name="ta">The alpha-component of the color that will be used for tinting.</param>
        /// <returns>
        /// A tuple where:
        /// Item 1 is the red-component of the resulting color.
        /// Item 2 is the blue-component of the resulting color.
        /// Item 3 is the green-component of the resulting color.
        /// Item 4 is the alpha-component of the resulting color.
        /// </returns>
        public delegate Tuple<int,int,int,int> TintFilterDelegate(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta);

        /// <summary>
        /// A delegate used for changing the colors of an image using a specific pre-determined filter.
        /// </summary>
        /// <param name="r">The red-component of the pixel to tint.</param>
        /// <param name="b">The blue-component of the pixel to tint.</param>
        /// <param name="g">The green-component of the pixel to tint.</param>
        /// <param name="a">The alpha-component of the pixel to tint.</param>
        /// <returns>
        /// A tuple where:
        /// Item 1 is the red-component of the resulting color.
        /// Item 2 is the blue-component of the resulting color.
        /// Item 3 is the green-component of the resulting color.
        /// Item 4 is the alpha-component of the resulting color.
        /// </returns>
        public delegate Tuple<int,int,int,int> SpecialFilterDelegate(int r, int b, int g, int a);
        
        public static Tuple<int, int, int, int> None(int r, int b, int g, int a)
        {
            return new Tuple<int, int, int, int>(r, g, b, a);
        }

        public static Tuple<int, int, int, int> None(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            return new Tuple<int, int, int, int>(or, og, ob, oa);
        }

        public static Tuple<int, int, int, int> Normal(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            return new Tuple<int, int, int, int>(tr, tg, tb, oa);
        }

        public static Tuple<int, int, int, int> Overlay(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            //The alpha compositing "over" blend
            int R = (int) (((tr / 255.0 * ta / 255.0) + (or / 255.0 * (1.0 - ta / 255.0))) * 255);
            int G = (int) (((tg / 255.0 * ta / 255.0) + (og / 255.0 * (1.0 - ta / 255.0))) * 255);
            int B = (int) (((tb / 255.0 * ta / 255.0) + (ob / 255.0 * (1.0 - ta / 255.0))) * 255);
            
            return new Tuple<int, int, int, int>(R, G, B, Math.Max(ta, oa));
        }

        public static Tuple<int, int, int, int> Multiply(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = (int)((or * tr) / 255.0);
            int G = (int)((og * tg) / 255.0);
            int B = (int)((ob * tb) / 255.0);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Darken(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Min(or, tr);
            int G = Math.Min(og, tg);
            int B = Math.Min(ob, tb);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Lighten(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Max(or, tr);
            int G = Math.Max(og, tg);
            int B = Math.Max(ob, tb);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Difference(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Abs(or - tr);
            int G = Math.Abs(og - tg);
            int B = Math.Abs(ob - tb);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Subtract(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Max(0, or - tr);
            int G = Math.Max(0, og - tg);
            int B = Math.Max(0, ob - tb);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Add(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Min(255, or + tr);
            int G = Math.Min(255, og + tg);
            int B = Math.Min(255, ob + tb);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Divide(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Min(255, (int)((256.0 * tr) / (or + 1)));
            int G = Math.Min(255, (int)((256.0 * tg) / (og + 1)));
            int B = Math.Min(255, (int)((256.0 * tb) / (ob + 1)));
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Screen(int or, int ob, int og, int oa, int tr, int tb, int tg, int ta)
        {
            int R = Math.Max(0, (255 - (255 - or) * (255 - tr)) / 255);
            int G = Math.Max(0, (255 - (255 - og) * (255 - tg)) / 255);
            int B = Math.Max(0, (255 - (255 - ob) * (255 - tb)) / 255);
            return new Tuple<int, int, int, int>(R, G, B, oa);
        }

        public static Tuple<int, int, int, int> Sepia(int r, int b, int g, int a)
        {
            int R = Math.Min(255, (int) ((r * 0.393) + (b * 0.189) + (g * 0.769)));
            int G = Math.Min(255, (int) ((r * 0.349) + (b * 0.168) + (g * 0.686)));
            int B = Math.Min(255, (int) ((r * 0.272) + (b * 0.131) + (g * 0.534)));
            return new Tuple<int, int, int, int>(R, G, B, a);
        }

        public static Tuple<int, int, int, int> Grayscale(int r, int b, int g, int a)
        {
            int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
            return new Tuple<int, int, int, int>(gray, gray, gray, a);
        }
    }
}
