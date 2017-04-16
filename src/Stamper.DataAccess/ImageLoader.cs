using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NGraphics;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace Stamper.DataAccess
{
    public class ImageLoader
    {
        public enum FitMode
        {
            Fill, Stretch
        }

        /// <summary>
        /// Gets a bitmap image from the file located at the specified path.
        /// Files will be loaded in the 32bppArgb pixelformat and stretched to fit
        /// the specified height and width if necessary.
        /// 
        /// If either height or width is null, neither value will be used
        /// and the actual size of the image will be returned.
        /// </summary>
        public static Bitmap LoadBitmapFromFile(string path, int? width = null, int? height = null)
        {
            Bitmap image;
            TryLoadBitmapFromFile(path, out image, width, height);
            return image;
        }

        /// <summary>
        /// <see cref="LoadBitmapFromFile"/>
        /// </summary>
        public static bool TryLoadBitmapFromFile(string path, out Bitmap bitmap, int? width = null, int? height = null)
        {
            try
            {
                if (Path.GetExtension(path) == ".svg")
                {
                    bitmap = width.HasValue && height.HasValue
                        ? GetBitmapFromSvg(path, width.Value, height.Value)
                        : GetBitmapFromSvg(path);
                    return true;
                }

                bitmap = width.HasValue && height.HasValue
                        ? GetBitmapFromFile(path, width.Value, height.Value)
                        : GetBitmapFromFile(path);
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException)
            {
                var image = width.HasValue && height.HasValue
                    ? new Bitmap(width.Value, height.Value)
                    : new Bitmap(100, 100);
                bitmap = ConvertBitmapToPixelFormat_32bppArgb(image);
                return false;
            }
        }

        /// <summary>
        /// Loads an image as a bitmap with the specified width and height.
        /// 
        /// Image will be stretched to the specified dimensions without preserving aspect ratio.
        /// 
        /// If a desired width and height of -1 is specified, the dimensions of the resulting bitmap 
        /// will be the actual width and height of the file, without any kind of scaling or stretching.
        /// </summary>
        /// <param name="path">The path to the image file</param>
        /// <param name="width">The desired width, or -1 for actual width</param>
        /// <param name="height">The desired height, or -1 for actual height</param>
        private static Bitmap GetBitmapFromFile(string path, int width = -1, int height = -1)
        {
            var file = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(file))
            {
                var bm = new Bitmap(ms);
                if (width == -1 || height == -1)
                {
                    return ConvertBitmapToPixelFormat_32bppArgb(bm);
                }

                var bm1 = new Bitmap(bm, width, height);

                return ConvertBitmapToPixelFormat_32bppArgb(bm1);
            }
        }

        /// <summary>
        /// Loads a SVG as a bitmap with the specified width and height.
        /// 
        /// If a desired width and height of -1 is specified, the dimensions of the resulting bitmap 
        /// will be the actual width and height of the file, without any kind of stretching.
        /// The bitmap will be resized if the actual resolution of the file is less than 1000 pixels
        /// in both width and height, but this is done losslessly since SVGs are vector-based.
        /// </summary>
        /// <param name="path">Path to the svg file</param>
        /// <param name="width">The desired width, or -1 for actual width</param>
        /// <param name="height">The desired height, or -1 for actual height</param>
        private static Bitmap GetBitmapFromSvg(string path, int width = -1, int height = -1)
        {
            using (var text = File.OpenText(path))
            {
                var graphic = Graphic.LoadSvg(text);
                NGraphics.Size size;

                if (width == -1 || height == -1)
                {
                    //If no resolution is given, return the actual aspect ratio of the SVG file, but with a minimum resolution of 1000 to give pixel-based operations something to work with.
                    var resolution = graphic.Size.Width < 1000 && graphic.Size.Height < 1000
                        ? FitDimensions(FitMode.Fill, 1000, 1000, (int) graphic.Size.Width, (int) graphic.Size.Height)
                        : new Tuple<int, int>((int) graphic.Size.Width, (int) graphic.Size.Height);
                    size = new NGraphics.Size(resolution.Item1, resolution.Item2);
                }
                else
                {
                    size = new NGraphics.Size(width, height);
                }
                var canvas = Platforms.Current.CreateImageCanvas(size);
                graphic.TransformGeometry(Transform.Scale(size));
                graphic.Size = size;
                graphic.Draw(canvas);
                var image = canvas.GetImage();

                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);

                    var img = new Bitmap(ms);

                    return ConvertBitmapToPixelFormat_32bppArgb(img);
                }
            }
        }

        /// <summary>
        /// Ensures that the given bitmap is using the 32bppArgb PixelFormat format.
        /// 
        /// Bitmap.MakeTransparent _says_ that it does the conversion, but the conversion
        /// done by that method doesn't work properly for all inputs...
        /// </summary>
        public static Bitmap ConvertBitmapToPixelFormat_32bppArgb(Bitmap image)
        {
            if (image.PixelFormat == PixelFormat.Format32bppArgb) return image;

            var new_img = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(new_img))
            {
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
            }
            image.Dispose();
            return new_img;
        }
        
        /// <summary>
        /// Based on the fitmode and desired image dimensions, returns the dimensions
        /// to give the image to adhere to the fitmode.
        /// </summary>
        /// <returns>
        /// A tuple where:
        /// Item 1: Width
        /// Item 2: Height
        /// </returns>
        public static Tuple<int, int> FitDimensions(FitMode mode, int desiredWidth, int desiredHeight, int imageWidth, int imageHeight)
        {
            switch (mode)
            {
                case FitMode.Fill:
                    float widthMult = desiredWidth / (float) imageWidth;
                    float heightMult = desiredHeight / (float) imageHeight;
                    float mult = Math.Min(widthMult, heightMult);
                    var width = (int)Math.Floor(imageWidth * mult);
                    var height = (int)Math.Floor(imageHeight * mult);
                    return new Tuple<int, int>(width, height);
                    break;
                case FitMode.Stretch:
                    return new Tuple<int, int>(desiredWidth, desiredHeight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
