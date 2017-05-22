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
using Stamper.UI.ViewModels;

namespace Stamper.UI.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            PInvokeHelper.DisableMinimizeButton(this);
            PInvokeHelper.DisableMaximizeButton(this);

            LoadSettings();
        }

        private void LoadSettings()
        {
            EnablePreviewAutoUpdate.IsChecked = SettingsManager.AutoUpdatePreview;
            EnableLiveColorPreview.IsChecked = SettingsManager.LiveColorPreview;
            NotifyMe.IsChecked = !SettingsManager.IgnoreUpdates;
            TokenWidth.Text = SettingsManager.StartupTokenWidth.ToString();
            TokenHeight.Text = SettingsManager.StartupTokenHeight.ToString();
            switch (SettingsManager.StartupFitmode)
            {
                case ImageLoader.FitMode.Fill:
                    FitMode_Fill.IsChecked = true;
                    break;
                case ImageLoader.FitMode.Stretch:
                    FitMode_Stretch.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RestoreDefaults_OnClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.AutoUpdatePreview = true;
            SettingsManager.LiveColorPreview = true;
            SettingsManager.IgnoreUpdates = false;
            SettingsManager.StartupTokenWidth = 512;
            SettingsManager.StartupTokenHeight = 512;
            SettingsManager.StartupFitmode = ImageLoader.FitMode.Stretch;
            LoadSettings();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.AutoUpdatePreview = EnablePreviewAutoUpdate.IsChecked.Value;
            SettingsManager.LiveColorPreview = EnableLiveColorPreview.IsChecked.Value;
            SettingsManager.IgnoreUpdates = !NotifyMe.IsChecked.Value;

            int width;
            if (int.TryParse(TokenWidth.Text, out width))
            {
                SettingsManager.StartupTokenWidth = width;
            }

            int height;
            if (int.TryParse(TokenHeight.Text, out height))
            {
                SettingsManager.StartupTokenHeight = height;
            }

            if (FitMode_Fill.IsChecked.Value)
            {
                SettingsManager.StartupFitmode = ImageLoader.FitMode.Fill;
            } else if (FitMode_Stretch.IsChecked.Value)
            {
                SettingsManager.StartupFitmode = ImageLoader.FitMode.Stretch;
            }
            
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InputValidation(object sender, TextCompositionEventArgs e)
        {
            SharedEventHandlingLogic.InputValidation_ContrainToInt(sender, e);
        }

        private void InputValidationOnPaste(object sender, DataObjectPastingEventArgs e)
        {
            SharedEventHandlingLogic.InputValidationOnPaste_ContrainToInt(sender, e);
        }
    }
}
