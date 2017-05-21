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
    public partial class OverlayControl : UserControl
    {
        private OverlayControlViewModel _vm;

        public OverlayControl()
        {
            InitializeComponent();

            _vm = new OverlayControlViewModel();
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

            DataContext = _vm;
        }

        public void RefreshLayers()
        {
            _vm.LoadLayers();
        }

        private void OverlaySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OverlaySelectedEvent));
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(TintFilterSelectedEvent));
        }

        #region EventRouting
        public static readonly RoutedEvent OverlaySelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(OverlaySelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(OverlayControl));

        public event RoutedEventHandler OverlaySelected
        {
            add { AddHandler(OverlaySelectedEvent, value); }
            remove { RemoveHandler(OverlaySelectedEvent, value); }
        }


        public static readonly RoutedEvent TintSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(TintSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(OverlayControl));

        public event RoutedEventHandler TintSelected
        {
            add { AddHandler(TintSelectedEvent, value); }
            remove { RemoveHandler(TintSelectedEvent, value); }
        }

        public static readonly RoutedEvent TintFilterSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(TintFilterSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(OverlayControl));

        public event RoutedEventHandler TintFilterSelected
        {
            add { AddHandler(TintFilterSelectedEvent, value); }
            remove { RemoveHandler(TintFilterSelectedEvent, value); }
        }
        #endregion
    }
}
