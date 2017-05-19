using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stamper.DataAccess;
using Stamper.UI.Filters;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Stamper.UI
{
    public static class BitmapHelper
    {
        /// <summary>
        /// The color in the mask-image that will be removed from images that masks are applied to.
        /// </summary>
        private static Color _maskColor = Color.FromArgb(255, 0, 255, 0);
        
        /// <summary>
        /// <see cref="LayerHelper.ConvertBitmapToPixelFormat_32bppArgb(Bitmap)"/>
        /// </summary>
        public static Bitmap ConvertToPixelFormat_32bppArgb(Bitmap image)
        {
            return ImageLoader.ConvertBitmapToPixelFormat_32bppArgb(image);
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
            

            Parallel.For(0, maskdata.Width, x =>
            {
                for (int y = 0; y < maskdata.Height; y++)
                {
                    var offset = (y * maskdata.Width + x) * maskdepth;
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    var B = maskbuffer[offset + 0];
                    var G = maskbuffer[offset + 1];
                    var R = maskbuffer[offset + 2];
                    var A = maskbuffer[offset + 3];

                    //If the color matches our mask-color, then make it transparent.
                    if (R == _maskColor.R && G == _maskColor.G && B == _maskColor.B && A == _maskColor.A)
                    {
                        imagebuffer[offset + 0] = 0;
                        imagebuffer[offset + 1] = 0;
                        imagebuffer[offset + 2] = 0;
                        imagebuffer[offset + 3] = 0;
                    }
                }
            });

            Marshal.Copy(imagebuffer, 0, imagedata.Scan0, imagebuffer.Length);
            image.UnlockBits(imagedata);
            mask.UnlockBits(maskdata);
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
        public static void AddFilter(Bitmap image, Color tint, FilterMethods.BlendFilterDelegate blendFilter)
        {
            //Dont waste time trying to tint if the given color is completely transparent and the selected filter does blending that is dependent on the selected tint colour.
            if (tint.A == Colors.Transparent.A && tint.R == Colors.Transparent.R && tint.G == Colors.Transparent.G &&
                tint.B == Colors.Transparent.B && !FilterMethods.IgnoresBlendColor.Contains(blendFilter))
            {
                return;
            }

            //Dont waste time if we're not going to do anything anyway.
            if (blendFilter == FilterMethods.None) return;
            

            var rect = new Rectangle(0, 0, image.Width, image.Height);
            
            var imagedata = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);

            Parallel.For(0, imagedata.Width, x =>
            {
                for (int y = 0; y < imagedata.Height; y++)
                {
                    var offset = (y * imagedata.Width + x) * imagedepth;
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    var B = imagebuffer[offset + 0];
                    var G = imagebuffer[offset + 1];
                    var R = imagebuffer[offset + 2];
                    var A = imagebuffer[offset + 3];
                    var result = blendFilter(R, G, B, A, tint.R, tint.G, tint.B, tint.A);

                    imagebuffer[offset + 0] = (byte)result.Item3;
                    imagebuffer[offset + 1] = (byte)result.Item2;
                    imagebuffer[offset + 2] = (byte)result.Item1;
                    imagebuffer[offset + 3] = (byte)result.Item4;
                }
            });

            Marshal.Copy(imagebuffer, 0, imagedata.Scan0, imagebuffer.Length);
            image.UnlockBits(imagedata);
        }
        
        public static async Task<Bitmap> LoadBitmapAsync(string path)
        {
            return ConvertToPixelFormat_32bppArgb(await Task.Factory.StartNew(() =>
            {
                var file = File.ReadAllBytes(path);
                using (var ms = new MemoryStream(file))
                {
                    return new Bitmap(ms);
                }
            }));
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
                    return ConvertToPixelFormat_32bppArgb(new Bitmap(ms));
                }
            }
            catch (TaskCanceledException e)
            {
                throw new WebException("Connection lost", e);
            }
        }

        /// <summary>
        /// Removes empty borders from the given image.
        /// </summary>
        public static Bitmap Autocrop(Bitmap image)
        {
            var rect = new Rectangle(0, 0, image.Width, image.Height);

            var imagedata = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);
            image.UnlockBits(imagedata);

            int minX = -1;
            int minY = -1;
            int maxX = -1;
            int maxY = -1; 

            Parallel.Invoke(() =>
            {
                //Find minX
                for (int x = 0; x < imagedata.Width; x++)
                {
                    for (int y = 0; y < imagedata.Height; y++)
                    {
                        var offset = (y * imagedata.Width + x) * imagedepth;

                        //Check if the pixel is not completely transparent.
                        if (imagebuffer[offset + 3] != 0)
                        {
                            minX = x;

                            //Break out of both loops
                            x = imagedata.Width;
                            y = imagedata.Height;
                        }
                    }
                }
            }, () =>
            {
                //Find minY
                for (int y = 0; y < imagedata.Height; y++)
                {
                    for (int x = 0; x < imagedata.Width; x++)
                    {
                        var offset = (y * imagedata.Width + x) * imagedepth;

                        //Check if the pixel is not completely transparent.
                        if (imagebuffer[offset + 3] != 0)
                        {
                            minY = y;

                            //Break out of both loops
                            x = imagedata.Width;
                            y = imagedata.Height;
                        }
                    }
                }
            }, () =>
            {
                //Find maxX
                for (int x = imagedata.Width - 1; x >= 0; x--)
                {
                    for (int y = imagedata.Height - 1; y >= 0; y--)
                    {
                        var offset = (y * imagedata.Width + x) * imagedepth;

                        //Check if the pixel is not completely transparent.
                        if (imagebuffer[offset + 3] != 0)
                        {
                            maxX = x;

                            //Break out of both loops
                            x = -1;
                            y = -1;
                        }
                    }
                }
            }, () =>
            {
                //Find maxY
                for (int y = imagedata.Height - 1; y >= 0; y--)
                {
                    for (int x = imagedata.Width - 1; x >= 0; x--)
                    {
                        var offset = (y * imagedata.Width + x) * imagedepth;

                        //Check if the pixel is not completely transparent.
                        if (imagebuffer[offset + 3] != 0)
                        {
                            maxY = y;

                            //Break out of both loops
                            x = -1;
                            y = -1;
                        }
                    }
                }
            });
            
            //Completely transparent image or something went wrong. Just return the input.
            if (minX == -1 || minY == -1 || maxX == -1 || maxY == -1)
            {
                return image;
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            
            if (width <= 0 || height <= 0)
            {
                return image;
            }

            var crop = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(crop))
            {
                g.DrawImage(image, -minX, -minY);
            }

            return crop;
        }

        /// <summary>
        /// Generates a mask from the given image.
        /// This is a best-effort attempt that will probably be worse than a well-made custom mask.
        /// 
        /// This works by running a breadth-first search starting from the corners of the image,
        /// setting all pixels in the mask to the specified mask-color, if the pixel is transparent in the image.
        /// 
        /// This ensures that enclosed transparent holes in the given image are preserved in the masked image.
        /// E.g. When generating a mask for a circle, we want keep everything inside the circle, but remove
        /// everything outside of it.
        /// 
        /// Additionally, this BFS implementation eats 1 additional pixel around all transparent colors to help
        /// provide a mask with nice edges around the layered image.
        /// </summary>
        /// <param name="image">The image to generate a mask for.</param>
        /// <returns>The generated mask</returns>
        /// <remarks>Results will differ between big- and little-endian architectures.</remarks>
        public static Bitmap GenerateMask(Bitmap image)
        {
            var mask = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            var queue = new ConcurrentQueue<Coordinate>();
            var set = new ConcurrentDictionary<Coordinate, bool>(); //We only need a set, but C# doesn't have a concurrent implementation


            var rect = new Rectangle(0, 0, image.Width, image.Height);

            var imagedata = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
            var imagedepth = Image.GetPixelFormatSize(imagedata.PixelFormat) / 8;
            var imagebuffer = new byte[imagedata.Width * imagedata.Height * imagedepth];
            Marshal.Copy(imagedata.Scan0, imagebuffer, 0, imagebuffer.Length);

            var maskdata = mask.LockBits(rect, ImageLockMode.WriteOnly, mask.PixelFormat);
            var maskdepth = Image.GetPixelFormatSize(maskdata.PixelFormat) / 8;
            var maskbuffer = new byte[maskdata.Width * maskdata.Height * maskdepth];
            

            var corners = new List<Coordinate>
            {
                new Coordinate(0, 0),                             // Top left
                new Coordinate(0, image.Height - 1),              // Bottom left
                new Coordinate(image.Width - 1, 0),               // Top right
                new Coordinate(image.Width - 1, image.Height - 1) // Bottom right
            };

            //Enqueue our starting positions.
            foreach (var corner in corners)
            {
                var offset = (corner.Y * imagedata.Width + corner.X) * imagedepth;

                if (imagebuffer[offset + 3] == Color.Transparent.A) //If the color is transparent, queue the pixel. Also dependent on endian-ness
                {
                    //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                    maskbuffer[offset + 0] = _maskColor.B;
                    maskbuffer[offset + 1] = _maskColor.G;
                    maskbuffer[offset + 2] = _maskColor.R;
                    maskbuffer[offset + 3] = _maskColor.A;
                    if (set.TryAdd(corner, true))
                    {
                        queue.Enqueue(corner);
                    }
                }
            }
            
            //We want to invoke the action the same number of times as logical processors on the system.
            var threadStatus = new bool[Environment.ProcessorCount];
            var actions = new Action[Environment.ProcessorCount];
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var i1 = i; // Copy to local variable to avoid a modified closure in the lambda
                actions[i] = () => GenerateMaskParallel(imagebuffer, maskbuffer, imagedepth, imagedata.Width, imagedata.Height, queue,
                        set, threadStatus, i1);
            }

            Parallel.Invoke(actions);
            
            Marshal.Copy(maskbuffer, 0, maskdata.Scan0, maskbuffer.Length);
            mask.UnlockBits(maskdata);
            image.UnlockBits(imagedata);

            return mask;
        }

        private static void GenerateMaskParallel(byte[] imagebuffer, byte[] maskbuffer, int imagedepth, int width, int height,
            ConcurrentQueue<Coordinate> queue, ConcurrentDictionary<Coordinate, bool> set, bool[] threadIdleStatus, int ownIndex)
        {
            while (!queue.IsEmpty || !threadIdleStatus.All(i => i))
            {
                Coordinate coord;
                if (queue.TryDequeue(out coord))
                {
                    threadIdleStatus[ownIndex] = false;
                    foreach (var coordinate in coord.SurroundingCoords(width, height).Where(c => !set.ContainsKey(c)))
                    {
                        if (!set.TryAdd(coordinate, true)) continue;

                        var offset = (coordinate.Y * width + coordinate.X) * imagedepth;

                        //The 32bppArgb format lies to us in regards to the order of channels because of endian-ness. On little-endian architecture the byte-order is BGRA instead of ARGB...
                        maskbuffer[offset + 0] = _maskColor.B;
                        maskbuffer[offset + 1] = _maskColor.G;
                        maskbuffer[offset + 2] = _maskColor.R;
                        maskbuffer[offset + 3] = _maskColor.A;

                        if (imagebuffer[offset + 3] == Color.Transparent.A) //If the color is transparent, queue the pixel. Also dependent on endian-ness
                        {
                            queue.Enqueue(coordinate);
                        }
                    }
                }
                else
                {
                    threadIdleStatus[ownIndex] = true;
                }
            }
        }

        private struct Coordinate
        {
            public readonly int X;
            public readonly int Y;

            public Coordinate(int x, int y)
            {
                X = x;
                Y = y;
            }

            public IEnumerable<Coordinate> SurroundingCoords(int width, int height)
            {
                return new[]
                {
                    new Coordinate(X - 1, Y),
                    new Coordinate(X - 1, Y - 1),
                    new Coordinate(X - 1, Y + 1),
                    new Coordinate(X + 1, Y),
                    new Coordinate(X + 1, Y - 1),
                    new Coordinate(X + 1, Y + 1),
                    new Coordinate(X, Y + 1),
                    new Coordinate(X, Y - 1)
                }.Where(coordinate => coordinate.IsValidLocation(width, height));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsValidLocation(int imageWidth, int imageHeight)
            {
                return X >= 0 && Y >= 0 && X < imageWidth && Y < imageHeight;
            }

            // Needed because coords are stored in a ConcurrentDictionary.
            public override bool Equals(object obj)
            {
                if (!(obj is Coordinate)) return false;

                var coord = (Coordinate) obj;
                return coord.X == X && coord.Y == Y;
            }

            // Needed because coords are stored in a ConcurrentDictionary.
            public override int GetHashCode()
            {
                return X * 100000 + Y;
            }
        }
    }
}
