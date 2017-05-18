using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using Stamper.UI.Filters;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class SpecialControlViewModel : BaseViewModel
    {
        public ObservableCollection<SpecialFilter> SpecialFilters { get; set; } = new ObservableCollection<SpecialFilter>();

        private string _rotationAngle = "0";
        public string RotationAngle { get { return _rotationAngle; } set { if (_rotationAngle != value) { _rotationAngle = value; OnPropertyChanged(); } } }

        private bool _textManipulationShowBorder;
        public bool TextManipulationShowBorder { get { return _textManipulationShowBorder; } set { if (_textManipulationShowBorder != value) { _textManipulationShowBorder = value; OnPropertyChanged(); } } }

        private bool _textManipulationShowText;
        public bool TextManipulationShowText { get { return _textManipulationShowText; } set { if (_textManipulationShowText != value) { _textManipulationShowText = value; OnPropertyChanged(); } } }

        private string _textRotationAngle = "0";
        public string TextRotationAngle { get { return _textRotationAngle; } set { if (_textRotationAngle != value) { _textRotationAngle = value; OnPropertyChanged(); } } }

        private string _textContent = "";
        public string TextContent { get { return _textContent; } set { if (_textContent != value) { _textContent = value; OnPropertyChanged(); } } }

        private Color _textColor = Colors.Black;
        public Color TextColor { get { return _textColor; } set { if (_textColor != value) { _textColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColorBrush)); } } }
        public SolidColorBrush ColorBrush => new SolidColorBrush(_textColor);

        private Color _backdropColor = Colors.Transparent;
        public Color BackdropColor { get { return _backdropColor; } set { if (_backdropColor != value) { _backdropColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackdropColorBrush)); } } }
        public SolidColorBrush BackdropColorBrush => new SolidColorBrush(_backdropColor);

        private Color _specialFilterColor = Colors.Transparent;
        public Color SpecialFilterColor { get { return _specialFilterColor; } set { if (_specialFilterColor != value) { _specialFilterColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpecialFilterColorBrush)); } } }
        public SolidColorBrush SpecialFilterColorBrush => new SolidColorBrush(_specialFilterColor);

        public ICommand FireRotationProperty { get; private set; }
        public ICommand ColorPickCommand { get; set; }
        public ICommand BackdropColorPickCommand { get; set; }
        public ICommand SpecialFilterColorPickCommand { get; set; }
        public ICommand ManualZoomImage { get; set; }
        public ICommand ManualZoomText { get; set; }

        public SpecialControlViewModel()
        {
            foreach (var filter in FilterMethods.SpecialFilters)
            {
                SpecialFilters.Add(filter);
            }

            FireRotationProperty = new RelayCommand(o => OnPropertyChanged(nameof(RotationAngle)));
        }
    }
}
