using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stamper.UI.Events
{
    public class RotationEvent : RoutedEventArgs
    {
        private readonly int _angleDelta;

        public int AngleDelta => _angleDelta;

        public RotationEvent(RoutedEvent routedEvent, int delta) : base(routedEvent)
        {
            _angleDelta = delta;
        }
    }
}
