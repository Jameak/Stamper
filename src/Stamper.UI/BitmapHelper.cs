using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stamper.DataAccess;
using Stamper.UI.Filters;
using Color = System.Drawing.Color;

namespace Stamper.UI
{
    public static class BitmapHelper
    {
        /// <summary>
        /// <see cref="LayerSource.ConvertBitmapToPixelFormat_32bppArgb(Bitmap)"/>
        /// </summary>
        public static Bitmap ConvertToPixelFormat_32bppArgb(Bitmap image)
        {
            return LayerSource.ConvertBitmapToPixelFormat_32bppArgb(image);
        }

        /// <summary>
        /// Returns a BitmapImage made from the given Bitmap that can be shown in a WPF Image UI element.
        /// </summary>
        public static BitmapImage ConvertBitmapToImageSource(Bitmap image)
        {
            using (var ms = new MemoryStream())
            {
                //To avoid "ExternalException" from System.Drawing with message "A Generic Error occured at GDI+" we need to create a new temporary bitmap here.
                //  Caused by the original stream that was used to create the original bitmap having been disposed.
                //  From MSDN Bitmap page: "You must keep the stream open for the lifetime of the Bitmap."
                //  I say fuck keeping track of the source stream, so let's work around that limitation like this instead.
                var img = new Bitmap(image);

                img.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                var output = new BitmapImage();
                output.BeginInit();
                output.CacheOption = BitmapCacheOption.OnLoad;
                output.StreamSource = ms;
                output.EndInit();
                return output;
            }
        }

        /// <summary>
        /// Applies the given mask to the given image.
        /// 
        /// Pixels in the mask with the color #FF00FF00 (#AARRGGBB) will be removed from the image.
        /// All other pixels will be ignored.
        /// 
        /// Image and mask must have identical sizes and use the 32bppArgb pixel format.
        /// </summary>
        public static void ApplyMaskToImage(Bitmap image, Bitmap mask)
        {
            var rect = new Rectangle(0,0,image.Width, image.Height);

            var imagedata = image.LockBits(rect, ImageLockMode.WriteOnly, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);
            
            var maskdata = mask.LockBits(rect, ImageLockMode.ReadOnly, mask.PixelFormat);
            var maskdepth = Image.GetPixelFormatSize(maskdata.PixelFormat) / 8;
            var maskbuffer = new byte[maskdata.Width * maskdata.Height * maskdepth];
            Marshal.Copy(maskdata.Scan0, maskbuffer, 0, maskbuffer.Length);
            
            Parallel.Invoke(
                () =>
                {
                    ParallelApplyMaskToImage(maskbuffer, imagebuffer, 0, 0, maskdata.Width / 4, maskdata.Height, maskdata.Width, maskdepth);
                }, () =>
                {
                    ParallelApplyMaskToImage(maskbuffer, imagebuffer, maskdata.Width / 4, 0, maskdata.Width / 2, maskdata.Height, maskdata.Width, maskdepth);
                }, () =>
                {
                    ParallelApplyMaskToImage(maskbuffer, imagebuffer, maskdata.Width / 2, 0, maskdata.Width / 4 * 3, maskdata.Height, maskdata.Width, maskdepth);
                }, () =>
                {
                    ParallelApplyMaskToImage(maskbuffer, imagebuffer, maskdata.Width / 4 * 3, 0, maskdata.Width, maskdata.Height, maskdata.Width, maskdepth);
                });
            

            Marshal.Copy(imagebuffer, 0, imagedata.Scan0, imagebuffer.Length);
            image.UnlockBits(imagedata);
            mask.UnlockBits(maskdata);

            //The above code is equal to this, but much much faster.
            //for (var x = 0; x < mask.Width; x++)
            //{
            //    for (var y = 0; y < mask.Height; y++)
            //    {
            //        var pixel = mask.GetPixel(x, y);
            //        if (pixel.R == 0 && pixel.G == 255 && pixel.B == 0 && pixel.A == 255) //If the pixel is #00FF00, we should make the pixel in the output transparent.
            //        {
            //            image.SetPixel(x, y, Color.Transparent);
            //        }
            //    }
            //}
        }

        private static void ParallelApplyMaskToImage(byte[] maskbuffer, byte[] imagebuffer, int startx, int starty, int endx, int endy, int width, int depth)
        {
            for (int x = startx; x < endx; x++)
            {
                for (int y = starty; y < endy; y++)
                {
                    var offset = (y * width + x) * depth;
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    var B = maskbuffer[offset + 0];
                    var G = maskbuffer[offset + 1];
                    var R = maskbuffer[offset + 2];
                    var A = maskbuffer[offset + 3];

                    if (R == 0 && G == 255 && B == 0 && A == 255) //If the pixel is #00FF00, we should make the pixel in the output transparent.
                    {
                        imagebuffer[offset + 0] = 0;
                        imagebuffer[offset + 1] = 0;
                        imagebuffer[offset + 2] = 0;
                        imagebuffer[offset + 3] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Draws the given layer on top of the given image.
        /// </summary>
        public static void AddLayerToImage(Bitmap image, Bitmap layer)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.DrawImage(layer, 0, 0);
            }
        }

        /// <summary>
        /// Applies the given tint to the given image, using the specified filter.
        /// 
        /// Image must use the 32bppArgb pixel format.
        /// </summary>
        /// <remarks>Results will differ between big- and little-endian architectures.</remarks>
        public static void AddFilter(Bitmap image, Color tint, FilterMethods.TintFilterDelegate tintFilter)
        {
            //Dont waste time trying to tint if the given color is completely transparent
            if (tint.A == Colors.Transparent.A && tint.R == Colors.Transparent.R && tint.G == Colors.Transparent.G &&
                tint.B == Colors.Transparent.B)
            {
                return;
            }

            //Dont waste time if we're not going to do anything anyway.
            if (tintFilter == FilterMethods.None) return;


            var rect = new Rectangle(0, 0, image.Width, image.Height);

            var imagedata = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);

            Parallel.Invoke(
                () =>
                {
                    ParallelAddFilter(imagebuffer, 0, 0, imagedata.Width / 4, imagedata.Height, imagedata.Width, imagedepth, tintFilter, tint);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 4, 0, imagedata.Width / 2, imagedata.Height, imagedata.Width, imagedepth, tintFilter, tint);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 2, 0, imagedata.Width / 4 * 3, imagedata.Height, imagedata.Width, imagedepth, tintFilter, tint);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 4 * 3, 0, imagedata.Width, imagedata.Height, imagedata.Width, imagedepth, tintFilter, tint);
                });

