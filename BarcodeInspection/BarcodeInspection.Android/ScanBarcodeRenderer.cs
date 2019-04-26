using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Vision.Barcodes;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BarcodeInspection.Droid;
using BarcodeInspection.Views.Common;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ScanBarcodeView), typeof(ScanBarcodeRenderer))]
namespace BarcodeInspection.Droid
{
    public class ScanBarcodeRenderer : PageRenderer
    {
        List<string> AllScanBarcode; //스캔할 전체 바코드 리스트
        List<string> ScanCompletedBarcode; //스캔 완료 한 바코드
        List<string> SaveCompletedBarcode; //부분 저장해서 완료된 바코드
        bool IsContinue; //연속스캔 해야 하는가?
        bool IsFixed; //스캔할 바코드가 지정되어 있는가?
        bool IsInterestArea; //카메라 스캔 영역 표시여부

        public ScanBarcodeRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            IsContinue = ((ScanBarcodeView)Element).IsContinue;
            IsFixed = ((ScanBarcodeView)Element).IsFixed;
            AllScanBarcode = ((ScanBarcodeView)Element).AllScanBarcode;
            ScanCompletedBarcode = ((ScanBarcodeView)Element).ScanCompletedBarcode;
            SaveCompletedBarcode = ((ScanBarcodeView)Element).SaveCompletedBarcode;

            var activity = this.Context;
            var intent = new Intent(activity, typeof(BarcodeScannerActivity));
            intent.PutExtra("IsContinue", IsContinue);
            intent.PutExtra("IsFixed", IsFixed);
            intent.PutStringArrayListExtra("AllScanBarcode", AllScanBarcode);
            intent.PutStringArrayListExtra("ScanCompletedBarcode", ScanCompletedBarcode);
            intent.PutStringArrayListExtra("SaveCompletedBarcode", SaveCompletedBarcode);

            try
            {
                BarcodeScannerActivity.OnScanCompleted += (Barcode result) =>
                {
                    if (result != null)
                    {
                        //if (result.Format.ToString().Equals("Code128")
                        //|| result.Format.ToString().Equals("Code39")
                        //|| result.Format.ToString().Equals("Code93")
                        //|| result.Format.ToString().Equals("Codabar")
                        //|| result.Format.ToString().Equals("DataMatrix")
                        //|| result.Format.ToString().Equals("Ean13")
                        //|| result.Format.ToString().Equals("Ean8")
                        //|| result.Format.ToString().Equals("Itf")
                        //|| result.Format.ToString().Equals("QrCode")
                        //|| result.Format.ToString().Equals("UpcA")
                        //|| result.Format.ToString().Equals("UpcE")
                        //|| result.Format.ToString().Equals("Pdf417")
                        //)
                        //if (!result.Format.ToString().Equals(string.Empty))
                        if (!result.DisplayValue.Equals("EXIT"))
                        {
                            ((ScanBarcodeView)Element).ScanReceive(result.Format.ToString(), result.DisplayValue);
                        }
                    }

                    if (!IsContinue || result.DisplayValue.Equals("EXIT"))
                    {
                        if (Element != null)
                        {
                            ((ScanBarcodeView)Element).ScanCompleted();
                            Element.Navigation.PopModalAsync();
                        }
                    }
                };

                activity.StartActivity(intent);
            }
            catch (Exception ex)
            {
                MethodBase m = MethodBase.GetCurrentMethod();

                //var properties = new Dictionary<string, string>
                //{
                //    {  m.ReflectedType.Name, m.Name }
                //};
                //Crashes.TrackError(ex, properties);
                //Console.WriteLine(ex.Message);
            }
        }
    }
}