using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace BarcodeInspection.iOS.Extensions
{
    using CoreText;
    using UIKit;

    public static class FontExtensions
    {
        public static CTFont ToCTFont(this UIFont font)
        {
            return new CTFont(font.Name, font.PointSize);
        }
    }
}