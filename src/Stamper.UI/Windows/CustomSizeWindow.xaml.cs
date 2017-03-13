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
using Stamper.UI.ViewModels;

namespace Stamper.UI.Windows
{
    public partial class CustomSizeWindow : Window
    {
        public bool OkClicked = false;

        public CustomSizeWindow()
        {
            InitializeComponent();
        }

        private void InputValidation(object sender, TextCompositionEventArgs e)
        {
            SharedEventHandlingLogic.InputValidation_ContrainToInt(sender, e);
        }

        private void InputValidationOnPaste(object sender, DataObjectPastingEventArgs e)
        {
            SharedEventHandlingLogic.InputValidationOnPaste_ContrainToInt(sender, e);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            OkClicked = true;
            Close();
        }
    }
}
