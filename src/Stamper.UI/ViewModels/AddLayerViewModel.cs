﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Stamper.DataAccess;
using Stamper.UI.ViewModels.Base;

namespace Stamper.UI.ViewModels
{
    public class AddLayerViewModel : BaseViewModel
    {
        private int _previewSize = 390;
        public int PreviewSize { get { return _previewSize; } set { if (_previewSize != value) { _previewSize = value; OnPropertyChanged(); } } }

        private string _name;
        public string Name { get { return _name; } set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

        private string _file;
        public string File { get { return _file; } set { if (_file != value) { _file = value; OnPropertyChanged(); } } }

        private string _mask;
        public string Mask { get { return _mask; } set { if (_mask != value) { _mask = value; OnPropertyChanged(); } } }

        private MaskTypes _maskType;
        public MaskTypes MaskType { get { return _maskType; } set { if (_maskType != value) { _maskType = value; OnPropertyChanged(); } } }

        private BitmapImage _image;
        public BitmapImage Image { get { return _image; } set { if (!Equals(_image, value)) { _image = value; OnPropertyChanged(); } } }

        public ObservableCollection<Layer.LayerType> LayerTypes { get; set; } = new ObservableCollection<Layer.LayerType>();

        private Visibility _previewVisibility = Visibility.Collapsed;
        public Visibility PreviewVisibility { get { return _previewVisibility; } set { if (_previewVisibility != value) { _previewVisibility = value; OnPropertyChanged(); } } }

        public ICommand SelectFileCommand { get; set; }
        public ICommand SelectMaskCommand { get; set; }
        public ICommand MaskRadioButtonCommand { get; set; }
        
        public Bitmap GeneratedMask { get; private set; }

        public AddLayerViewModel()
        {
            foreach (var layer in Enum.GetValues(typeof(Layer.LayerType)).Cast<Layer.LayerType>())
            {
                LayerTypes.Add(layer);
            }

            Image = BitmapHelper.ConvertBitmapToImageSource(DataAccess.Properties.Resources.LayerPreviewImage);
        }

        //TODO: This only works with local files. Add a way for it to support internet images.
        public bool UpdatePreview(Layer.LayerType layerType)
        {
            var backdrop = new Bitmap(DataAccess.Properties.Resources.LayerPreviewImage, PreviewSize, PreviewSize);

            // If no file has been specified, we dont have anything to show preview-wise
            if (string.IsNullOrWhiteSpace(File))
            {
                Image = BitmapHelper.ConvertBitmapToImageSource(backdrop);
                return true;
            }

            Bitmap image; //Image in its actual size
            var loadedImage = ImageLoader.TryLoadBitmapFromFile(File, out image);
            if (!loadedImage) return false;
            Bitmap imageSmall = BitmapHelper.ConvertToPixelFormat_32bppArgb(new Bitmap(image, PreviewSize, PreviewSize)); //Image scaled to the size of the preview.

            Bitmap mask;
            switch (layerType)
            {
                case Layer.LayerType.Border:
                    switch (MaskType)
                    {
                        case MaskTypes.None:
                            BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                            break;
                        case MaskTypes.User:
                            //If the user hasn't specified a mask, dont attempt to load it. Just show image without a mask.
                            if (string.IsNullOrWhiteSpace(Mask))
                            {
                                BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                                break; 
                            }

                            var loadedMask = ImageLoader.TryLoadBitmapFromFile(Mask, out mask, PreviewSize, PreviewSize);
                            if (!loadedMask) return false;

                            BitmapHelper.ApplyMaskToImage(backdrop, mask); //For Border-layers we apply the mask to the backdrop
                            BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                            break;
                        case MaskTypes.Autogenerated:
                            var generatedMask = BitmapHelper.GenerateMask(image); //Generate mask for full-size image, and then scale it down afterward so we have as much detail as possible
                            var generatedMaskSmall = BitmapHelper.ConvertToPixelFormat_32bppArgb(new Bitmap(generatedMask, PreviewSize, PreviewSize));
                            BitmapHelper.ApplyMaskToImage(backdrop, generatedMaskSmall);
                            BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                            GeneratedMask = generatedMask;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Layer.LayerType.Overlay:
                    switch (MaskType)
                    {
                        case MaskTypes.None:
                            BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                            break;
                        case MaskTypes.User:
                            //If the user hasn't specified a mask, dont attempt to load it. Just show image without a mask.
                            if (string.IsNullOrWhiteSpace(Mask))
                            {
                                BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                                break;
                            }

                            var loadedMask = ImageLoader.TryLoadBitmapFromFile(Mask, out mask, PreviewSize, PreviewSize);
                            if (!loadedMask) return false;

                            BitmapHelper.ApplyMaskToImage(imageSmall, mask); //For Overlay-layers we apply the mask to the overlay
                            BitmapHelper.AddLayerToImage(backdrop, imageSmall);
                            break;
                        case MaskTypes.Autogenerated:
                            throw new InvalidEnumArgumentException("Overlays cant have autogenerated masks");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layerType), layerType, null);
            }
            
            Image = BitmapHelper.ConvertBitmapToImageSource(backdrop);
            return true;
        }

        public void FirePropertyChanged(string property)
        {
            OnPropertyChanged(property);
        }

        public enum MaskTypes
        {
            None, User, Autogenerated
        }
    }
}
