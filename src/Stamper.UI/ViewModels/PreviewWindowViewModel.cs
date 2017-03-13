using System.Windows.Media.Imaging;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class PreviewWindowViewModel : BaseViewModel
    {
        private BitmapImage _bitmapImage = null;
        public BitmapImage BitmapImage { get { return _bitmapImage; } set { _bitmapImage = value; OnPropertyChanged(); } }

        private int _imageWidth;
        public int ImageWidth { get { return _imageWidth; } set { if (_imageWidth != value) { _imageWidth = value; OnPropertyChanged(); } } }

        private int _imageHeight;
        public int ImageHeight { get { return _imageHeight; } set { if (_imageHeight != value) { _imageHeight = value; OnPropertyChanged(); } } }

    }
}
