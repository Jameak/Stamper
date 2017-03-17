using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NGraphics;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace Stamper.DataAccess
{
    public static class LayerSource
    {
        private const string LayerDirectory = "layers";
        private const string CustomDirectory = "custom";

        /// <summary>
        /// Returns all the layers in the "layers" directory located in the current working directory.
        /// Only adds layers located directly in the "layers" directory, and not in any subdirectories.
        /// </summary>
        public static IEnumerable<Layer> GetLayers()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), LayerDirectory);
            var files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            var list = new List<Layer>();

            foreach (var jsonFile in files)
            {
                var layer = JsonConvert.DeserializeObject<Layer>(File.ReadAllText(jsonFile));
                layer.JsonFileName = jsonFile;

                //Convert relative paths from json into absolute paths.
                layer.File = Path.Combine(path, layer.File);
                if (!string.IsNullOrWhiteSpace(layer.Mask))
                {
                    layer.Mask = Path.Combine(path, layer.Mask);
                }

                list.Add(layer);
            }
            return list;
        }
        
        public static bool CreateNewLayer(string name, string filepath, string maskpath, Layer.LayerType type)
        {
            var layer = new Layer {File = filepath, Mask = maskpath, Name = name, Type = type};

            var absoluteOutputFolder = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), LayerDirectory),
                CustomDirectory);
            if (!Directory.Exists(absoluteOutputFolder)) Directory.CreateDirectory(absoluteOutputFolder);
            
            string filedest = null;
            string maskdest = null;
            string jsondest = null;
            try
            {
                filedest = Path.Combine(absoluteOutputFolder, Path.GetFileName(filepath));
                if (!File.Exists(filedest)) File.Copy(filepath, filedest);

                layer.File = Path.Combine(CustomDirectory, Path.GetFileName(filedest)); //relative path

                if (!string.IsNullOrEmpty(maskpath))
                {
                    maskdest = Path.Combine(absoluteOutputFolder, Path.GetFileName(maskpath));
                    if (!File.Exists(maskdest)) File.Copy(maskpath, maskdest);
                    layer.Mask = Path.Combine(CustomDirectory, Path.GetFileName(maskdest)); //relative path
                }
                else
                {
                    layer.Mask = string.Empty;
                }

                jsondest = Path.Combine(Directory.GetCurrentDirectory(), LayerDirectory);
                jsondest = Path.Combine(jsondest, $"{Guid.NewGuid()}.json");
                File.WriteAllText(jsondest, JsonConvert.SerializeObject(layer));
            }
            catch (Exception e) when (e is ArgumentException || e is IOException)
            {
                if (filedest != null && File.Exists(filedest)) File.Delete(filedest);
                if (maskdest != null && File.Exists(maskdest)) File.Delete(maskdest);
                if (jsondest != null && File.Exists(jsondest)) File.Delete(jsondest);
                return false;
            }

            return true;
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
            try
            {
                if (Path.GetExtension(path) == ".svg")
                {
                    return width.HasValue && height.HasValue
                        ? GetBitmapFromSvg(path, width.Value, height.Value)
                        : GetBitmapFromSvg(path);
                }

                return width.HasValue && height.HasValue
                        ? GetBitmapFromFile(path, width.Value, height.Value)
                        : GetBitmapFromFile(path);
            }
            catch (Exception e) when (e is ArgumentException || e is IOException )
            {

                var image = width.HasValue && height.HasValue
                    ? new Bitmap(width.Value, height.Value)
                    : new Bitmap(100, 100);
                return ConvertBitmapToPixelFormat_32bppArgb(image);
            }
        }

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
            //If given a non-svg file, try to load it in the specified height and width, without caring about preserving aspect ratio
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
        /// </summary>
        /// <param name="path">Path to the svg file</param>
        /// <param name="width">The desired width</param>
        /// <param name="height">The desired height</param>
        private static Bitmap GetBitmapFromSvg(string path, int width = 1000, int height = 1000)
        {
            using (var text = File.OpenText(path))
            {
                var size = new NGraphics.Size(width, height);
                var graphic = Graphic.LoadSvg(text);
                var canvas = Platforms.Current.CreateImageCanvas(size);

                graphic.TransformGeometry(Transform.Scale(size));
                graphic.Size = size;
                graphic.Draw(canvas);

                using (var ms = new MemoryStream())
                {
                    canvas.GetImage().SaveAsPng(ms);

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
        /// Saves the given bitmap in the custom-files folder with a unique name.
        /// </summary>
        /// <returns>The path to the saved file.</returns>
        public static string SaveCustomMask(Bitmap mask)
        {
            var absoluteOutputFolder = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), LayerDirectory),
                CustomDirectory);
            if (!Directory.Exists(absoluteOutputFolder)) Directory.CreateDirectory(absoluteOutputFolder);

            var outputFile = Path.Combine(absoluteOutputFolder, $"{Guid.NewGuid()}.png");

            mask.Save(outputFile, ImageFormat.Png);

            return outputFile;
        }

        /// <summary>
        /// Deletes the json-file that the layer is stored in.
        /// Does NOT delete the files that the layer references, since multiple layers might refer to the same files.
        /// </summary>
        public static void DeleteLayer(Layer layer)
        {
            try
            {
                File.Delete(layer.JsonFileName);
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                //NOOP
            }
        }
    }
}
