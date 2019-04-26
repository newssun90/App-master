using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BarcodeInspection.iOS;
using BarcodeInspection.Views.Common;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ScanBarcodeView), typeof(ScanPageRenderer))]
namespace BarcodeInspection.iOS
{
    public class ScanPageRenderer : PageRenderer
    {
        public CameraViewController viewController;

        List<string> AllScanBarcode = new List<string>(); //스캔할 전체 바코드 리스트
        List<string> ScanCompletedBarcode = new List<string>(); //스캔 완료 한 바코드
        List<string> SaveCompletedBarcode = new List<string>(); //부분 저장해서 완료된 바코드
        bool IsContinue; //연속스캔 해야 하는가?
        bool IsFixed; //스캔할 바코드가 지정되어 있는가?

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            AllScanBarcode = ((ScanBarcodeView)Element).AllScanBarcode;
            ScanCompletedBarcode = ((ScanBarcodeView)Element).ScanCompletedBarcode;
            SaveCompletedBarcode = ((ScanBarcodeView)Element).SaveCompletedBarcode;
            IsContinue = ((ScanBarcodeView)Element).IsContinue;
            IsFixed = ((ScanBarcodeView)Element).IsFixed;

            try
            {
                viewController = new CameraViewController(AllScanBarcode, ScanCompletedBarcode, SaveCompletedBarcode, IsContinue, IsFixed);
                viewController.OnScanCompleted += async (string resultType, string resultText) =>
                {
                    if (!resultText.Equals("EXIT"))
                    {
                        if (!string.IsNullOrEmpty(resultText))
                        {
                            if (Element != null)
                            {
                                Console.WriteLine(resultType);
                                ((ScanBarcodeView)Element).ScanReceive(resultType, resultText);
                            }
                        }
                    }

                    if (!IsContinue || resultText.Equals("EXIT"))
                    {
                        if (Element != null)
                        {
                            ((ScanBarcodeView)Element).ScanCompleted();
                            await Element.Navigation.PopAsync();
                        }
                    }
                };

                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController((UIViewController)viewController, true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"ERROR: ", ex.Message);
            }
        }
    }
}