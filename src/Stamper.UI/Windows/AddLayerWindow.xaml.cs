using System;
using System.Collections.Generic;
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
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.Windows
{
    /// <summary>
    /// Interaction logic for AddLayerWindow.xaml
    /// </summary>
    public partial class AddLayerWindow : Window
    {
        private AddLayerViewModel _vm;
        public bool OkClicked = false;

        public AddLayerWindow()
        {
            InitializeComponent();
            _vm = new AddLayerViewModel();

            _vm.SelectFileCommand = new RelayCommand(o =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = "Choose file",
                    Filter = "Supported Images|*.svg;*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif",
                    Multiselect = false
                };

                var result = dialog.ShowDialog();

                if (result != null && result.Value)
                {
                    _vm.File = dialog.FileName;
                }
            });

            _vm.SelectMaskCommand = new RelayCommand(o =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = "Choose mask",
                    Filter = "Supported Images|*.svg;*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif",
                    Multiselect = false
                };

                var result = dialog.ShowDialog();

                if (result != null && result.Value)
                {
                    _vm.Mask = dialog.FileName;
                }
            });

            DataContext = _vm;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            bool valid = !string.IsNullOrWhiteSpace(_vm.Name);
            if (string.IsNullOrWhiteSpace(_vm.File)) valid = false;

            if (valid)
            {
                var success = LayerSource.CreateNewLayer(_vm.Name, _vm.File, _vm.Mask, (Layer.LayerType) LayerType.SelectedItem);
                if (success)
                {
                    OkClicked = true;
                    Close();
                    return;
                }

                MessageBox.Show(this, "Layer creation failed.");
            }
            else
            {
                MessageBox.Show(this, "Layer Name and Layer File are required.");
            }
        }
    }
}
