using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Stamper.UI.Events;
using Wpf.Controls.PanAndZoom;

namespace Stamper.UI.Controls
{
    public class CustomZoomBorder : ZoomBorder
    {
        public CustomZoomBorder() : base()
        {
            PreviewMouseWheel += Border_PreviewMouseWheel;

            Unloaded += (sender, args) => PreviewMouseWheel -= Border_PreviewMouseWheel;
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                RaiseEvent(new RotationEvent(RotationEvent, e.Delta < 0 ? -5 : 5));
                e.Handled = true;
            }
        }

        public void ManualZoom(ButtonZoomEvent e)
        {
            ZoomDeltaTo(e.Direction < 0 ? -120 : 120, new Point(ActualWidth/2, ActualHeight/2));
        }


        public static readonly RoutedEvent RotationEvent =
            EventManager.RegisterRoutedEvent(nameof(RotationEvent), RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler), typeof(ZoomBorder_NotHitTestable));

        public event RoutedEventHandler Rotation
        {
            add { AddHandler(RotationEvent, value); }
            remove { RemoveHandler(RotationEvent, value); }
        }
    }
}
