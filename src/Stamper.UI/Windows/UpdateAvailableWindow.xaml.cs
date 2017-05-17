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
using Stamper.DataAccess;

namespace Stamper.UI.Windows
{
    public partial class UpdateAvailableWindow : Window
    {
        public UpdateAvailableWindow(string newVersion)
        {
            InitializeComponent();
            PInvokeHelper.DisableMinimizeButton(this);
            PInvokeHelper.DisableMaximizeButton(this);

            CurrentVersion.Text = $"Current version: {SettingsManager.Version}";
            AvailabeVersion.Text = $"Available version: {newVersion}";
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DontNotifyButton_OnClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.IgnoreUpdates = true;
            Close();
        }

        private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Jameak/Stamper/releases");
            Close();
        }
    }
}
