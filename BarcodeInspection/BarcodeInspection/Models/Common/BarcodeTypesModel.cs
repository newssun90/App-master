using BarcodeInspection.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection.Models.Common
{
    public class BarcodeTypesModel
    {
        public BarcodeFormat BarcodeType { get; set; }
        public bool IsSupport { get; set; }
    }
}
