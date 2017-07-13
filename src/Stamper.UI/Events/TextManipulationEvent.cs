using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stamper.UI.Events
{
    public class TextManipulationEvent : RoutedEventArgs
    {
        private readonly bool _showTextBorder;
        private readonly bool _textVisible;
        private readonly string _text;
        private readonly int _textRotationAngle;

        public bool ShowTextBorder => _showTextBorder;
        public bool TextVisible => _textVisible;
        public string Text => _text;
        public int TextRotationAngle => _textRotationAngle;

        public TextManipulationEvent(RoutedEvent routedEvent, bool showTextBorder, bool textVisible, string text, int textRotationAngle) : base(routedEvent)
        {
            _showTextBorder = showTextBorder;
            _textVisible = textVisible;
            _text = text;
            _textRotationAngle = textRotationAngle;
        }
    }
}
