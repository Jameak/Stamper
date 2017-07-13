using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Stamper.UI.Events
{
    public class FontChangedEvent : RoutedEventArgs
    {
        private readonly FontFamily _font;

        public FontFamily Font => _font;

        public FontChangedEvent(RoutedEvent routedEvent, FontFamily font) : base(routedEvent)
        {
            _font = font;
        }
    }
}
