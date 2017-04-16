using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;

namespace Stamper.DataAccess
{
    public static class LayerHelper
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
        
        /// <summary>
        /// Creates a new layer by copying the referenced paths into the custom layer directory
        /// and creates a .json file representing the layer at the layer path.
        /// </summary>
        /// <param name="name">The name of the layer. (Not the name of the resulting layer-file)</param>
        /// <param name="filepath">A path to the main image file</param>
        /// <param name="maskpath">A path to the mask file</param>
        /// <param name="type">The layer type</param>
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
