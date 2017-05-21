using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Stamper.UI.Events
{
    public class ColorSelectedEvent : RoutedEventArgs
    {
        public Color Color { get; }

        public ColorSelectedEvent(RoutedEvent routedEvent, System.Windows.Media.Color color) : base(routedEvent)
        {
            Color = color;
        }
    }
}
