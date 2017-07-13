using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Stamper.DataAccess;
using Stamper.UI.Controls;
using Stamper.UI.Events;
using Stamper.UI.Filters;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;
using Stamper.UI.ViewModels.Enums;
using Color = System.Drawing.Color;

namespace Stamper.UI.Windows
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;
        private PreviewWindow _preWindow;

        private BorderControlViewModel.BorderInfo _borderInfo;
        private OverlayControlViewModel.OverlayInfo _overlayInfo;
        private Color _overlayTintColor = Color.FromArgb(0, 255, 255, 255); //Default to transparent
        private Color _borderTintColor = Color.FromArgb(0, 255, 255, 255); //Default to transparent
        private FilterMethods.BlendFilterDelegate _overlayBlendFilter = FilterMethods.Normal;
        private FilterMethods.BlendFilterDelegate _borderBlendFilter = FilterMethods.Normal;
        private FilterMethods.BlendFilterDelegate _specialFilter = FilterMethods.None;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            DataContext = _vm;
            
            _vm.ResetImageCommand = new RelayCommand(o =>
            {
                TokenControl.ResetControls();
                SpecialControl.ResetRotation();
                if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
            });
            _vm.OpenPreviewWindow = new RelayCommand(o => OpenPreviewWindow(null, null), o => _preWindow == null);
            _vm.UpdatePreview = new RelayCommand(o => RenderUsingDispatcher(), o => _preWindow != null && !SettingsManager.AutoUpdatePreview);
            _vm.SaveToken = new RelayCommand(o => MenuItemSave_OnClick(null, null));
            _vm.LoadToken = new RelayCommand(o => MenuItemLoad_OnClick(null, null));
            _vm.UpdateZoomSpeed = new RelayCommand(o =>
            {
                TokenControl.SetZoomSpeed(o.ToString());
                _vm.ZoomSpeed = o.ToString();
            });
            _vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_vm.KeepPreviewOnTop) && _preWindow != null)
                {
                    _preWindow.Topmost = _vm.KeepPreviewOnTop;
                }
            };
            TokenControl.SetImage(DataAccess.Properties.Resources.Splash);

            //Load settings-values
            TokenControl.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(SettingsManager.StartupTokenWidth, SettingsManager.StartupTokenHeight, SettingsManager.StartupFitmode));
            

            Loaded += async (sender, args) =>
            {
                await CheckIfUpdateAvailable(false);
            };
        }

        private async Task<bool> CheckIfUpdateAvailable(bool forceCheck)
        {
            var result = await UpdateChecker.CheckForUpdate(forceCheck);
            if (!result.Item1) return false;

            var win = new UpdateAvailableWindow(result.Item2)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();
            return true;
        }

        /// <summary>
        /// Renders the image when all other window rendering has been completed.
        /// </summary>
        internal void RenderUsingDispatcher()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => RenderImage()));
        }

        private void OpenPreviewWindow(object sender, RoutedEventArgs e)
        {
            _preWindow?.Close();

            var sidemargin = 20;
            var topmargin = 30;

            _preWindow = new PreviewWindow(new PreviewWindowViewModel());
            _preWindow.Height = Math.Max(164, TokenControl.ImageResolutionHeight) + topmargin * 2;
            _preWindow.Width = Math.Max(164, TokenControl.ImageResolutionWidth) + sidemargin * 2;
            _preWindow.Closed += (o, args) =>
            {
                _preWindow = null;
                _vm.OpenPreviewWindow.OnCanExecuteChanged(sender);
                _vm.UpdatePreview.OnCanExecuteChanged(sender);
            };
            _preWindow.Show();
            _preWindow.Topmost = _vm.KeepPreviewOnTop;

            _vm.OpenPreviewWindow.OnCanExecuteChanged(sender);
            _vm.UpdatePreview.OnCanExecuteChanged(sender);
            RenderUsingDispatcher();
        }

        private Bitmap RenderImage()
        {
            Bitmap bitmap = TokenControl.RenderVisual(TokenControl.ZoomControl);

            //Apply the special filter.
            BitmapHelper.AddFilter(bitmap, _vm.SpecialFilterColor, _specialFilter);

            // Modify the rendered image.
            if (_overlayInfo != null)
            {
                var overlay = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight);
                BitmapHelper.AddFilter(overlay, _overlayTintColor, _overlayBlendFilter);
                if (!string.IsNullOrWhiteSpace(_overlayInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(overlay, ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.Mask, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight));
                BitmapHelper.AddLayerToImage(bitmap, overlay);
            }

            if (_borderInfo != null)
            {
                if (!string.IsNullOrWhiteSpace(_borderInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(bitmap, ImageLoader.LoadBitmapFromFile(_borderInfo.Info.Mask, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight));
                var border = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight);
                BitmapHelper.AddFilter(border, _borderTintColor, _borderBlendFilter);
                BitmapHelper.AddLayerToImage(bitmap, border);  //Draw the border
            }

            if (TokenControl.ZoomControl_Text.Visibility == Visibility.Visible)
            {
                Bitmap text = TokenControl.RenderVisual(TokenControl.ZoomControl_Text);
                BitmapHelper.AddLayerToImage(bitmap, text);
            }

            if (_vm.Autocrop)
            {
                bitmap = BitmapHelper.Autocrop(bitmap);
            }
            
            //Since we just spent time rendering the image, we might as well update the preview even if the user didn't ask for that specifically.
            _preWindow?.SetImage(bitmap);
            return bitmap;
        }

        internal void UpdateOverlays()
        {
            //When updating overlays, the actual output resolution may be different from the desired resolution if stretching of overlays isn't allowed.
            if (_borderInfo != null)
            {
                var border = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File);
                TokenControl.SetDimensions(border.Width, border.Height);
            }
            else if (_overlayInfo != null)
            {
                var overlay = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File);
                TokenControl.SetDimensions(overlay.Width, overlay.Height);
            }

            if (_borderInfo != null)
            {
                //Border
                var borderImage = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight);
                BitmapHelper.AddFilter(borderImage, _borderTintColor, _borderBlendFilter);

                TokenControl.SetBorderImage(borderImage);
                TokenControl.BorderImage.Height = TokenControl.ImageResolutionHeight;
                TokenControl.BorderImage.Width = TokenControl.ImageResolutionWidth;
            }

            if (_overlayInfo != null)
            {
                //Overlay
                var overlayImage = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight);
                BitmapHelper.AddFilter(overlayImage, _overlayTintColor, _overlayBlendFilter);
                if (!string.IsNullOrWhiteSpace(_overlayInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(overlayImage, ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.Mask, TokenControl.ImageResolutionWidth, TokenControl.ImageResolutionHeight));

                TokenControl.SetOverlayImage(overlayImage);
                TokenControl.OverlayImage.Height = TokenControl.ImageResolutionHeight;
                TokenControl.OverlayImage.Width = TokenControl.ImageResolutionWidth;
            }

            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }

        #region Layer Selection
        private void BorderControl_OnBorderSelected(object sender, RoutedEventArgs e)
        {
            var bc = sender as BorderControl;
            var bi = bc.BorderList.SelectedItem as BorderControlViewModel.BorderInfo;
            _borderInfo = bi;
            UpdateOverlays();
        }

        private void BorderControl_OnTintSelected(object sender, RoutedEventArgs e)
        {
            var color = ((ColorSelectedEvent) e).Color;
            _borderTintColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            UpdateOverlays();
        }

        private void BorderControl_OnTintFilterSelected(object sender, RoutedEventArgs e)
        {
            var bc = sender as BorderControl;
            var filter = bc.FilterBox.SelectedItem as TintFilter;
            _borderBlendFilter = filter.Method;
            UpdateOverlays();
        }

        private void OverlayControl_OnTintFilterSelected(object sender, RoutedEventArgs e)
        {
            var oc = sender as OverlayControl;
            var filter = oc.FilterBox.SelectedItem as TintFilter;
            _overlayBlendFilter = filter.Method;
            UpdateOverlays();
        }

        private void OverlayControl_OnOverlaySelected(object sender, RoutedEventArgs e)
        {
            var oc = sender as OverlayControl;
            var oi = oc.OverlayList.SelectedItem as OverlayControlViewModel.OverlayInfo;
            _overlayInfo = oi;
            UpdateOverlays();
        }

        private void OverlayControl_OnTintSelected(object sender, RoutedEventArgs e)
        {
            var color = ((ColorSelectedEvent)e).Color;
            _overlayTintColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            UpdateOverlays();
        }

        private void SpecialControl_OnFilterSelected(object sender, RoutedEventArgs e)
        {
            var gc = sender as SpecialControl;
            var specialfilter = gc.SpecialFilterBox.SelectedItem as SpecialFilter;
            if (specialfilter != null) _specialFilter = specialfilter.Method;
            UpdateOverlays();
        }

        private void SpecialControl_OnRotationChanged(object sender, RoutedEventArgs e)
        {
            TokenControl.RotationAngle = ((RotationChangedEvent) e).RotationAngle;
            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnTextManipulationChanged(object sender, RoutedEventArgs e)
        {
            if (e is ColorSelectedEvent)
            {
                TokenControl.TextColor = new SolidColorBrush(((ColorSelectedEvent)e).Color);
            }
            else if (e is FontChangedEvent)
            {
                TokenControl.TextFont = ((FontChangedEvent) e).Font; 
            }
            else if (e is TextManipulationEvent)
            {
                var arg = (TextManipulationEvent) e;
                TokenControl.ShowTextBorder = arg.ShowTextBorder;
                TokenControl.ShowText = arg.TextVisible ? Visibility.Visible : Visibility.Collapsed;
                TokenControl.TextRotationAngle = arg.TextRotationAngle;

                //Convert \n to an actual newline
                var text = arg.Text.ToCharArray();
                var finalText = new StringBuilder();
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\\' && i < text.Length - 1 && text[i + 1] == 'n')
                    {
                        finalText.Append(Environment.NewLine);
                        i++;
                    }
                    else
                    {
                        finalText.Append(text[i]);
                    }
                }
                TokenControl.TextContent = finalText.ToString();
            }

            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnButtonZoom(object sender, RoutedEventArgs e)
        {
            var e1 = (ButtonZoomEvent)e;
            switch (e1.ZoomTarget)
            {
                case ButtonZoomEvent.Target.Image:
                    TokenControl.ZoomControl.ManualZoom(e1);
                    break;
                case ButtonZoomEvent.Target.Text:
                    TokenControl.ZoomControl_Text.ManualZoom(e1);
                    break;
                default:
                    throw new ArgumentException();
            }

            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnBackdropColorChanged(object sender, RoutedEventArgs e)
        {
            TokenControl.BackdropColor = new SolidColorBrush(((ColorSelectedEvent)e).Color);

            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnSpecialFilterColorChanged(object sender, RoutedEventArgs e)
        {
            var color = ((ColorSelectedEvent)e).Color;
            _vm.SpecialFilterColor = Color.FromArgb(color.A, color.R, color.G, color.B);

            if (SettingsManager.AutoUpdatePreview) RenderUsingDispatcher();
        }
        #endregion

        #region Menu
        private void MenuItemLoad_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Choose file to load",
                Filter = "Supported Images|*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif",
                Multiselect = false
            };

            var result = dialog.ShowDialog();

            if (result != null && result.Value)
            {
                TokenControl.LoadExternalImageCommand.Execute(new Tuple<string, ExternalImageType>(dialog.FileName, ExternalImageType.LocalFile));
            }
        }

        private void MenuItemSave_OnClick(object sender, RoutedEventArgs e)
        {
            var image = RenderImage();

            var filename = SettingsManager.LastFilename;
            filename = string.IsNullOrWhiteSpace(filename) ? SettingsManager.DefaultFilename : filename;
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Choose save location",
                FileName = filename,
                DefaultExt = ".png",
                AddExtension = true,
                Filter = "All Files|*.*"
            };

            //Manually handle storing information about the last directory so that we can determine if a file with the name already exists.
            var lastSaveDirectory = SettingsManager.LastSaveDirectory;
            if (!string.IsNullOrWhiteSpace(lastSaveDirectory) && Directory.Exists(lastSaveDirectory))
            {
                dialog.InitialDirectory = lastSaveDirectory;
                var uniqueFilename = Filenamer.UniqueFilename(lastSaveDirectory, filename, dialog.DefaultExt);
                dialog.FileName = uniqueFilename;
            }

            var result = dialog.ShowDialog();

            if (result != null && result.Value)
            {
                if (!dialog.FileName.EndsWith(".png")) dialog.FileName = dialog.FileName + ".png";
                image.Save(dialog.FileName, ImageFormat.Png);

                //Save the filename for next time the user saves something, stripping (1), (2), etc. incase the user uses the unique suggested name and the original suggestion already existed.
                var actualfilename = Path.GetFileNameWithoutExtension(dialog.FileName);
                var filenameregex = new Regex(@"^(?<name>.*)\([1-9]*\) *$");
                var match = filenameregex.Match(actualfilename);
                if (match.Success)
                {
                    actualfilename = match.Groups["name"].Value.Trim();
                }

                SettingsManager.LastFilename = actualfilename;
                SettingsManager.LastSaveDirectory = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new AboutWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();
        }

        private void MenuItemRatelimiter_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new RatelimitWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();
        }

        private async void MenuItemImgur_OnClick(object sender, RoutedEventArgs e)
        {
            MenuImgur.IsEnabled = false;

            var win = new UploadingWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();
            var url = await Imgur.UploadImage(RenderImage());
            win.Close();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(this, "Upload failed. The upload-ratelimits have been hit, or something went wrong.", "Upload Failed");
            }
            else
            {
                Process.Start(new ProcessStartInfo(url));
            }

            MenuImgur.IsEnabled = true;
        }

        private void MenuItemCustomSize_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new CustomSizeWindow(TokenControl.FitMode)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();

            win.Closing += (o, args) =>
            {
                int width;
                int height;

                if (win.OkClicked && !string.IsNullOrWhiteSpace(win.Width.Text) && int.TryParse(win.Width.Text, out width) && !string.IsNullOrWhiteSpace(win.Height.Text) && int.TryParse(win.Height.Text, out height))
                {
                    if (win.Stretch.IsChecked.HasValue && win.Stretch.IsChecked.Value)
                    {
                        TokenControl.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(width, height, ImageLoader.FitMode.Stretch));
                        return;
                    }

                    if (win.Fill.IsChecked.HasValue && win.Fill.IsChecked.Value)
                    {
                        TokenControl.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(width, height, ImageLoader.FitMode.Fill));
                        return;
                    }
                }
            };
        }

        private void MenuItemLoadInstructions_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new InstructionsWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.Show();
        }

        private void MenuItemManageLayers_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new ManageLayersWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            win.Show();

            win.Closing += (o, args) =>
            {
                Borders.RefreshLayers();
                Overlays.RefreshLayers();
            };
        }

        private async void MenuItemCheckForUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            var updateAvailable = await CheckIfUpdateAvailable(true);
            if (!updateAvailable) MessageBox.Show(this, $"No update is available.\nCurrent version: {SettingsManager.Version}", "No update");
        }

        private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.Show();
            window.Closed += (o, args) => _vm.UpdatePreview.OnCanExecuteChanged(null);
        }
        
        private void MenuItemSize_OnUpdateResolution(object sender, RoutedEventArgs e)
        {
            var value = int.Parse(((MenuItem)sender).CommandParameter.ToString());

            TokenControl.UpdateResolution.Execute(value);
        }
        #endregion

        #region TokenControl event handlers
        private void TokenControl_OnTextRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            SpecialControl.SetTextRotationAngle(arg.AngleDelta);
        }

        private void TokenControl_OnRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            SpecialControl.SetRotationAngle(arg.AngleDelta);
        }

        private void TokenControl_OnImageChanged(object sender, RoutedEventArgs e)
        {
            if (SettingsManager.AutoUpdatePreview)
            {
                RenderUsingDispatcher();
            }
        }
        #endregion
    }
}
