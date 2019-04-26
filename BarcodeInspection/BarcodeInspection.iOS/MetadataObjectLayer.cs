using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace BarcodeInspection.iOS
{
    using AVFoundation;
    using CoreAnimation;

    public class MetadataObjectLayer : CAShapeLayer
    {
        public AVMetadataObject MetadataObject { get; set; }
    }
}