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

        public void SetImage(Bitmap image, int width, int height)
        {
            var bitmap = BitmapHelper.ConvertBitmapToImageSource(image);
            _vm.BitmapImage = bitmap;
            _vm.ImageWidth = width;
            _vm.ImageHeight = height;
        }
    }
}
