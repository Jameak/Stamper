using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Stamper.DataAccess;
using Stamper.UI.Filters;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class OverlayControlViewModel : BaseViewModel
    {
        public ObservableCollection<OverlayInfo> Overlays { get; set; } = new ObservableCollection<OverlayInfo>();
        public ObservableCollection<TintFilter> TintChoices { get; set; } = new ObservableCollection<TintFilter>();
        public ICommand ColorPickCommand { get; set; }

        private Color _selectColor = Colors.Transparent;
        public Color SelectColor { get { return _selectColor; } set { if (_selectColor != value) { _selectColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColorBrush)); } } }
        public SolidColorBrush ColorBrush => new SolidColorBrush(_selectColor);

        public OverlayControlViewModel()
        {
            LoadLayers();

            foreach (var implementedFilter in FilterMethods.TintFilters)
            {
                TintChoices.Add(implementedFilter);
            }
        }

        public void LoadLayers()
        {
            Overlays.Clear();
            var previewResolution = 75;

            var layers = LayerSource.GetLayers().ToList();

            //Ensure that the empty layer is the first option in the list.
            var emptyLayer = layers.FirstOrDefault(i => i.Type == Layer.LayerType.Overlay && i.Name == "None");
            if (emptyLayer != null) Overlays.Add(new OverlayInfo
            {
                Image = BitmapHelper.ConvertBitmapToImageSource(LayerSource.GetBitmapFromFile(emptyLayer.File, previewResolution, 5)),
                Info = emptyLayer
            });

            foreach (var layer in layers.Where(i => i.Type == Layer.LayerType.Overlay && i != emptyLayer))
            {
                Overlays.Add(new OverlayInfo
                {
                    Image = BitmapHelper.ConvertBitmapToImageSource(LayerSource.GetBitmapFromFile(layer.File, previewResolution, previewResolution)),
                    Info = layer
                });
            }
        }

        public class OverlayInfo
        {
            public ImageSource Image { get; set; }
            public Layer Info { get; set; }
        }
    }
}
