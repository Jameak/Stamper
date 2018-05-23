using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Stamper.DataAccess;
using Stamper.UI.ViewModels;

namespace Stamper.UI.Windows
{
    public partial class PreviewWindow : Window
    {
        private readonly PreviewWindowViewModel _vm;

        public PreviewWindow(PreviewWindowViewModel vm)
        {
            _vm = vm;
            InitializeComponent();
            DataContext = _vm;
        }

        public void SetImage(Bitmap image)
        {
            var bitmap = BitmapHelper.ConvertBitmapToImageSource(image);
            _vm.BitmapImage = bitmap;

            //Undo dpi-scaling that will be applied to the size of the control, so that when Windows
            // applies its dpi-scaling, the resulting control size is the intended number of pizels.
            var dpiInfo = VisualTreeHelper.GetDpi(this);
            _vm.ImageWidth = (int)(image.Width / dpiInfo.DpiScaleX);
            _vm.ImageHeight = (int)(image.Height / dpiInfo.DpiScaleY);            
        }
    }
}