            Marshal.Copy(imagebuffer, 0, imagedata.Scan0, imagebuffer.Length);
            image.UnlockBits(imagedata);
        }

        private static void ParallelAddFilter(byte[] imagebuffer, int startx, int starty, int endx, int endy, int width, int depth, FilterMethods.TintFilterDelegate tintFilter, Color tint)
        {
            for (int x = startx; x < endx; x++)
            {
                for (int y = starty; y < endy; y++)
                {
                    var offset = (y * width + x) * depth;
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    var B = imagebuffer[offset + 0];
                    var G = imagebuffer[offset + 1];
                    var R = imagebuffer[offset + 2];
                    var A = imagebuffer[offset + 3];
                    var result = tintFilter(R, G, B, A, tint.R, tint.B, tint.G, tint.A);

                    imagebuffer[offset + 0] = (byte)result.Item3;
                    imagebuffer[offset + 1] = (byte)result.Item2;
                    imagebuffer[offset + 2] = (byte)result.Item1;
                    imagebuffer[offset + 3] = (byte)result.Item4;
                }
            }
        }

        /// <summary>
        /// Applies the specified filter to the image.
        /// 
        /// Image must use the 32bppArgb pixel format.
        /// </summary>
        /// <remarks>Results will differ between big- and little-endian architectures.</remarks>
        public static void AddFilter(Bitmap image, FilterMethods.SpecialFilterDelegate specialFilter)
        {
            //Dont waste time if we're not going to do anything anyway.
            if (specialFilter == FilterMethods.None) return;

            var rect = new Rectangle(0, 0, image.Width, image.Height);

            var imagedata = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);

            Parallel.Invoke(
                () =>
                {
                    ParallelAddFilter(imagebuffer, 0, 0, imagedata.Width / 4, imagedata.Height, imagedata.Width, imagedepth, specialFilter);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 4, 0, imagedata.Width / 2, imagedata.Height, imagedata.Width, imagedepth, specialFilter);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 2, 0, imagedata.Width / 4 * 3, imagedata.Height, imagedata.Width, imagedepth, specialFilter);
                }, () =>
                {
                    ParallelAddFilter(imagebuffer, imagedata.Width / 4 * 3, 0, imagedata.Width, imagedata.Height, imagedata.Width, imagedepth, specialFilter);
                });
            
            Marshal.Copy(imagebuffer, 0, imagedata.Scan0, imagebuffer.Length);
            image.UnlockBits(imagedata);
        }

        private static void ParallelAddFilter(byte[] imagebuffer, int startx, int starty, int endx, int endy, int width, int depth, FilterMethods.SpecialFilterDelegate specialFilter)
        {
            for (int x = startx; x < endx; x++)
            {
                for (int y = starty; y < endy; y++)
                {
                    var offset = (y * width + x) * depth;
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    var B = imagebuffer[offset + 0];
                    var G = imagebuffer[offset + 1];
                    var R = imagebuffer[offset + 2];
                    var A = imagebuffer[offset + 3];
                    var result = specialFilter(R, G, B, A);

                    imagebuffer[offset + 0] = (byte)result.Item3;
                    imagebuffer[offset + 1] = (byte)result.Item2;
                    imagebuffer[offset + 2] = (byte)result.Item1;
                    imagebuffer[offset + 3] = (byte)result.Item4;
                }
            }
        }

        /// <summary>
        /// Download an image from the given url. Will abort if download takes more than 60 seconds.
        /// </summary>
        public static async Task<Bitmap> GetWebContent(string url)
        {
            var DOWNLOAD_TIMEOUT_SECONDS = 60;

            if (url == null) return null;
            var tokensource = new CancellationTokenSource();
            tokensource.CancelAfter(TimeSpan.FromSeconds(DOWNLOAD_TIMEOUT_SECONDS));

            try
            {
                var request = WebRequest.CreateHttp(url);
                tokensource.Token.Register(request.Abort);
                using (var stream = (await request.GetResponseAsync()).GetResponseStream())
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms, 81920, tokensource.Token);
                    ms.Position = 1;
                    return new Bitmap(ms);
                }
            }
            catch (TaskCanceledException e)
            {
                throw new WebException("Connection lost", e);
            }
        } 
    }
}
