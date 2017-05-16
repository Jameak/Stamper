using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stamper.DataAccess
{
    public class Imgur
    {
        private static HttpClient Client { get; set; }

        static Imgur()
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Client-ID {SettingsManager.ImgurClientID}");
        }
        
        /// <summary>
        /// Converts the given image to png and uploads it to Imgur.
        /// </summary>
        /// <returns>A link to the uploaded image if successful, and null on failure.</returns>
        public static async Task<string> UploadImage(Bitmap image)
        {
            var ratelimits = await GetUploadRatelimits();
            //Abort if we cant upload any more images from this IP. Abort slightly early for client-wide IPs
            //  to make sure that multiple concurrent uploads wont accidentally exceed the limit.
            if (ratelimits.Item1 < 1 || ratelimits.Item2 < 10) return null;

            byte[] img;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                img = ms.ToArray();
            }

            var form = new MultipartFormDataContent
            {
                {new StringContent("base64"), "type"},
                {new StringContent("Stamper - Token"), "title"},
                {new StringContent("Made with https://github.com/Jameak/Stamper"), "description"},
                {new StringContent(Convert.ToBase64String(img)), "image"}
            };
            
            var result = await Client.PostAsync("https://api.imgur.com/3/image", form);

            if (result.IsSuccessStatusCode)
            {
                var val = await result.Content.ReadAsStringAsync();

                //Extract the ID of the created image from the response
                var regex = new Regex("\"id\"\\s*:\\s*\"(?<image>[a-zA-Z0-9]+)\"");
                var match = regex.Match(val);
                if (match.Success)
                {
                    var id = match.Groups["image"].Value;
                    return $"http://imgur.com/{id}";
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the ratelimits that Imgur-uploads are subjected to. Since one
        /// POST-request uses 10 credits, the returned values are pre-divided by 10.
        /// </summary>
        /// <returns>
        /// A Tuple where item 1 is the number of uploads that the user may perform
        /// and where item 2 is the number of uploads left for all users of the program.
        /// </returns>
        public static async Task<Tuple<int,int>> GetUploadRatelimits()
        {
            HttpResponseMessage result;
            try
            {
                result = await Client.GetAsync("https://api.imgur.com/3/credits");
            }
            catch (HttpRequestException)
            {
                return new Tuple<int, int>(-1, -1);
            }
            

            if (result == null) return null;

            if (result.IsSuccessStatusCode)
            {
                var val = await result.Content.ReadAsStringAsync();
                var ratelimit = JsonConvert.DeserializeObject<ApiHelper>(val);
                
                return new Tuple<int, int>(ratelimit.Data.UserRemaining / 10, ratelimit.Data.ClientRemaining / 10);
            }

            return new Tuple<int, int>(-1,-1);
        }

        private class ImgurRatelimitResponse
        {
            public int UserRemaining { get; set; }
            public int ClientRemaining { get; set; }
        }

        private class ApiHelper
        {
            public ImgurRatelimitResponse Data { get; set; }
        }
    }
}
