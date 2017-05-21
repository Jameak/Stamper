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
using ColorPickerWPF.Code;
using Stamper.UI.Events;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.Controls
{
    public partial class BorderControl : UserControl
    {
        private BorderControlViewModel _vm;

        public BorderControl()
        {
            InitializeComponent();

            _vm = new BorderControlViewModel();
            DataContext = _vm;

            _vm.ColorPickCommand = new RelayCommand(o =>
            {
                ColorPickerControl.ColorPickerChangeHandler previewHandler =
                    selectedColor => RaiseEvent(new ColorSelectedEvent(TintSelectedEvent, selectedColor));

                Color color;
                bool ok = ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.SelectColor, previewHandler);

                if (ok)
                {
                    _vm.SelectColor = color;
                }

                RaiseEvent(new ColorSelectedEvent(TintSelectedEvent, _vm.SelectColor));
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
