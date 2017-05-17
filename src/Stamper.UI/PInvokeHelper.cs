using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Stamper.UI
{
    public static class PInvokeHelper
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000; //maximize button
        private const int WS_MINIMIZEBOX = 0x20000; //minimize button
        private const int WS_SYSMENU =     0x80000; //close button

        /// <summary>
        /// Disables the maximize button for a specific window.
        /// Call this method in the window constructor.
        /// </summary>
        public static void DisableMaximizeButton(Window window)
        {
            window.SourceInitialized += (sender, args) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("The window has not been completely initialized yet.");
                }

                SetWindowLong(handle, GWL_STYLE, GetWindowLong(handle, GWL_STYLE) & ~WS_MAXIMIZEBOX);
            };
        }

        /// <summary>
        /// Disables the minimize button for a specific window.
        /// Call this method in the window constructor.
        /// </summary>
        public static void DisableMinimizeButton(Window window)
        {
            window.SourceInitialized += (sender, args) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("The window has not been completely initialized yet.");
                }

                SetWindowLong(handle, GWL_STYLE, GetWindowLong(handle, GWL_STYLE) & ~WS_MINIMIZEBOX);
            };
        }
        
        /// <summary>
        /// Disables the minimize, maximize and close buttons for a specific window, as well
        /// as the program icon.
        /// 
        /// Call this method in the window constructor.
        /// </summary>
        public static void DisableCloseButton(Window window)
        {
            window.SourceInitialized += (sender, args) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("The window has not been completely initialized yet.");
                }

                SetWindowLong(handle, GWL_STYLE, GetWindowLong(handle, GWL_STYLE) & ~WS_SYSMENU);
            };
        }
    }
}
