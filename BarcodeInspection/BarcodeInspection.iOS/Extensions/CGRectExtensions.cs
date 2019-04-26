using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace BarcodeInspection.iOS.Extensions
{
    using CoreGraphics;

    public static class CGRectExtensions
    {
        public static CGPoint CornerTopLeft(this CGRect rect)
        {
            return rect.Location;
        }

        public static CGPoint CornerTopRight(this CGRect rect)
        {
            rect.X += rect.Width;
            return rect.Location;
        }

        public static CGPoint CornerBottomRight(this CGRect rect)
        {
            return new CGPoint(rect.GetMaxX(), rect.GetMaxY());
        }

        public static CGPoint CornerBottomLeft(this CGRect rect)
        {
            return new CGPoint(rect.GetMinX(), rect.GetMaxY());
        }
    }
}