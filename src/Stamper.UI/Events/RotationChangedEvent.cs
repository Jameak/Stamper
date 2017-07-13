using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stamper.UI.Events
{
    public class RotationChangedEvent : RoutedEventArgs
    {
        private readonly int _rotationAngle;

        public int RotationAngle => _rotationAngle;

        public RotationChangedEvent(RoutedEvent routedEvent, int rotationAngle) : base(routedEvent)
        {
            _rotationAngle = rotationAngle;
        }
    }
}
