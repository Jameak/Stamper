using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class ManageLayersWindow : Window
    {
        private ManageLayersWindowViewModel _vm;

        public ManageLayersWindow()
        {
            InitializeComponent();

            _vm = new ManageLayersWindowViewModel();
            _vm.DeleteLayerCommand = new RelayCommand(o =>
            {
                if (LayerList.SelectedItem != null)
                {
                    LayerSource.DeleteLayer(((ManageLayersWindowViewModel.LayerEntry)LayerList.SelectedItem).Info);
                    _vm.ReloadLayers();
                }
            });

            DataContext = _vm;
        }

        private void AddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new AddLayerWindow()
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();

            win.Closing += (o, args) =>
            {
                if (win.OkClicked)
                {
                    _vm.ReloadLayers();
                }
            };
        }
    }
}
