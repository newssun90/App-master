using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection
{
    public class GlobalSetting
    {
        //GITHUB테스트
        //git서버에 반영하려면 Command & Push
        private string DefaultEndpoint = "http://wms.dsbestco.co.kr";

        private string _baseEndpoint = string.Empty;
        private static readonly GlobalSetting _instance = new GlobalSetting();

        public GlobalSetting()
        {
#if DEBUG
            DefaultEndpoint = "http://192.168.42.111:8080";
#endif
            AuthToken = "INSERT AUTHENTICATION TOKEN";
            BaseEndpoint = DefaultEndpoint;
        }

        public static GlobalSetting Instance
        {
            get { return _instance; }
        }

        public string BaseEndpoint
        {
            get { return _baseEndpoint; }
            set
            {
                _baseEndpoint = value;
                UpdateEndpoint(_baseEndpoint);
            }
        }
        public string AuthToken { get; set; }

        public string ClientId { get { return "BarcodeInspection"; } }

        public string BCWMSEndpoint { get; set; }

        public string MOBILEEndpoint { get; set; }

        public string MobileGetEndpoint { get; set; }
        public string MobileSetEndpoint { get; set; }

        public string MobileAuthEndpoint { get; set; }

        private void UpdateEndpoint(string baseEndpoint)
        {
            MobileAuthEndpoint = $"{baseEndpoint}/xamarin/AuthorizationServer";
            MOBILEEndpoint = $"{baseEndpoint}/bcwms/mobile";
            MobileGetEndpoint = $"{baseEndpoint}/bcwms/MobileGet";
            MobileSetEndpoint = $"{baseEndpoint}/bcwms/MobileSet";
        }
    }
}
