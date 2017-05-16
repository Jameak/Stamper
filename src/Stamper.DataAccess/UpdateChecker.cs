using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stamper.DataAccess
{
    public class UpdateChecker
    {
        private static HttpClient Client { get; set; }

        static UpdateChecker()
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Add("User-Agent", SettingsManager.GithubUserAgent);
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        /// <summary>
        /// Checks if a new non-draft, non-prerelease version has been released on GitHub.
        /// Returns false if the user has chosen to ignore updates, unless forceCheck is true.
        /// </summary>
        /// <returns>
        /// A Tuple where:
        /// Item 1: bool indicating whether an update is available.
        /// Item 2: The version number of the updated release. Empty string or null if no update is available.
        /// </returns>
        public static async Task<Tuple<bool, string>> CheckForUpdate(bool forceCheck)
        {
            if (SettingsManager.IgnoreUpdates && !forceCheck) return new Tuple<bool, string>(false, string.Empty);

            HttpResponseMessage result;
            try
            {
                result = await Client.GetAsync("https://api.github.com/repos/jameak/stamper/releases");
            }
            catch (HttpRequestException)
            {
                return new Tuple<bool, string>(false, string.Empty);
            }

            if (result != null && result.IsSuccessStatusCode)
            {
                var val = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<List<GithubRelease>>(val);
                
                var currentVersion = ParseVersion(SettingsManager.Version);
                foreach (var githubRelease in response)
                {
                    if (!githubRelease.Draft && !githubRelease.Prerelease)
                    {
                        var releaseVersion = ParseVersion(githubRelease.Tag_name);
                        
                        if (currentVersion.Item1 < releaseVersion.Item1)
                        {
                            return new Tuple<bool, string>(true, githubRelease.Tag_name);
                        }

                        if (currentVersion.Item1 == releaseVersion.Item1 && currentVersion.Item2 < releaseVersion.Item2)
                        {
                            return new Tuple<bool, string>(true, githubRelease.Tag_name);
                        }
                            
                        if (currentVersion.Item1 == releaseVersion.Item1 && currentVersion.Item2 == releaseVersion.Item2 &&
                            currentVersion.Item3 < releaseVersion.Item3)
                        {
                            return new Tuple<bool, string>(true, githubRelease.Tag_name);
                        }
                    }
                }
            }

            return new Tuple<bool, string>(false, string.Empty);
        }

        private static Tuple<int, int, int> ParseVersion(string version)
        {
            var cleanVersion = version.StartsWith("v") ? version.Substring(1) : version;
            var labels = cleanVersion.Split('.');

            int major;
            int minor;
            int patch;
            int.TryParse(labels[0], out major);
            int.TryParse(labels[1], out minor);

            //Patch may contain extra non-int info. We dont want to notify about new pre-releases, so failing on those are fine.
            // This will also fail on build-metadata, but I wont be using that so that doesn't matter.
            int.TryParse(labels[2], out patch);
            
            return new Tuple<int, int, int>(major, minor, patch);
        }
        
        private class GithubRelease
        {
            public string Tag_name { get; set; }
            public bool Draft { get; set; }
            public bool Prerelease { get; set; }
        }
    }
}
