using BarcodeInspection.Controls;
using BarcodeInspection.Models.Outbound;
using BarcodeInspection.Services;
using BarcodeInspection.Views.Common;
using DevExpress.Mobile.DataGrid;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace BarcodeInspection.ViewModels.Outbound
{
    public class LOBSM040ViewModel : ViewModelBase
    {
        private GridControl _gridControl;

        private ObservableRangeCollection<LOBSM040Model> _searchResult = new ObservableRangeCollection<LOBSM040Model>();
        private List<LOBSM040Model> _listSearchResult = new List<LOBSM040Model>();
        private List<LOBSM030Model> _lobsm030Models = new List<LOBSM030Model>();
        private string _tranName = string.Empty;
        private bool _isTranToggle = false;
        private int _rowTotal = 0;

        public ICommand SwitchToggledCommand { get; }
        public ICommand BarcodeScanCommand { get; }
        public ICommand PullToRefreshCommand { get; }
        public ICommand SaveLOBSM040Command { get; }

        //전체 리스트
        private List<string> AllScanBarcode = new List<string>();
        //스캔 완료 바코드
        private List<string> ScanCompletedBarcode = new List<string>();
        //스캔 완료해서 저장처리한 바코드
        private List<string> SaveCompletedBarcode = new List<string>();

        public LOBSM040ViewModel()
        {
            TranName = "미완료";

            SwitchToggledCommand = new Command<ToggledEventArgs>(async (e) => await SwitchToggled(e));
            BarcodeScanCommand = new Command(async () => await BarcodeScan());
            PullToRefreshCommand = new Command(async () => await PullToRefresh());
            SaveLOBSM040Command = new Command(async () => await SaveLOBSM040());

        }

        private async Task BarcodeScan()
        {
            if (IsTranToggle)
            {
                return;
            }

            if (!IsEnabled)
            {
                return;
            }

            //화면에 리스트가 없으면 카메라 진입하지 못함.
            if (this.SearchResult.Count == 0)
            {
                return;
            }

            IsBusy = true;
            IsEnabled = false;

            //this.Pltid = string.Empty;
            this.Grid.SelectedRowHandle = -1;

            ScanBarcodeView scanBarcode = new ScanBarcodeView(false, false, AllScanBarcode, ScanCompletedBarcode, SaveCompletedBarcode);
            scanBarcode.OnScanCompleted += (List<string> result) =>
            {
                //이렇게 하지 않으면 에러 발생함.
                //아이폰에서만 에러
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await LOBSM040_ScanCompleted(result);
                });
            };

            if (Device.RuntimePlatform == Device.iOS)
            {
                await Application.Current.MainPage.Navigation.PushAsync(scanBarcode);
            }
            else if (Device.RuntimePlatform == Device.Android)
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(scanBarcode);
            }

            IsBusy = false;
            IsEnabled = true;

        }

        private async Task SwitchToggled(ToggledEventArgs e)
        {
            this.TranName = e.Value ? "완료" : "미완료";

            await GetLOBSM040();
        }

        private async Task PullToRefresh()
        {
            await GetLOBSM040();
        }

        private async Task SaveLOBSM040()
        {
            IsEnabled = false;

            string responseResult = string.Empty;
            string requestParamJson = string.Empty;

            List<LOBSM040Model> list_param = new List<LOBSM040Model>();
            list_param.Add(new LOBSM040Model { Slipno = "A001101904010001", Lbbrcd = "10351839090040000200001", Status = "Y" });
            string jsonString = JsonConvert.SerializeObject(list_param);

            Dictionary<string, string> requestDic = new Dictionary<string, string>();
            requestDic.Add("UFN", "{? = call ufn_set_lobsm040(?, ?)}");  //함수 호출
            requestDic.Add("p_barcode_json", jsonString);
            requestDic.Add("p_userid", "90773532");

            requestParamJson = JsonConvert.SerializeObject(requestDic);

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            responseResult = await BaseHttpService.Instance.SetRequestAsync(requestParamJson);

            sw.Stop();
            IsEnabled = true;
        }

        public async void Init(List<LOBSM030Model> lobsm030Models, bool isTranToggle)
        {
            this._lobsm030Models = lobsm030Models;
            this.IsTranToggle = isTranToggle;
        }

        public async Task GetLOBSM040()
        {
            Debug.WriteLine("GetLOBSM040!!!!!!!!!!!!!!!! " + this.IsTranToggle +" /// "+ this._lobsm030Models[0].Dlvycd  + " /// " + this._lobsm030Models.Count);

            IsEnabled = false;

            //network access check

            if (await VersionCheck.Instance.IsUpdate())
            {
                await VersionCheck.Instance.UpdateCheck();
                IsEnabled = true;
                return;
            }

            this.RowTotal = 0;
            this.SearchResult.Clear();
            this._listSearchResult.Clear();

            string responseResult = string.Empty;
            string requestParamJson = string.Empty;

            Dictionary<string, string> requestDic = new Dictionary<string, string>();
            if (this.IsTranToggle)
            {
                requestDic.Add("UFN", "{? = call ufn_get_lobsm041(?, ?, ?, ?, ?, ?)}"); //완료
            }
            else
            {
                requestDic.Add("UFN", "{? = call ufn_get_lobsm040(?, ?, ?, ?, ?, ?)}"); //미완료
            }

            if(this._lobsm030Models.Count == 1)
            {
                requestDic.Add("p_compky", this._lobsm030Models[0].Compky);
                requestDic.Add("p_wareky", this._lobsm030Models[0].Wareky);
                requestDic.Add("p_rqshpd", this._lobsm030Models[0].Rqshpd);
                requestDic.Add("p_dlwrky", this._lobsm030Models[0].Dlwrky);
                requestDic.Add("p_ruteky", this._lobsm030Models[0].Ruteky);
                requestDic.Add("p_dlvycd", this._lobsm030Models[0].Dlvycd);
            }
            else
            {
                foreach (var item in this._lobsm030Models)
                {

                }
            }

            requestParamJson = JsonConvert.SerializeObject(requestDic);

            //json결과값
            responseResult = await BaseHttpService.Instance.GetRequestAsync(requestParamJson);

            if (string.IsNullOrEmpty(responseResult) || responseResult.StartsWith("ERROR"))
            {
                if (responseResult.StartsWith("ERROR"))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", responseResult, "OK");
                }

                IsEnabled = true;

                return;
            }
            else
            {
                _listSearchResult = JsonConvert.DeserializeObject<List<LOBSM040Model>>(responseResult);

                this.RowTotal = _listSearchResult.Count;
                this.SearchResult.AddRange(_listSearchResult, System.Collections.Specialized.NotifyCollectionChangedAction.Reset);

                _listSearchResult.Clear();
            }

            IsEnabled = true;

        }

        public async Task LOBSM040_ScanCompleted(List<string> scanResult)
        {
            foreach (var scanItem in scanResult) //카메라 화면에서 받아 온 것
            {
                foreach (var item in SearchResult) //화면에 있는 데이터
                {
                    if (item.Lbbrcd == scanItem)
                    {
                        //해당 라인으로 포커스 이동
                        int rowNum = this.Grid.FindRowByValue("Lbbrcd", scanItem); //PalletID
                        this.Grid.SelectedRowHandle = rowNum; // 선택
                        this.Grid.ScrollToRow(rowNum); //해당 Row로 이동

                        //this.Pltid = this._gridControl.GetCellValue(rowNum, "Lbbrcd").ToString();

                        //ToDo
                        //await SearchDetail();

                        break;
                    }
                }
            }
        }

        public GridControl Grid
        {
            get { return _gridControl; }
            set { _gridControl = value; }
        }

        public ObservableRangeCollection<LOBSM040Model> SearchResult { get => _searchResult; set => SetProperty(ref this._searchResult, value); }
        public string TranName { get => _tranName; set => SetProperty(ref _tranName, value); }
        public bool IsTranToggle { get => _isTranToggle; set => SetProperty(ref _isTranToggle, value); }
        public int RowTotal { get => _rowTotal; set => SetProperty(ref _rowTotal, value); }
    }
}
