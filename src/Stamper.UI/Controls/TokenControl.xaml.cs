using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Stamper.DataAccess;
using Stamper.UI.Events;
using Stamper.UI.ViewModels;
using Stamper.UI.ViewModels.Base;
using Stamper.UI.ViewModels.Enums;
using Stamper.UI.Windows;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace Stamper.UI.Controls
{
    public partial class TokenControl : UserControl
    {
        private TokenControlViewModel _vm;
        private readonly DispatcherTimer _timer;

        public int ImageResolutionWidth => _vm.ImageResolutionWidth;
        public int ImageResolutionHeight => _vm.ImageResolutionHeight;
        public ICommand UpdateResolution => _vm.UpdateResolution;
        public ICommand LoadExternalImageCommand => _vm.LoadExternalImageCommand;
        public ImageLoader.FitMode FitMode => _vm.FitMode;
        
        public int RotationAngle { set => _vm.RotationAngle = value; }
        public SolidColorBrush TextColor { set => _vm.TextColor = value; }
        public bool ShowTextBorder { set => _vm.ShowTextBorder = value; }
        public Visibility ShowText { set => _vm.ShowText = value; }
        public FontFamily TextFont { set => _vm.TextFont = value; }
        public string TextContent { set => _vm.TextContent = value; }
        public int TextRotationAngle { set => _vm.TextRotationAngle = value; }
        public SolidColorBrush BackdropColor { set => _vm.BackdropColor = value; }


        public TokenControl()
        {
            InitializeComponent();
            _vm = new TokenControlViewModel();
            DataContext = _vm;
            
            _vm.UpdateResolution = new RelayCommand(param =>
            {
                if (param != null)
                {
                    if (param is Tuple<int, int, ImageLoader.FitMode>)
                    {
                        var tuple = (Tuple<int, int, ImageLoader.FitMode>)param;
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
                GetWindow().UpdateOverlays();
            });

            _vm.LoadExternalImageCommand = new RelayCommand(o =>
            {
                var args = o as Tuple<string, ExternalImageType>;
                _vm.LoadExternalImage(args.Item1, args.Item2);
            });


            _vm.PropertyChanged += (sender, args) =>
            {
                //When a new image is loaded, update the preview
                if (args.PropertyName == nameof(_vm.Image))
                {
                    RaiseEvent(new RoutedEventArgs(ImageChangedEvent));
                }
            };

            //Timer for mousewheel events.
            _timer = new DispatcherTimer();
            _timer.Tick += TimerTicked;
            _timer.Interval = TimeSpan.FromMilliseconds(200);
        }

        private MainWindow GetWindow()
        {
            var window = Window.GetWindow(this) as MainWindow;
            if (window == null)
            {
                throw new ApplicationException("This control only works when inside of a window of type MainWindow");
            }
            return window;
        }

        public Bitmap RenderVisual(Visual element)
        {
            var dpi = VisualTreeHelper.GetDpi(element);
            //Reverse the dpi-scaling applied to the size of the RenderLocation control.
            var unscaledRenderLocation = new Point(RenderLocation.ActualWidth / dpi.DpiScaleX, RenderLocation.ActualHeight / dpi.DpiScaleY);

            //Setting image offset and size.
            var offsetFromTopLeft = new Point((ZoomControl.ActualWidth / 2 - unscaledRenderLocation.X / 2), (ZoomControl.ActualHeight / 2 - unscaledRenderLocation.Y / 2));
            var imageSize = new Size(ImageResolutionWidth, ImageResolutionHeight);
            
            //Rendering part of visual.
            var brush = new VisualBrush(element)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(offsetFromTopLeft.X, offsetFromTopLeft.Y, imageSize.Width, imageSize.Height),
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                Viewport = new Rect(new Point(0, 0), new Point(1,1))
            };

            var renderTarget = new Rectangle { Width = imageSize.Width, Height = imageSize.Height, Fill = brush };
            renderTarget.Measure(imageSize);
            renderTarget.Arrange(new Rect(0, 0, imageSize.Width, imageSize.Height));
            
            var render = new RenderTargetBitmap(ImageResolutionWidth, ImageResolutionHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
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

        public void ResetControls()
        {
            ZoomControl.Reset();
            ZoomControl_Text.Reset();
        }

        public void SetZoomSpeed(string zoomSpeed)
        {
            _vm.ZoomSpeed = zoomSpeed;
            var param = Convert.ToDecimal(zoomSpeed, CultureInfo.InvariantCulture);
            ZoomControl.ZoomSpeed = Convert.ToDouble(param);
        }

        public void SetImage(Bitmap image)
        {
            _vm.Image = BitmapHelper.ConvertBitmapToImageSource(image);
        }

        public void SetBorderImage(Bitmap image)
        {
            _vm.BorderImage = BitmapHelper.ConvertBitmapToImageSource(image);
        }

        public void SetOverlayImage(Bitmap image)
        {
            _vm.OverlayImage = BitmapHelper.ConvertBitmapToImageSource(image);
        }

        public void SetDimensions(int width, int height)
        {
            _vm.SetDimensions(_vm.FitMode, _vm.DesiredResolutionWidth, _vm.DesiredResolutionHeight, width, height);
        }

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
                    MessageBox.Show(GetWindow(), "File couldn't be loaded");
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
                        MessageBox.Show(GetWindow(), "Input couldn't be loaded");
                    }
                }
            }
        }

        private void ZoomControl_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SettingsManager.AutoUpdatePreview) GetWindow().RenderUsingDispatcher();
        }

        private void ZoomControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (SettingsManager.AutoUpdatePreview)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        private void TimerTicked(object sender, EventArgs eventArgs)
        {
            var dt = sender as DispatcherTimer;
            dt.Stop();
            GetWindow().RenderUsingDispatcher();
        }

        private void ZoomControl_Text_OnRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            _vm.TextRotationAngle += arg.AngleDelta;
            RaiseEvent(new RotationEvent(TextRotationEvent, arg.AngleDelta));
        }

        private void ZoomControl_OnRotation(object sender, RoutedEventArgs e)
        {
            var arg = (RotationEvent)e;
            _vm.RotationAngle += arg.AngleDelta;
            RaiseEvent(new RotationEvent(RotationEvent, arg.AngleDelta));
        }
        #endregion

        #region Events
        public static readonly RoutedEvent RotationEvent =
            EventManager.RegisterRoutedEvent(nameof(RotationEvent), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TokenControl));

        public event RoutedEventHandler Rotation
        {
            add { AddHandler(RotationEvent, value); }
            remove { RemoveHandler(RotationEvent, value); }
        }

        public static readonly RoutedEvent TextRotationEvent =
            EventManager.RegisterRoutedEvent(nameof(TextRotationEvent), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TokenControl));

        public event RoutedEventHandler TextRotation
        {
            add { AddHandler(TextRotationEvent, value); }
            remove { RemoveHandler(TextRotationEvent, value); }
        }
        
        public static readonly RoutedEvent ImageChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ImageChangedEvent), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TokenControl));

        public event RoutedEventHandler ImageChanged
        {
            add { AddHandler(ImageChangedEvent, value); }
            remove { RemoveHandler(ImageChangedEvent, value); }
        }
        #endregion
    }
}
