using System.Windows;
using System.Windows.Input;

namespace Stamper.UI.ViewModels
{
    public static class SharedEventHandlingLogic
    {
        public static void InputValidation_ContrainToInt(object sender, TextCompositionEventArgs e)
        {
            int num;
            e.Handled = !int.TryParse(e.Text, out num);
        }

        public static void InputValidationOnPaste_ContrainToInt(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                int num;
                if (!int.TryParse((string)e.DataObject.GetData(typeof(string)), out num))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
