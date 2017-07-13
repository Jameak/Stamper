using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stamper.DataAccess;
using Stamper.UI.ViewModels.Base;
using Stamper.UI.ViewModels.Enums;
using FontFamily = System.Windows.Media.FontFamily;

namespace Stamper.UI.ViewModels
{
    public class TokenControlViewModel : BaseViewModel
    {
        private const int DEFAULT_STARTUP_RESOLUTION = 512;
        public ImageLoader.FitMode FitMode { get; private set; } = ImageLoader.FitMode.Stretch;

        //The resolution that the user wants the output image to be.
        public int DesiredResolutionWidth { get; private set; } = DEFAULT_STARTUP_RESOLUTION;
        public int DesiredResolutionHeight { get; private set; } = DEFAULT_STARTUP_RESOLUTION;

        //The resolution of the currently chosen border or overlay.
        public int ActualResolutionWidth { get; private set; } = DEFAULT_STARTUP_RESOLUTION;
        public int ActualResolutionHeight { get; private set; } = DEFAULT_STARTUP_RESOLUTION;

        //The resolution given to the border and overlays after adhering to the chosen FitMode. Will be equal to DesiredResolutionWidth/Height if the FitMode is set to Stretch
        private int _imageResolutionWidth = DEFAULT_STARTUP_RESOLUTION;
        public int ImageResolutionWidth { get { return _imageResolutionWidth; } private set { if (_imageResolutionWidth != value) { _imageResolutionWidth = value; OnPropertyChanged(); } } }

        private int _imageResolutionHeight = DEFAULT_STARTUP_RESOLUTION;
        public int ImageResolutionHeight { get { return _imageResolutionHeight; } private set { if (_imageResolutionHeight != value) { _imageResolutionHeight = value; OnPropertyChanged(); } } }

        private BitmapImage _image;
        public BitmapImage Image { get { return _image; } set { if (!Equals(_image, value)) { _image = value; OnPropertyChanged(); } } }

        private BitmapImage _borderImage;
        public BitmapImage BorderImage { get { return _borderImage; } set { if (!Equals(_borderImage, value)) { _borderImage = value; OnPropertyChanged(); } } }

        private BitmapImage _overlayImage;
        public BitmapImage OverlayImage { get { return _overlayImage; } set { if (!Equals(_overlayImage, value)) { _overlayImage = value; OnPropertyChanged(); } } }
        
        private string _zoomSpeed = "1.2";
        public string ZoomSpeed { get { return _zoomSpeed; } set { _zoomSpeed = value; OnPropertyChanged(); } } //ZoomSpeed on the ZoomBorder isn't a DependencyObject, so it cant have a binding.

        private int _rotationAngle = 0;
        public int RotationAngle { get { return _rotationAngle; } set { if (_rotationAngle != value) { _rotationAngle = value; OnPropertyChanged(); } } }

        private int _textRotationAngle = 0;
        public int TextRotationAngle { get { return _textRotationAngle; } set { if (_textRotationAngle != value) { _textRotationAngle = value; OnPropertyChanged(); } } }

        private string _textContent = "";
        public string TextContent { get { return _textContent; } set { if (_textContent != value) { _textContent = value; OnPropertyChanged(); } } }

        private bool _showTextBorder;
        public bool ShowTextBorder
        {
            get { return _showTextBorder; }
            set
            {
                _showTextBorder = value;
                BorderBrush = value ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Transparent);
                OnPropertyChanged();
                OnPropertyChanged(nameof(BorderBrush));
            }
        }

        public SolidColorBrush BorderBrush { get; set; } = new SolidColorBrush(Colors.Transparent);

        private Visibility _showText = Visibility.Collapsed;
        public Visibility ShowText { get { return _showText; } set { if (_showText != value) { _showText = value; OnPropertyChanged(); } } }

        private FontFamily _textFont = new FontFamily("Arial");
        public FontFamily TextFont { get { return _textFont; } set { if (_textFont != value) { _textFont = value; OnPropertyChanged(); } } }

        private SolidColorBrush _textColor = new SolidColorBrush(Colors.Black);
        public SolidColorBrush TextColor { get { return _textColor; } set { if (_textColor != value) { _textColor = value; OnPropertyChanged(); } } }

        private SolidColorBrush _backdropColor = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush BackdropColor { get { return _backdropColor; } set { if (_backdropColor != value) { _backdropColor = value; OnPropertyChanged(); } } }


        public ICommand UpdateResolution { get; set; }
        public ICommand LoadExternalImageCommand { get; set; }

        public async void LoadExternalImage(string uri, ExternalImageType imagetype)
        {
            Image = BitmapHelper.ConvertBitmapToImageSource(DataAccess.Properties.Resources.Loading);

            try
            {
                switch (imagetype)
                {
                    //Creating BitmapImage objects on non-UI threads has major issues with the thread-owner of the BitmapImage as well as firing OnPropertyChanged-events on DependencyObjects.
                    // So we create a Bitmap asynchonously and then convert it to a BitmapSource later instead.

                    case ExternalImageType.WebContent:
                    {
                        Bitmap img = await BitmapHelper.GetWebContent(uri);
                        Image = BitmapHelper.ConvertBitmapToImageSource(img);
                    }
                        break;
                    case ExternalImageType.LocalFile:
                    {
                        Bitmap img = await BitmapHelper.LoadBitmapAsync(uri);
                        Image = BitmapHelper.ConvertBitmapToImageSource(img);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(imagetype), imagetype, null);
                }
            }
            catch (Exception e) when (e is ArgumentException || e is WebException || e is NotSupportedException)
            {
                Image = BitmapHelper.ConvertBitmapToImageSource(DataAccess.Properties.Resources.LoadFailure);
            }
        }

        public void SetDimensions(ImageLoader.FitMode mode, int desiredWidth, int desiredHeight, int imageWidth, int imageHeight)
        {
            FitMode = mode;
            DesiredResolutionWidth = desiredWidth;
            DesiredResolutionHeight = desiredHeight;
            ActualResolutionWidth = imageWidth;
            ActualResolutionHeight = imageHeight;
            var resolutions = ImageLoader.FitDimensions(FitMode, DesiredResolutionWidth, DesiredResolutionHeight, ActualResolutionWidth, ActualResolutionHeight);
            ImageResolutionWidth = resolutions.Item1;
            ImageResolutionHeight = resolutions.Item2;
        }
    }
}
