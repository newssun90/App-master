using BarcodeInspection.Helpers;
using BarcodeInspection.Models.Common;
using BarcodeInspection.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace BarcodeInspection
{
    public partial class App : Application
    {
        public App()
        {

            // Initialize Live Reload.
#if DEBUG
            LiveReload.Init();
#endif
            InitializeComponent();

            //MainPage = new MainPage();
            //MainPage = new NavigationPage(new MainPage());
            MainPage = new NavigationPage(new Views.Outbound.LOBSM030View());
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            if (Application.Current.Properties.ContainsKey("SupportBarcodeFormat"))
            {

            }
            else
            {
                //string jsonList = Application.Current.Properties["SupportBarcodeFormat"].ToString();
                //List<BarcodeFormat> tempList = JsonConvert.DeserializeObject<List<BarcodeFormat>>(jsonList);
                List<BarcodeFormat> tempList = new List<BarcodeFormat>();
                tempList.Add(BarcodeFormat.Code128);
                tempList.Add(BarcodeFormat.Code39);
                tempList.Add(BarcodeFormat.Code93);
                tempList.Add(BarcodeFormat.Codabar);
                tempList.Add(BarcodeFormat.DataMatrix);
                tempList.Add(BarcodeFormat.Ean13);
                tempList.Add(BarcodeFormat.Ean8);
                tempList.Add(BarcodeFormat.Itf);
                tempList.Add(BarcodeFormat.QrCode);
                tempList.Add(BarcodeFormat.UpcA);
                tempList.Add(BarcodeFormat.UpcE);
                tempList.Add(BarcodeFormat.Pdf417);
                tempList.Add(BarcodeFormat.AztecCode);
                tempList.Add(BarcodeFormat.Itf14);

                Application.Current.Properties["SupportBarcodeFormat"] = JsonConvert.SerializeObject(tempList);
            }

            //ToDo
            //로그인 테스트
            var p = new LoginUser { Compky = "A001", Wareky = "10", Userid = "90773532", Passwd = "3532" };
            string jsonString = JsonConvert.SerializeObject(p);

            string result = await BaseHttpService.Instance.AuthorizationAsync(jsonString);

            LoginUser loginUser = JsonConvert.DeserializeObject<LoginUser>(result);
            //System.Diagnostics.Debug.WriteLine(loginUser.Auth_Token);

            //Settings.Token = loginUser.Auth_Token;

            //Debug.WriteLine(Settings.Token);

        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

