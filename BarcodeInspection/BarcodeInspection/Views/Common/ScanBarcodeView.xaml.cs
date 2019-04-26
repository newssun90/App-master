using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BarcodeInspection.Views.Common
{
    public delegate void ScanResultDelegate(List<string> scanList);

    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ScanBarcodeView : ContentPage
	{

        public bool IsContinue; //연속스캔 해야 하는가?
        public bool IsFixed = false; //스캔해야 할 바코드가 지정되어 있는가?
        public bool IsInterestArea = false; //카메라 포커스 관심 영역 표시
        public List<string> AllScanBarcode = new List<string>(); //스캔해야 할 전체 바코드
        public List<string> ScanCompletedBarcode = new List<string>(); //스캔 완료된 바코드
        public List<string> SaveCompletedBarcode = new List<string>(); //부분 스캔해서 저장 완료된 바코드, Scan하고 나서 메인화면으로 갔다가 저장하지 않고 다시 스캔화면으로 갈경우 구분해야 함.
        public List<string> ScandedBarcode = new List<string>(); //현재 화면에서 스캔완료 한 바코드
        public event ScanResultDelegate OnScanCompleted;

        public ScanBarcodeView (bool isContinue, bool isFixed, List<string> allScanBarcode, List<string> scanCompletedBarcode, List<string> saveCompletedBarcode)
		{
			InitializeComponent ();

            this.IsContinue = isContinue;
            this.IsFixed = isFixed;
            this.AllScanBarcode = allScanBarcode;
            this.ScanCompletedBarcode = scanCompletedBarcode;
            this.SaveCompletedBarcode = saveCompletedBarcode;
        }

        public ScanBarcodeView(bool isContinue, bool isFixed, List<string> allScanBarcode, List<string> scanCompletedBarcode)
        {
            InitializeComponent();

            this.IsContinue = isContinue;
            this.IsFixed = isFixed;
            this.AllScanBarcode = allScanBarcode;
            this.ScanCompletedBarcode = scanCompletedBarcode;
        }

        public ScanBarcodeView(bool isContinue)
        {
            InitializeComponent();

            this.IsContinue = isContinue;
        }

        public void ScanCompleted()
        {
            OnScanCompleted?.Invoke(ScandedBarcode);
        }

        public void ScanReceive(string barcodeFormat, string barcodeResult)
        {
            Debug.WriteLine(string.Format("ScanReceive Format : {0}, Result : {1}", barcodeFormat, barcodeResult));

            if (!string.IsNullOrEmpty(barcodeResult) && !ScandedBarcode.Contains(barcodeResult))
            {
                ScandedBarcode.Add(barcodeResult);
            }

            //if(!IsContinue)
            //{
            //    ScanResult();
            //}
        }
    }
}