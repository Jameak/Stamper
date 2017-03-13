using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Stamper.DataAccess;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class AddLayerViewModel : BaseViewModel
    {
        private string _name;
        public string Name { get { return _name; } set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

        private string _file;
        public string File { get { return _file; } set { if (_file != value) { _file = value; OnPropertyChanged(); } } }

        private string _mask;
        public string Mask { get { return _mask; } set { if (_mask != value) { _mask = value; OnPropertyChanged(); } } }

        public ObservableCollection<Layer.LayerType> LayerTypes { get; set; } = new ObservableCollection<Layer.LayerType>();

        public ICommand SelectFileCommand { get; set; }
        public ICommand SelectMaskCommand { get; set; }

        public AddLayerViewModel()
        {

            foreach (var layer in Enum.GetValues(typeof(Layer.LayerType)).Cast<Layer.LayerType>())
            {
                LayerTypes.Add(layer);
            }
            //LayerTypes.Add(Layer.LayerType.Border);
            //LayerTypes.Add(Layer.LayerType.Overlay);
        }
    }
}
