using Plugin.Settings;
using Plugin.Settings.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection.Helpers
{
    public class Settings
    {
        private static ISettings AppSettings
        {
            get
            {
                return CrossSettings.Current;
            }
        }

        #region Setting Constants
        private const string IdWareky = "wareky";
        private static readonly string IdWarekyDefault = string.Empty;

        private const string IdUserid = "userid";
        private static readonly string IdUseridDefault = string.Empty;

        private const string IdVersion = "version";
        private static readonly string IdVersionDefault = string.Empty;

        private const string IdSerial = "serial";
        private static readonly string IdSerialDefault = string.Empty;

        private const string IdToken = "token";
        private static readonly string IdTokenDefault = string.Empty;

        private const string IdScanMode = "scanmode";
        private static readonly string IdScanModeDefault = "WIDE";
        #endregion

        public static string Wareky
        {
            get => AppSettings.GetValueOrDefault(IdWareky, IdWarekyDefault);
            set => AppSettings.AddOrUpdateValue(IdWareky, value);
        }

        public static string Userid
        {
            get => AppSettings.GetValueOrDefault(IdUserid, IdUseridDefault);
            set => AppSettings.AddOrUpdateValue(IdUserid, value);
        }

        public static string Version
        {
            get => AppSettings.GetValueOrDefault(IdVersion, IdVersionDefault);
            set => AppSettings.AddOrUpdateValue(IdVersion, value);
        }

        public static string Token
        {
            get => AppSettings.GetValueOrDefault(IdToken, IdTokenDefault);
            set => AppSettings.AddOrUpdateValue(IdToken, value);
        }

        public static string ScanMode
        {
            get => AppSettings.GetValueOrDefault(IdScanMode, IdScanModeDefault);
            set => AppSettings.AddOrUpdateValue(IdScanMode, value);
        }
    }
}
