using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stamper.DataAccess
{
    public class Filenamer
    {
        /// <summary>
        /// Given the path to a file, this method provides functionality for changing the
        /// given path to ensure that it is unique and will not cause any files to be
        /// overwritten, by appending a number to the filename.
        /// </summary>
        /// <example>The input-string "folder\\file.png" will turn into "folder\\file (1).png"
        /// if 'file.png' already exists in 'folder'. </example>
        /// <param name="path">The path for which to determine a unique path</param>
        /// <returns>A path that doens't point to an existing file.</returns>
        public static string UniquePath(string path)
        {
            if (path == null) return null;

            var result = path;

            int i = 1;
            while (File.Exists(result))
            {
                string fileNameWithPathWithoutExtension;
                var dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    fileNameWithPathWithoutExtension = Path.Combine(Path.GetDirectoryName(path),
                        Path.GetFileNameWithoutExtension(path));
                }
                else
                {
                    fileNameWithPathWithoutExtension = Path.GetFileNameWithoutExtension(path);
                }

                result = $"{fileNameWithPathWithoutExtension} ({i}){Path.GetExtension(path)}";
                i++;
            }

            return result;
        }

        /// <summary>
        /// Given the directory, filename and extension of a file, this method returns a filename
        /// that is unique by appending a number to the filename.
        /// See <see cref="UniquePath"/>
        /// </summary>
        /// <returns>A unique filename for the given directory.</returns>
        public static string UniqueFilename(string directory, string filename, string extension)
        {
            var ext = extension.StartsWith(".") ? extension : "." + extension;
            var path = Path.Combine(directory, filename + ext);
            var uniquepath = UniquePath(path);
            var uniquename = Path.GetFileNameWithoutExtension(uniquepath);
            return uniquename;
        }
    }
}
