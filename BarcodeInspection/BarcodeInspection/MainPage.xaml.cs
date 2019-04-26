using BarcodeInspection.Views.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BarcodeInspection
{
    public partial class MainPage : ContentPage
    {
        //전체 리스트
        private List<string> AllScanBarcode = new List<string>();
        //스캔 완료 바코드
        private List<string> ScanCompletedBarcode = new List<string>();
        //스캔 완료해서 저장처리한 바코드
        private List<string> SaveCompletedBarcode = new List<string>();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void BarcodeScan_Clicked(object sender, EventArgs e)
        {
            ScanBarcodeView scanBarcode = new ScanBarcodeView(false, true, AllScanBarcode, ScanCompletedBarcode, SaveCompletedBarcode);
            scanBarcode.OnScanCompleted += (List<string> result) =>
            {
                if (result.Count > 0)
                {
                    //LOBL020_ScanCompleted(result);

                    //이렇게 하지 않으면 에러 발생함.
                    //아이폰에서만 에러
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var item in result)
                        {
                            Debug.WriteLine(item.ToString());
                        }
                    });
                }
            };


            if (Device.RuntimePlatform == Device.iOS)
            {
                await Application.Current.MainPage.Navigation.PushAsync(scanBarcode);
            }
            else if (Device.RuntimePlatform == Device.Android)
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(scanBarcode);
            }
        }

        private async void BarcodeType_Clicked(object sender, EventArgs e)
        {            
            await Navigation.PushAsync(new BarcodeTypesView());
        }
    }
}
