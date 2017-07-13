using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stamper.UI.Events
{
    public class ButtonZoomEvent : RoutedEventArgs
    {
        private readonly int _direction;
        private readonly Target _target;

        public int Direction => _direction;
        public Target ZoomTarget => _target;

        public ButtonZoomEvent(RoutedEvent routedEvent, int direction, Target target) : base(routedEvent)
        {
            _direction = direction;
            _target = target;
        }

        public enum Target
        {
            Image, Text
        }
    }
}
