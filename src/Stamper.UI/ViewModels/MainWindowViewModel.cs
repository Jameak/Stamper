using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stamper.UI.ViewModels.Base;
using FontFamily = System.Windows.Media.FontFamily;

namespace Stamper.UI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private int _imageResolution = 512;
        public int ImageResolution { get { return _imageResolution; } set { if (_imageResolution != value) { _imageResolution = value; OnPropertyChanged(); } } }
        
        private BitmapImage _image;
        public BitmapImage Image { get { return _image; } set { if (!Equals(_image, value)) { _image = value; OnPropertyChanged(); } } }

        private bool _autoUpdatePreview;
        public bool AutoUpdatePreview { get { return _autoUpdatePreview; } set { if (_autoUpdatePreview != value) { _autoUpdatePreview = value; OnPropertyChanged(); } } }

        private string _zoomSpeed = "1.2";
        public string ZoomSpeed { get { return _zoomSpeed; } set { _zoomSpeed = value; OnPropertyChanged(); } } //ZoomSpeed on the ZoomBorder isn't a DependencyObject, so it cant have a binding.

        private int _rotationAngle = 0;
        public int RotationAngle { get { return _rotationAngle; } set { if (_rotationAngle != value) { _rotationAngle = value; OnPropertyChanged(); } } }

        private int _textRotationAngle = 0;
        public int TextRotationAngle { get { return _textRotationAngle; } set { if (_textRotationAngle != value) { _textRotationAngle = value; OnPropertyChanged(); } } }

        private string _textContent = "";
        public string TextContent { get { return _textContent; } set { if (_textContent != value) { _textContent = value; OnPropertyChanged(); } } }

        private bool _showTextBorder;
        public bool ShowTextBorder { get { return _showTextBorder; } set
        {
            _showTextBorder = value;
            BorderBrush = value ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Transparent);
            OnPropertyChanged();
            OnPropertyChanged(nameof(BorderBrush));
        } }

        public SolidColorBrush BorderBrush { get; set; } = new SolidColorBrush(Colors.Transparent);

        private Visibility _showText = Visibility.Collapsed;
        public Visibility ShowText { get { return _showText; } set { if (_showText != value) { _showText = value; OnPropertyChanged(); } } }

        private FontFamily _textFont = new FontFamily("Arial");
        public FontFamily TextFont { get { return _textFont; } set { if (_textFont != value) { _textFont = value; OnPropertyChanged(); } } }

        private SolidColorBrush _textColor = new SolidColorBrush(Colors.Black);
        public SolidColorBrush TextColor { get { return _textColor; } set { if (_textColor != value) { _textColor = value; OnPropertyChanged(); } } }


        public ICommand SaveToken { get; set; }
        public ICommand LoadToken { get; set; }
        public ICommand ResetImageCommand { get; set; }
        public ICommand UpdateResolution { get; set; }
        public ICommand ShowInstructions { get; set; }
        public RelayCommand OpenPreviewWindow { get; set; }
        public RelayCommand UpdatePreview { get; set; }
        public RelayCommand UpdateZoomSpeed { get; set; }
        
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
                        img = BitmapHelper.ConvertToPixelFormat_32bppArgb(img);
                        Image = BitmapHelper.ConvertBitmapToImageSource(img);
                    }
                        break;
                    case ExternalImageType.LocalFile:
                    {
                        Bitmap img = await Task.Factory.StartNew(() =>
                        {
                            var file = File.ReadAllBytes(uri);
                            using (var ms = new MemoryStream(file))
                            {
                                return new Bitmap(ms);
                            }
                        });
                        img = BitmapHelper.ConvertToPixelFormat_32bppArgb(img);
                        
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
    }

    public enum ExternalImageType
    {
        LocalFile, WebContent
    }
}
