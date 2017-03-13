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

            foreach (var layer in files.Select(File.ReadAllText).Select(JsonConvert.DeserializeObject<Layer>))
            {
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
                File.Copy(filepath, filedest);
                layer.File = Path.Combine(CustomDirectory, Path.GetFileName(filedest)); //relative path

                if (!string.IsNullOrEmpty(maskpath))
                {
                    maskdest = Path.Combine(absoluteOutputFolder, Path.GetFileName(maskpath));
                    File.Copy(maskpath, maskdest);
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
                if(filedest != null && File.Exists(filedest)) File.Delete(filedest);
                if(maskdest != null && File.Exists(maskdest)) File.Delete(maskdest);
                if(jsondest != null && File.Exists(jsondest)) File.Delete(jsondest);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a bitmap image from the file located at the specified path.
        /// Files will be loaded in the 32bppArgb pixelformat and stretched to fit
        /// the specified height and width if necessary.
        /// </summary>
        public static Bitmap GetBitmapFromFile(string path, int width, int height)
        {
            try
            {
                if (Path.GetExtension(path) == ".svg")
                {
                    return GetBitmapFromSvg(path, width, height);
                }

                //If given a non-svg file, try to load it in the specified height and width, without caring about preserving aspect ratio
                var file = File.ReadAllBytes(path);
                using (var ms = new MemoryStream(file))
                {
                    var bm = new Bitmap(ms);
                    var bm1 = new Bitmap(bm, width, height);
                    
                    return ConvertBitmapToPixelFormat_32bppArgb(bm1);
                }
            }
            catch (Exception e) when (e is ArgumentException || e is IOException )
            {
                var image = new Bitmap(width, height);
                return ConvertBitmapToPixelFormat_32bppArgb(image);
            }
        }

        private static Bitmap GetBitmapFromSvg(string path, int width, int height)
        {
            using (var text = File.OpenText(path))
            {
                var reader = new SvgReader(text);
                var graphic = reader.Graphic;
                graphic.Size = new NGraphics.Size(width, height);
                var canvas = Platforms.Current.CreateImageCanvas(graphic.Size);
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
    }
}
