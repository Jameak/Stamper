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
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.Windows
{
    public partial class RatelimitWindow : Window
    {
        private RatelimitWindowViewModel _vm;

        public RatelimitWindow()
        {
            InitializeComponent();
            _vm = new RatelimitWindowViewModel();
            DataContext = _vm;

            _vm.RefreshCommand = new RelayCommand(async o =>
            {
                RefreshButton.IsEnabled = false;
                await _vm.Refresh();
                RefreshButton.IsEnabled = true;
            });

            _vm.RefreshCommand.Execute(null);
        }
    }
}
