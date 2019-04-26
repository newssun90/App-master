using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection.Helpers
{
    public enum BarcodeFormat
    {
        Code128 = 1,
        Code39 = 2,
        Code93 = 4,
        Codabar = 8,
        DataMatrix = 16,
        Ean13 = 32,
        Ean8 = 64,
        Itf = 128,
        QrCode = 256,
        UpcA = 512, //iOS지원 안함
        UpcE = 1024,
        Pdf417 = 2048,
        AztecCode = 4096,
        Itf14 = 8192
    }

    public enum ScanMode
    {
        Split,
        Wide,
        Full
    }
}
