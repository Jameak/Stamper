using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Stamper.DataAccess;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class ManageLayersWindowViewModel : BaseViewModel
    {
        public ObservableCollection<LayerEntry> Layers { get; set; } = new ObservableCollection<LayerEntry>();

        public ManageLayersWindowViewModel()
        {
            ReloadLayers();
        }

        public void ReloadLayers()
        {
            Layers.Clear();

            var previewResolution = 40;

            foreach (var layer in LayerSource.GetLayers())
            {
                Layers.Add(new LayerEntry
                {
                    Image = BitmapHelper.ConvertBitmapToImageSource(LayerSource.LoadBitmapFromFile(layer.File, previewResolution, previewResolution)),
                    Info = layer
                });
            }
        }

        public ICommand DeleteLayerCommand { get; set; }

        public class LayerEntry
        {
            public ImageSource Image { get; set; }
            public Layer Info { get; set; }
        }
    }
}
