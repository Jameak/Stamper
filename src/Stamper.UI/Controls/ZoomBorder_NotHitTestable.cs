using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Controls.PanAndZoom;

namespace Stamper.UI.Controls
{
    /// <summary>
    /// This ZoomBorder is not HitTest-able, but child elements are still HitTest-able.
    /// </summary>
    public class ZoomBorder_NotHitTestable : CustomZoomBorder
    {
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return null;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }
    }
}
