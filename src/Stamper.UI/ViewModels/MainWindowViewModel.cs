using System;
using System.Drawing;
using System.IO;
using System.Net;
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
    public class MainWindowViewModel : BaseViewModel
    {
        private string _zoomSpeed = "1.2";
        public string ZoomSpeed { get { return _zoomSpeed; } set { _zoomSpeed = value; OnPropertyChanged(); } } //ZoomSpeed on the ZoomBorder isn't a DependencyObject, so it cant have a binding.

        private bool _keepPreviewOnTop;
        public bool KeepPreviewOnTop { get { return _keepPreviewOnTop; } set { if (_keepPreviewOnTop != value) { _keepPreviewOnTop = value; OnPropertyChanged(); } } }

        private bool _autocrop;
        public bool Autocrop { get { return _autocrop; } set { if (_autocrop != value) { _autocrop = value; OnPropertyChanged(); } } }
        
        private System.Drawing.Color _specialFilterColor = System.Drawing.Color.Transparent;
        public System.Drawing.Color SpecialFilterColor { get { return _specialFilterColor; } set { if (_specialFilterColor != value) { _specialFilterColor = value; OnPropertyChanged(); } } }
        
        public ICommand SaveToken { get; set; }
        public ICommand LoadToken { get; set; }
        public ICommand ResetImageCommand { get; set; }
        public RelayCommand OpenPreviewWindow { get; set; }
        public RelayCommand UpdatePreview { get; set; }
        public RelayCommand UpdateZoomSpeed { get; set; }
    }
}
