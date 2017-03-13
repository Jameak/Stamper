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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColorPickerWPF;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.Controls
{
    public partial class BorderControl : UserControl
    {
        private BorderControlViewModel _vm;
        public Color SelectedColor => _vm.SelectColor;

        public BorderControl()
        {
            InitializeComponent();

            _vm = new BorderControlViewModel();
            DataContext = _vm;

            _vm.ColorPickCommand = new RelayCommand(o =>
            {
                Color color;
                bool ok = ColorPickerWindow.ShowDialog(out color);
                if (ok)
                {
                    _vm.SelectColor = color;
                    RaiseEvent(new RoutedEventArgs(TintSelectedEvent));
                }
            });
        }

        public void RefreshLayers()
        {
            _vm.LoadLayers();
        }

        private void BorderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(BorderSelectedEvent));
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(TintFilterSelectedEvent));
        }

        #region EventRouting
        public static readonly RoutedEvent BorderSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(BorderSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(BorderControl));

        public event RoutedEventHandler BorderSelected
        {
            add { AddHandler(BorderSelectedEvent, value); }
            remove { RemoveHandler(BorderSelectedEvent, value); }
        }


        public static readonly RoutedEvent TintSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(TintSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(BorderControl));

        public event RoutedEventHandler TintSelected
        {
            add { AddHandler(TintSelectedEvent, value); }
            remove { RemoveHandler(TintSelectedEvent, value); }
        }


        public static readonly RoutedEvent TintFilterSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(TintFilterSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(BorderControl));

        public event RoutedEventHandler TintFilterSelected
        {
            add { AddHandler(TintFilterSelectedEvent, value); }
            remove { RemoveHandler(TintFilterSelectedEvent, value); }
        }
        #endregion
    }
}
