using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Stamper.DataAccess;
using Stamper.UI.Controls;
using Stamper.UI.Events;
using Stamper.UI.Filters;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;
using Stamper.UI.ViewModels.Enums;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

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
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            DataContext = _vm;

            _vm.UpdateResolution = new RelayCommand(param =>
            {
                if (param != null)
                {
                    if (param is Tuple<int,int,ImageLoader.FitMode>)
                    {
                        var tuple = (Tuple<int, int, ImageLoader.FitMode>) param;
                        _vm.SetDimensions(tuple.Item3, tuple.Item1, tuple.Item2, _vm.ActualResolutionWidth, _vm.ActualResolutionHeight);
                    }
                    else
                    {
                        int res;
                        if (int.TryParse(param.ToString(), out res))
                        {
                            _vm.SetDimensions(_vm.FitMode, res, res, _vm.ActualResolutionWidth, _vm.ActualResolutionHeight);
                        }
                    }
                }
                UpdateOverlays();
            });
            _vm.ResetImageCommand = new RelayCommand(o =>
            {
                ZoomControl.Reset();
                ZoomControl_Text.Reset();
                SpecialControl._vm.RotationAngle = "0";
                SpecialControl._vm.TextRotationAngle = "0";
                if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
            });
            _vm.OpenPreviewWindow = new RelayCommand(o => OpenPreviewWindow(null, null), o => _preWindow == null);
            _vm.UpdatePreview = new RelayCommand(o => RenderUsingDispatcher(), o => _preWindow != null);
            _vm.SaveToken = new RelayCommand(o => MenuItemSave_OnClick(null, null));
            _vm.LoadToken = new RelayCommand(o => MenuItemLoad_OnClick(null, null));
            _vm.UpdateZoomSpeed = new RelayCommand(o =>
            {
                _vm.ZoomSpeed = o.ToString();
                var param = Convert.ToDecimal(o.ToString(), CultureInfo.InvariantCulture);
                ZoomControl.ZoomSpeed = Convert.ToDouble(param);
            });
            _vm.PropertyChanged += (sender, args) =>
            {
                //When a new image is loaded, update the preview
                if(args.PropertyName == nameof(_vm.Image) && _vm.AutoUpdatePreview) RenderUsingDispatcher();
            };

            _vm.Image = BitmapHelper.ConvertBitmapToImageSource(DataAccess.Properties.Resources.Splash);

            //Timer for mousewheel events.
            _timer = new DispatcherTimer();
            _timer.Tick += TimerTicked;
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            
            //Load settings-values
            _vm.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(SettingsManager.StartupTokenWidth, SettingsManager.StartupTokenHeight, SettingsManager.StartupFitmode));
            _vm.AutoUpdatePreview = SettingsManager.StartupAutoUpdatePreview;


            CheckIfUpdateAvailable(false);
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
        private void RenderUsingDispatcher()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => RenderImage()));
        }

        private void OpenPreviewWindow(object sender, RoutedEventArgs e)
        {
            _preWindow?.Close();

            var sidemargin = 20;
            var topmargin = 30;

            _preWindow = new PreviewWindow(new PreviewWindowViewModel());
            _preWindow.Height = Math.Max(164, _vm.ImageResolutionHeight) + topmargin * 2;
            _preWindow.Width = Math.Max(164, _vm.ImageResolutionWidth) + sidemargin * 2;
            _preWindow.Closed += (o, args) =>
            {
                _preWindow = null;
                _vm.OpenPreviewWindow.OnCanExecuteChanged(sender);
                _vm.UpdatePreview.OnCanExecuteChanged(sender);
            };
            _preWindow.Show();

            _vm.OpenPreviewWindow.OnCanExecuteChanged(sender);
            _vm.UpdatePreview.OnCanExecuteChanged(sender);
            RenderUsingDispatcher();
        }

        private Bitmap RenderVisual(Visual element)
        {
            //Setting image offset and size.
            var offsetFromTopLeft = new Point(ZoomControl.ActualWidth / 2 - RenderLocation.ActualWidth / 2, ZoomControl.ActualHeight / 2 - RenderLocation.ActualHeight / 2);
            var imageSize = new Size(_vm.ImageResolutionWidth, _vm.ImageResolutionHeight);

            //Rendering part of visual.
            var brush = new VisualBrush(element)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(offsetFromTopLeft.X, offsetFromTopLeft.Y, imageSize.Width, imageSize.Height),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(new Point(0, 0), imageSize)
            };

            var renderTarget = new Rectangle { Width = imageSize.Width, Height = imageSize.Height, Fill = brush };
            renderTarget.Measure(imageSize);
            renderTarget.Arrange(new Rect(0, 0, imageSize.Width, imageSize.Height));

            var render = new RenderTargetBitmap((int)imageSize.Width, (int)imageSize.Height, 96, 96, PixelFormats.Pbgra32);
            render.Render(renderTarget);


            using (var ms = new MemoryStream())
            {
                var enc = new PngBitmapEncoder();
                var bitmapFrame = BitmapFrame.Create(render);
                enc.Frames.Add(bitmapFrame);
                enc.Save(ms);
                return BitmapHelper.ConvertToPixelFormat_32bppArgb(new Bitmap(ms));
            }
        }

        private Bitmap RenderImage()
        {
            Bitmap bitmap = RenderVisual(ZoomControl);

            //Apply the special filter.
            BitmapHelper.AddFilter(bitmap, _vm.SpecialFilterColor, _specialFilter);

            // Modify the rendered image.
            if (_overlayInfo != null)
            {
                var overlay = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight);
                BitmapHelper.AddFilter(overlay, _overlayTintColor, _overlayBlendFilter);
                if (!string.IsNullOrWhiteSpace(_overlayInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(overlay, ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.Mask, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight));
                BitmapHelper.AddLayerToImage(bitmap, overlay);
            }

            if (_borderInfo != null)
            {
                if (!string.IsNullOrWhiteSpace(_borderInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(bitmap, ImageLoader.LoadBitmapFromFile(_borderInfo.Info.Mask, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight));
                var border = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight);
                BitmapHelper.AddFilter(border, _borderTintColor, _borderBlendFilter);
                BitmapHelper.AddLayerToImage(bitmap, border);  //Draw the border
            }

            if (ZoomControl_Text.Visibility == Visibility.Visible)
            {
                Bitmap text = RenderVisual(ZoomControl_Text);
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

        private void UpdateOverlays()
        {
            //When updating overlays, the actual output resolution may be different from the desired resolution if stretching of overlays isn't allowed.
            if (_borderInfo != null)
            {
                var border = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File);
                _vm.SetDimensions(_vm.FitMode, _vm.DesiredResolutionWidth, _vm.DesiredResolutionHeight, border.Width, border.Height);
            }
            else if (_overlayInfo != null)
            {
                var overlay = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File);
                _vm.SetDimensions(_vm.FitMode, _vm.DesiredResolutionWidth, _vm.DesiredResolutionHeight, overlay.Width, overlay.Height);
            }

            if (_borderInfo != null)
            {
                //Border
                var borderImage = ImageLoader.LoadBitmapFromFile(_borderInfo.Info.File, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight);
                BitmapHelper.AddFilter(borderImage, _borderTintColor, _borderBlendFilter);

                _vm.BorderImage = BitmapHelper.ConvertBitmapToImageSource(borderImage);
                BorderImage.Height = _vm.ImageResolutionHeight;
                BorderImage.Width = _vm.ImageResolutionWidth;
            }

            if (_overlayInfo != null)
            {
                //Overlay
                var overlayImage = ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.File, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight);
                BitmapHelper.AddFilter(overlayImage, _overlayTintColor, _overlayBlendFilter);
                if (!string.IsNullOrWhiteSpace(_overlayInfo.Info.Mask)) BitmapHelper.ApplyMaskToImage(overlayImage, ImageLoader.LoadBitmapFromFile(_overlayInfo.Info.Mask, _vm.ImageResolutionWidth, _vm.ImageResolutionHeight));

                _vm.OverlayImage = BitmapHelper.ConvertBitmapToImageSource(overlayImage);
                OverlayImage.Height = _vm.ImageResolutionHeight;
                OverlayImage.Width = _vm.ImageResolutionWidth;
            }

            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
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
            var bc = sender as BorderControl;
            var color = bc.SelectedColor;
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
            var oc = sender as OverlayControl;
            var color = oc.SelectedColor;
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
            int num;
            if (int.TryParse((sender as SpecialControl)._vm.RotationAngle, out num))
            {
                _vm.RotationAngle = num;
            }
            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnTextManipulationChanged(object sender, RoutedEventArgs e)
        {
            _vm.ShowTextBorder = (sender as SpecialControl)._vm.TextManipulationShowBorder;
            _vm.ShowText = (sender as SpecialControl)._vm.TextManipulationShowText ? Visibility.Visible : Visibility.Collapsed;
            _vm.TextFont = (sender as SpecialControl).FontBox.SelectedItem as System.Windows.Media.FontFamily;
            _vm.TextColor = new SolidColorBrush((sender as SpecialControl)._vm.TextColor);

            //Convert \n to an actual newline
            var text = (sender as SpecialControl)._vm.TextContent.ToCharArray();
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
            _vm.TextContent = finalText.ToString();


            int num;
            if (int.TryParse((sender as SpecialControl)._vm.TextRotationAngle, out num))
            {
                _vm.TextRotationAngle = num;
            }

            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnButtonZoom(object sender, RoutedEventArgs e)
        {
            var e1 = (ButtonZoomEvent)e;
            switch (e1.Target)
            {
                case "Image":
                    ZoomControl.ManualZoom(e1);
                    break;
                case "Text":
                    ZoomControl_Text.ManualZoom(e1);
                    break;
                default:
                    throw new ArgumentException();
            }

            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnBackdropColorChanged(object sender, RoutedEventArgs e)
        {
            _vm.BackdropColor = new SolidColorBrush((sender as SpecialControl)._vm.BackdropColor);

            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void SpecialControl_OnSpecialFilterColorChanged(object sender, RoutedEventArgs e)
        {
            var color = (sender as SpecialControl)._vm.SpecialFilterColor;
            _vm.SpecialFilterColor = Color.FromArgb(color.A, color.R, color.G, color.B);

            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
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
                _vm.LoadExternalImage(dialog.FileName, ExternalImageType.LocalFile);
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
            var win = new CustomSizeWindow(_vm.FitMode)
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
                        _vm.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(width, height, ImageLoader.FitMode.Stretch));
                        return;
                    }

                    if (win.Fill.IsChecked.HasValue && win.Fill.IsChecked.Value)
                    {
                        _vm.UpdateResolution.Execute(new Tuple<int, int, ImageLoader.FitMode>(width, height, ImageLoader.FitMode.Fill));
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
        }
        #endregion

        #region ZoomBorder
        private void ZoomControl_OnDrop(object sender, DragEventArgs e)
        {
            // Edge or Firefox will save the image locally and pass it as a local file. Chrome gives you a Html-fragment
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    _vm.LoadExternalImage(((string[])e.Data.GetData(DataFormats.FileDrop))[0], ExternalImageType.LocalFile);
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show(this, "File couldn't be loaded");
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Html))
            {
                var regex = new Regex("<!--StartFragment--><img\\s.*src=\"(?<source>.*?)\".*<!--EndFragment-->");
                var match = regex.Match(e.Data.GetData(DataFormats.Html).ToString());
                if (match.Success)
                {
                    var imagesource = match.Groups["source"].Value;

                    try
                    {
                        _vm.LoadExternalImage(imagesource, ExternalImageType.WebContent);
                    }
                    catch (NotSupportedException)
                    {
                        MessageBox.Show(this, "Input couldn't be loaded");
                    }
                }
            }
        }

        private void ZoomControl_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_vm.AutoUpdatePreview) RenderUsingDispatcher();
        }

        private void ZoomControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_vm.AutoUpdatePreview)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        private void TimerTicked(object sender, EventArgs eventArgs)
        {
            var dt = sender as DispatcherTimer;
            dt.Stop();
            RenderUsingDispatcher();
        }

        private void ZoomControl_Text_OnRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            _vm.TextRotationAngle += arg.AngleDelta;
            SpecialControl._vm.TextRotationAngle = _vm.TextRotationAngle.ToString();
        }

        private void ZoomControl_OnRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            _vm.RotationAngle += arg.AngleDelta;
            SpecialControl._vm.RotationAngle = _vm.RotationAngle.ToString();
        }
        #endregion
    }
}
