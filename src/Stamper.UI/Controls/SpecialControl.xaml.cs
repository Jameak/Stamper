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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColorPickerWPF;
using ColorPickerWPF.Code;
using Stamper.DataAccess;
using Stamper.UI.Events;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.Controls
{
    public partial class SpecialControl : UserControl
    {
        public SpecialControlViewModel _vm;

        public SpecialControl()
        {
            InitializeComponent();
            _vm = new SpecialControlViewModel();
            DataContext = _vm;

            _vm.ColorPickCommand = new RelayCommand(o =>
            {
                ColorPickerControl.ColorPickerChangeHandler previewHandler =
                    selectedColor => RaiseEvent(new ColorSelectedEvent(TextManipulationChangedEvent, selectedColor));

                Color color;
                bool ok = SettingsManager.LiveColorPreview
                    ? ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.TextColor, previewHandler)
                    : ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.TextColor);

                if (ok)
                {
                    _vm.TextColor = color;
                }

                RaiseEvent(new ColorSelectedEvent(TextManipulationChangedEvent, _vm.TextColor));
            });

            _vm.BackdropColorPickCommand = new RelayCommand(o =>
            {
                ColorPickerControl.ColorPickerChangeHandler previewHandler =
                    selectedColor => RaiseEvent(new ColorSelectedEvent(BackdropColorChangedEvent, selectedColor));

                Color color;
                bool ok = SettingsManager.LiveColorPreview
                    ? ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.BackdropColor, previewHandler)
                    : ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.BackdropColor);

                if (ok)
                {
                    _vm.BackdropColor = color;
                }

                RaiseEvent(new ColorSelectedEvent(BackdropColorChangedEvent, _vm.BackdropColor));
            });

            _vm.SpecialFilterColorPickCommand = new RelayCommand(o =>
            {
                ColorPickerControl.ColorPickerChangeHandler previewHandler =
                    selectedColor => RaiseEvent(new ColorSelectedEvent(SpecialFilterColorChangedEvent, selectedColor));

                Color color;
                bool ok = SettingsManager.LiveColorPreview
                    ? ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.SpecialFilterColor, previewHandler)
                    : ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.None, null, _vm.SpecialFilterColor);

                if (ok)
                {
                    _vm.SpecialFilterColor = color;
                }

                RaiseEvent(new ColorSelectedEvent(SpecialFilterColorChangedEvent, _vm.SpecialFilterColor));
            });

            _vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_vm.RotationAngle)) RaiseEvent(new RoutedEventArgs(RotationChangedEvent));
                if (args.PropertyName == nameof(_vm.TextManipulationShowBorder) ||
                    args.PropertyName == nameof(_vm.TextManipulationShowText) ||
                    args.PropertyName == nameof(_vm.TextRotationAngle) || args.PropertyName == nameof(_vm.TextContent))
                    RaiseEvent(new RoutedEventArgs(TextManipulationChangedEvent));
            };
            _vm.ManualZoomImage = new RelayCommand(o =>
            {
                RaiseEvent(new ButtonZoomEvent(ButtonZoomEvent, int.Parse(o.ToString()), "Image"));
            });
            _vm.ManualZoomText = new RelayCommand(o =>
            {
                RaiseEvent(new ButtonZoomEvent(ButtonZoomEvent, int.Parse(o.ToString()), "Text"));
            });
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(FilterSelectedEvent));
        }

        private void FontBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(TextManipulationChangedEvent));
        }

        private void InputValidation(object sender, TextCompositionEventArgs e)
        {
            //Allow the user to input a minus-sign (but only one)
            if (AngleBox.Text != string.Empty && AngleBox.Text.First() != '-' && e.Text != string.Empty && e.Text.First() == '-' && TextAngle.CaretIndex == 0)
            {
                return;
            }

            SharedEventHandlingLogic.InputValidation_ContrainToInt(sender, e);
        }

        private void InputValidationOnPaste(object sender, DataObjectPastingEventArgs e)
        {
            SharedEventHandlingLogic.InputValidationOnPaste_ContrainToInt(sender, e);
        }

        private void InputValidation_Text(object sender, TextCompositionEventArgs e)
        {
            //Allow the user to input a minus-sign (but only one)
            if (TextAngle.Text != string.Empty && TextAngle.Text.First() != '-' && e.Text != string.Empty && e.Text.First() == '-' && TextAngle.CaretIndex == 0)
            {
                return;
            }

            SharedEventHandlingLogic.InputValidation_ContrainToInt(sender, e);
        }

        private void AngleBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AngleBox.Text)) _vm.RotationAngle = "0";

            _vm.FireRotationProperty.Execute(null);
        }

        private void TextBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextAngle.Text)) _vm.TextRotationAngle = "0";
        }

        public void SetTextRotationAngle(int angleDelta)
        {
            _vm.TextRotationAngle = (int.Parse(_vm.TextRotationAngle) + angleDelta).ToString();
        }

        public void SetRotationAngle(int angleDelta)
        {
            _vm.RotationAngle = (int.Parse(_vm.RotationAngle) + angleDelta).ToString();
        }

        #region EventRouting
        public static readonly RoutedEvent FilterSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(FilterSelectedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler FilterSelected
        {
            add { AddHandler(FilterSelectedEvent, value); }
            remove { RemoveHandler(FilterSelectedEvent, value); }
        }


        public static readonly RoutedEvent RotationChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(RotationChangedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler RotationChanged
        {
            add { AddHandler(RotationChangedEvent, value); }
            remove { RemoveHandler(RotationChangedEvent, value); }
        }


        public static readonly RoutedEvent TextManipulationChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(TextManipulationChangedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler TextManipulationChanged
        {
            add { AddHandler(TextManipulationChangedEvent, value); }
            remove { RemoveHandler(TextManipulationChangedEvent, value); }
        }


        public static readonly RoutedEvent ButtonZoomEvent =
            EventManager.RegisterRoutedEvent(nameof(ButtonZoomEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler ButtonZoom
        {
            add { AddHandler(ButtonZoomEvent, value); }
            remove { RemoveHandler(ButtonZoomEvent, value); }
        }


        public static readonly RoutedEvent BackdropColorChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(BackdropColorChangedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler BackdropColorChanged
        {
            add { AddHandler(BackdropColorChangedEvent, value); }
            remove { RemoveHandler(BackdropColorChangedEvent, value); }
        }


        public static readonly RoutedEvent SpecialFilterColorChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SpecialFilterColorChangedEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(SpecialControl));

        public event RoutedEventHandler SpecialFilterColorChanged
        {
            add { AddHandler(SpecialFilterColorChangedEvent, value); }
            remove { RemoveHandler(SpecialFilterColorChangedEvent, value); }
        }
        #endregion

    }
}
