using BarcodeInspection.Views.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BarcodeInspection.Services
{
    public class VersionCheck
    {
        private static readonly VersionCheck instance = new VersionCheck();
        Version versionServer;
        Version versionClient;

        string url = string.Format(@"{0}{1}", GlobalSetting.Instance.MOBILEEndpoint.ToString(), @"/version.html");

        private VersionCheck()
        {
            GetVersionClient();
        }

        public static VersionCheck Instance
        {
            get
            {
                return instance;
            }
        }

        public bool IsNetworkAccess()
        {
            var NetworkAccess = Connectivity.NetworkAccess;

            if (NetworkAccess == NetworkAccess.Internet)
            {
                return true;
            }
            else
            {
                return false;
            }

            //#if DEBUG
            //            if (profiles.Contains(ConnectionProfile.WiFi))
            //            {
            //                return true;
            //            }
            //            else
            //            {
            //                await Application.Current.MainPage.DisplayAlert("WiFi 연결 오류", "WiFi 연결 후\n다시 처리해 주세요.", "OK");

            //                // Active Wi-Fi connection.
            //                return false;
            //            }
            //#else
            //            if (profiles.Contains(ConnectionProfile.Cellular)) //라이브
            //            {
            //                return true;
            //            }
            //            else
            //            {
            //                await Application.Current.MainPage.DisplayAlert("인터넷 연결 오류", "LTE 연결 확인\n다시 처리해 주세요.", "OK");
            //                return false;
            //            }
            //#endif

            /*
            if (profiles.Contains(ConnectionProfile.WiFi))
            {
                // Active Wi-Fi connection.
                return true;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("WiFi 연결 오류", "WiFi 연결 후\n다시 처리해 주세요.", "OK");

                // Active Wi-Fi connection.
                return false;
            }
            */
        }

        public async Task<bool> IsUpdate()
        {
            await GetVersionServer();

            if (versionServer > versionClient)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task UpdateCheck()
        {
            //if (!Connectivity.Profiles.Contains(ConnectionProfile.WiFi))
            //{
            //    await Application.Current.MainPage.DisplayAlert("Alert", "Wifi 연결 오류\n다시 처리해 주세요.", "OK");

            //    // Active Wi-Fi connection.
            //    return;
            //}

            if (versionServer == null)
            {
                await GetVersionServer();
            }

            if (versionServer > versionClient)
            {
                AutoUpdateView autoUpdateView = new AutoUpdateView();
                await Application.Current.MainPage.Navigation.PushModalAsync(autoUpdateView);
            }
        }

        /// <summary>
        /// Client 버전 확인
        /// </summary>
        /// <returns></returns>
        private Version GetVersionClient()
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var assemblyName = new AssemblyName(assembly.FullName);
            versionClient = assemblyName.Version;

            return versionClient;
        }

        /// <summary>
        /// Server 버전 확인
        /// </summary>
        /// <returns></returns>
        private async Task<Version> GetVersionServer()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(new Uri(url));

                    if (response.IsSuccessStatusCode)
                    {
                        versionServer = new Version(response.Content.ReadAsStringAsync().Result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return versionServer;
        }
    }
}
