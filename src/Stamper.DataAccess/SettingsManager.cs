using System;

namespace Stamper.DataAccess
{
    public static class SettingsManager
    {
        public static string ImgurClientID => Properties.Settings.Default.ImgurClientID;
        public static string Version => Properties.Settings.Default.Version;
        public static string GithubUserAgent => Properties.Settings.Default.GitHubUserAgent;
        public static string DefaultFilename => Properties.Settings.Default.DefaultFilename;

        public static bool IgnoreUpdates
        {
            get { return Properties.Settings.Default.IgnoreUpdates; }
            set
            {
                Properties.Settings.Default.IgnoreUpdates = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string LastSaveDirectory
        {
            get { return Properties.Settings.Default.LastSaveDirectory; }
            set
            {
                Properties.Settings.Default.LastSaveDirectory = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string LastFilename
        {
            get { return Properties.Settings.Default.LastFilename; }
            set
            {
                Properties.Settings.Default.LastFilename = value;
                Properties.Settings.Default.Save();
            }
        }
        
        public static int StartupTokenWidth
        {
            get
            {
                return Properties.Settings.Default.StartupTokenWidth > 0 ? Properties.Settings.Default.StartupTokenWidth : 512;
            }
            set
            {
                if (value > 0)
                {
                    Properties.Settings.Default.StartupTokenWidth = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static int StartupTokenHeight
        {
            get
            {
                return Properties.Settings.Default.StartupTokenHeight > 0 ? Properties.Settings.Default.StartupTokenHeight : 512;
            }
            set
            {
                if (value > 0)
                {
                    Properties.Settings.Default.StartupTokenHeight = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static ImageLoader.FitMode StartupFitmode
        {
            get
            {
                switch (Properties.Settings.Default.StartupFitmode)
                {
                    case 0:
                        return ImageLoader.FitMode.Stretch;
                    case 1:
                        return ImageLoader.FitMode.Fill;
                    default:
#if DEBUG
                        throw new ArgumentOutOfRangeException("StartupFitmode setting", Properties.Settings.Default.StartupFitmode, null);
#else
                        return ImageLoader.FitMode.Stretch;
#endif
                }
            }
            set
            {
                switch (value)
                {
                    case ImageLoader.FitMode.Stretch:
                        Properties.Settings.Default.StartupFitmode = 0;
                        break;
                    case ImageLoader.FitMode.Fill:
                        Properties.Settings.Default.StartupFitmode = 1;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                Properties.Settings.Default.Save();
            }
        }

        public static bool AutoUpdatePreview
        {
            get { return Properties.Settings.Default.AutoUpdatePreview; }
            set
            {
                Properties.Settings.Default.AutoUpdatePreview = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool LiveColorPreview
        {
            get { return Properties.Settings.Default.LiveColorPreview; }
            set
            {
                Properties.Settings.Default.LiveColorPreview = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
