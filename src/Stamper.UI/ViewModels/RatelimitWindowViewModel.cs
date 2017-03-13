using System.Threading.Tasks;
using System.Windows.Input;
using Stamper.DataAccess;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class RatelimitWindowViewModel : BaseViewModel
    {
        private int _clientWideLimit = -1;
        public int ClientWideLimit { get { return _clientWideLimit; } set { if (_clientWideLimit != value) { _clientWideLimit = value; OnPropertyChanged(); } } }

        private int _ipLimit = -1;
        public int IpLimit { get { return _ipLimit; } set { if (_ipLimit != value) { _ipLimit = value; OnPropertyChanged(); } } }

        public ICommand RefreshCommand { get; set; }

        public async Task Refresh()
        {
            var rates = await Imgur.GetUploadRatelimits();
            IpLimit = rates.Item1;
            ClientWideLimit = rates.Item2;
        }
    }
}
