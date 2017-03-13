using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stamper.UI.Windows
{
    public partial class UpdateAvailableWindow : Window
    {
        public UpdateAvailableWindow(string newVersion)
        {
            InitializeComponent();

            CurrentVersion.Text = $"Current version: {DataAccess.Properties.Settings.Default.Version}";
            AvailabeVersion.Text = $"Available version: {newVersion}";
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DontNotifyButton_OnClick(object sender, RoutedEventArgs e)
        {
            DataAccess.Properties.Settings.Default.IgnoreUpdates = true;
            DataAccess.Properties.Settings.Default.Save();
            Close();
        }

        private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Jameak/Stamper/releases");
            Close();
        }
    }
}
