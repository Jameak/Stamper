using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stamper.UI.Filters
{
    /// <summary>
    /// Most of the filters use the blending-formula specified in the SVG compositing spec (https://dev.w3.org/SVG/modules/compositing/master/)
    /// 
    /// Translation of the spec variables:
    /// Da = Alpha of the foreground color
    /// Sa = Alpha of the background color
    /// Dca = Foreground color, premultiplied by its alpha
    /// Sca = Background color, premultiplied by its alpha
    /// Da' = Alpha of the final color
    /// Dca' = Blended color, before being de-multiplifed by the final alpha
    /// 
    /// One modification that have been made in regards to the application of the final alpha value, is that if
    /// the pixel was completely transparent initially, the output pixel will still be completely transparent.
    /// </summary>
    public static class FilterMethods
    {
        /// <summary>
        /// List of blends that should be applied to the underlying image even if the tint-colour is transparent.
        /// </summary>
        public static ICollection<BlendFilterDelegate> IgnoresBlendColor => new List<BlendFilterDelegate>
        {
            Sepia,
            Grayscale
        };

        /// <summary>
        /// List of filters that should be shown in the dropdown-menu for overlay tint-selections.
        /// The order of this list desides the order of the filters in the dropdown-menu.
        /// The first item will be the default option.
        /// </summary>
        public static ICollection<TintFilter> TintFilters => new List<TintFilter>
        {
            new TintFilter {Name = Filters.Normal, Method = Normal},
            new TintFilter {Name = Filters.Multiply, Method = Multiply},
            new TintFilter {Name = Filters.Screen, Method = Screen},
            new TintFilter {Name = Filters.Add, Method = Add},
            new TintFilter {Name = Filters.Difference, Method = Difference},
            new TintFilter {Name = Filters.Subtract, Method = Subtract},
            new TintFilter {Name = Filters.Darken, Method = Darken},
            new TintFilter {Name = Filters.Lighten, Method = Lighten},
            new TintFilter {Name = Filters.Overlay, Method = Overlay},
            new TintFilter {Name = Filters.None, Method = None}
        };

        /// <summary>
        /// List of filters that should be shown in the dropdown-menu for special filters.
        /// The order of this list desides the order of the filters in the dropdown-menu.
        /// The first item will be the default option.
        /// </summary>
        public static ICollection<SpecialFilter> SpecialFilters => new List<SpecialFilter>
        {
            new SpecialFilter {Name = Filters.None, Method = None},
            new SpecialFilter {Name = Filters.Sepia, Method = Sepia},
            new SpecialFilter {Name = Filters.Grayscale, Method = Grayscale},
            new SpecialFilter {Name = Filters.Overlay, Method = Overlay},
            new SpecialFilter {Name = Filters.Multiply, Method = Multiply},
            new SpecialFilter {Name = Filters.Screen, Method = Screen},
            new SpecialFilter {Name = Filters.Add, Method = Add},
            new SpecialFilter {Name = Filters.Difference, Method = Difference},
            new SpecialFilter {Name = Filters.Subtract, Method = Subtract},
            new SpecialFilter {Name = Filters.Darken, Method = Darken},
            new SpecialFilter {Name = Filters.Lighten, Method = Lighten}
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
        public delegate Tuple<int, int, int, int> BlendFilterDelegate(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta);

        private static double Premultiply(double c, double a)
        {
            return c * a;
        }

        private static double UndoPremultiply(double c, double a)
        {
            return c / a;
        }

        private static double ToDoubleRange(int c)
        {
            return c / 255.0;
        }

        private static int ToIntRange(double c)
        {
            int val = (int) (c * 255.0);
            return val < 255 ? (val > 0 ? val : 0) : 255;
        }

        private static double FinalAlpha(double fa, double ba)
        {
            return fa + ba - fa * ba;
        }

        public static Tuple<int, int, int, int> None(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            return new Tuple<int, int, int, int>(or, og, ob, oa);
        }

        public static Tuple<int, int, int, int> Normal(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            return new Tuple<int, int, int, int>(tr, tg, tb, oa);
        }

        public static Tuple<int, int, int, int> Overlay(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            //Implementation from the SVG spec - Gives a very wrong result and I'm not sure why
            //double oa_d = ToDoubleRange(oa);
            //double ta_d = ToDoubleRange(ta);
            //double or_d = Premultiply(ToDoubleRange(or), oa_d);
            //double og_d = Premultiply(ToDoubleRange(og), oa_d);
            //double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            //double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            //double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            //double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            //double R;
            //double G;
            //double B;
            //if (tr_d * 2 <= ta_d)
            //{
            //    R = or_d * tr_d * 2 + or_d * (1.0 - ta_d) + tr_d * (1.0 - oa_d);
            //    G = og_d * tg_d * 2 + og_d * (1.0 - ta_d) + tg_d * (1.0 - oa_d);
            //    B = ob_d * tb_d * 2 + ob_d * (1.0 - ta_d) + tb_d * (1.0 - oa_d);
            //}
            //else
            //{
            //    R = or_d * (1.0 - ta_d) + tr_d * (1.0 - oa_d) - 2 * (ta_d - tr_d) * (oa_d - or_d) + oa_d * ta_d;
            //    G = og_d * (1.0 - ta_d) + tg_d * (1.0 - oa_d) - 2 * (ta_d - tg_d) * (oa_d - og_d) + oa_d * ta_d;
            //    B = ob_d * (1.0 - ta_d) + tb_d * (1.0 - oa_d) - 2 * (ta_d - tb_d) * (oa_d - ob_d) + oa_d * ta_d;
            //}

            //var fa = FinalAlpha(oa_d, ta_d);
            //R = UndoPremultiply(R, fa);
            //G = UndoPremultiply(G, fa);
            //B = UndoPremultiply(B, fa);

            //return oa == 0
            //    ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
            //    : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));

            
            //Implementation from the Gimp layer modes page: https://docs.gimp.org/en/gimp-concepts-layer-modes.html
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d * (or_d + (2 * tr_d) / 1.0 * (1.0 - or_d));
            double G = og_d * (og_d + (2 * tg_d) / 1.0 * (1.0 - og_d));
            double B = ob_d * (ob_d + (2 * tb_d) / 1.0 * (1.0 - ob_d));

            R = UndoPremultiply(R, oa_d);
            G = UndoPremultiply(G, oa_d);
            B = UndoPremultiply(B, oa_d);

            return new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa);
        }

        public static Tuple<int, int, int, int> Multiply(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d * tr_d + or_d * (1.0 - ta_d) + tr_d * (1.0 - oa_d);
            double G = og_d * tg_d + og_d * (1.0 - ta_d) + tg_d * (1.0 - oa_d);
            double B = ob_d * tb_d + ob_d * (1.0 - ta_d) + tb_d * (1.0 - oa_d);
            
            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Darken(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = Math.Min(or_d * ta_d, tr_d * oa_d) + or_d * (1.0 - ta_d) + tr_d * (1.0 - oa_d);
            double G = Math.Min(og_d * ta_d, tg_d * oa_d) + og_d * (1.0 - ta_d) + tg_d * (1.0 - oa_d);
            double B = Math.Min(ob_d * ta_d, tb_d * oa_d) + ob_d * (1.0 - ta_d) + tb_d * (1.0 - oa_d);

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Lighten(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = Math.Max(or_d * ta_d, tr_d * oa_d) + or_d * (1.0 - ta_d) + tr_d * (1.0 - oa_d);
            double G = Math.Max(og_d * ta_d, tg_d * oa_d) + og_d * (1.0 - ta_d) + tg_d * (1.0 - oa_d);
            double B = Math.Max(ob_d * ta_d, tb_d * oa_d) + ob_d * (1.0 - ta_d) + tb_d * (1.0 - oa_d);

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Difference(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d + tr_d - 2 * Math.Min(or_d * ta_d, tr_d * oa_d);
            double G = og_d + tg_d - 2 * Math.Min(og_d * ta_d, tg_d * oa_d);
            double B = ob_d + tb_d - 2 * Math.Min(ob_d * ta_d, tb_d * oa_d);

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Subtract(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            //Doesn't exist in the SVG spec
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d - tr_d;
            double G = og_d - tg_d;
            double B = ob_d - tb_d;

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Add(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d + tr_d;
            double G = og_d + tg_d;
            double B = ob_d + tb_d;

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        public static Tuple<int, int, int, int> Screen(int or, int og, int ob, int oa, int tr, int tg, int tb, int ta)
        {
            double oa_d = ToDoubleRange(oa);
            double ta_d = ToDoubleRange(ta);
            double or_d = Premultiply(ToDoubleRange(or), oa_d);
            double og_d = Premultiply(ToDoubleRange(og), oa_d);
            double ob_d = Premultiply(ToDoubleRange(ob), oa_d);
            double tr_d = Premultiply(ToDoubleRange(tr), ta_d);
            double tg_d = Premultiply(ToDoubleRange(tg), ta_d);
            double tb_d = Premultiply(ToDoubleRange(tb), ta_d);

            double R = or_d + tr_d - or_d * tr_d;
            double G = og_d + tg_d - og_d * tg_d;
            double B = ob_d + tb_d - ob_d * tb_d;

            var fa = FinalAlpha(oa_d, ta_d);
            R = UndoPremultiply(R, fa);
            G = UndoPremultiply(G, fa);
            B = UndoPremultiply(B, fa);

            return oa == 0
                ? new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), oa)
                : new Tuple<int, int, int, int>(ToIntRange(R), ToIntRange(G), ToIntRange(B), ToIntRange(fa));
        }

        // Doesn't use the given tint-values.
        public static Tuple<int, int, int, int> Sepia(int r, int g, int b, int a, int tr, int tg, int tb, int ta)
        {
            int R = Math.Min(255, (int)((r * 0.393) + (b * 0.189) + (g * 0.769)));
            int G = Math.Min(255, (int)((r * 0.349) + (b * 0.168) + (g * 0.686)));
            int B = Math.Min(255, (int)((r * 0.272) + (b * 0.131) + (g * 0.534)));
            return new Tuple<int, int, int, int>(R, G, B, a);
        }

        // Doesn't use the given tint-values.
        public static Tuple<int, int, int, int> Grayscale(int r, int g, int b, int a, int tr, int tg, int tb, int ta)
        {
            int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
            return new Tuple<int, int, int, int>(gray, gray, gray, a);
        }
    }
}
