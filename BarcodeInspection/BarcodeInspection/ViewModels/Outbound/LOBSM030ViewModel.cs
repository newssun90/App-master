using BarcodeInspection.Controls;
using BarcodeInspection.Helpers;
using BarcodeInspection.Models.Common;
using BarcodeInspection.Models.Outbound;
using BarcodeInspection.Services;
using BarcodeInspection.Views.Outbound;
using DevExpress.Mobile.DataGrid;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Xamarin.Forms;

namespace BarcodeInspection.ViewModels.Outbound
{
    public class LOBSM030ViewModel : ViewModelBase
    {
        private GridControl _gridControl;
        private DateTime _rqshpd = Convert.ToDateTime("1990-01-01");
        private string _tranName = string.Empty;
        private int _rowTotal = 0;
        private bool _isTranToggle = false;

        private LOBSM030Model _selectedLOBSM030Model = new LOBSM030Model();

        private ObservableRangeCollection<LOBSM030Model> _searchResult = new ObservableRangeCollection<LOBSM030Model>();

        private List<LOBSM030Model> _listSearchResult = new List<LOBSM030Model>();

        public ICommand DateSelectedCommand { get; }
        public ICommand GridRowDoubleTapCommand { get; }
        public ICommand SearchAllDetailCommand { get; }
        public ICommand PullToRefreshCommand { get; }
        public ICommand SwitchToggledCommand { get; }
        public ICommand ClearCommand { get; }

        public LOBSM030ViewModel()
        {
            Rqshpd = DateTime.Now;
            TranName = "미완료";

            DateSelectedCommand = new Command(async () => await DateSelected());
            GridRowDoubleTapCommand = new Command<DevExpress.Mobile.DataGrid.RowDoubleTapEventArgs>(async (e) => await GirdRowDoubleTap(e));
            PullToRefreshCommand = new Command(async () => await PullToRefresh());
            SearchAllDetailCommand = new Command(async () => await SearchAllDetail());
            SwitchToggledCommand = new Command<ToggledEventArgs>(async (e) => SwitchToggled(e));
            ClearCommand = new Command(() => Clear());
        }

        private async Task DateSelected()
        {
            await GetLOBSM030();
        }

        private async Task PullToRefresh()
        {
            await GetLOBSM030();
        }

        private async Task GirdRowDoubleTap(RowDoubleTapEventArgs e)
        {
            Debug.WriteLine("SelectedLOBSM030Model Dlvycd ::: " + SelectedLOBSM030Model.Dlvycd);
            if (e.RowHandle >= 0)
            {
                await SearchDetail();
            }
        }

        private async Task SearchDetail()
        {
            IsEnabled = false;

            if (await VersionCheck.Instance.IsUpdate())
            {
                await VersionCheck.Instance.UpdateCheck();
                IsEnabled = true;

                return;
            }

            if (!string.IsNullOrEmpty(SelectedLOBSM030Model.Dlvycd))
            {
                List<LOBSM030Model> selectResult = new List<LOBSM030Model>();
                selectResult.Insert(0, this.SelectedLOBSM030Model);

                await Application.Current.MainPage.Navigation.PushAsync(new LOBSM040View(selectResult, this.IsTranToggle));
            }

            IsEnabled = true;

        }

        private async Task SearchAllDetail()
        {
            if (this._listSearchResult.Count > 0)
            {
                await Application.Current.MainPage.Navigation.PushAsync(new LOBSM040View(this._listSearchResult, this.IsTranToggle));
            }
        }

        private void SwitchToggled(ToggledEventArgs e)
        {
            this.TranName = e.Value ? "완료" : "미완료";

            Device.BeginInvokeOnMainThread(async () =>
            {
                await GetLOBSM030();
            });
        }

        private void Clear()
        {
            this.SearchResult.Clear();
        }

        public async Task GetLOBSM030()
        {
            IsEnabled = false;

            this.RowTotal = 0;
            this.SearchResult.Clear();
            this._listSearchResult.Clear();

            string responseResult = string.Empty;
            string requestParamJson = string.Empty;

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            Dictionary<string, object> requestDic = new Dictionary<string, object>();
            if (this.IsTranToggle)
            {
                requestDic.Add("UFN", "{? = call ufn_get_lobsm031(?, ?, ?, ?, ?)}"); //완료
            }
            else
            {
                requestDic.Add("UFN", "{? = call ufn_get_lobsm030(?, ?, ?, ?, ?)}"); //미완료
            }

            //하드코딩..
            requestDic.Add("p_compky", "A001"); //(T- lmaccp1 맑은식품) 프로시저 파라미터와 동일하게
            requestDic.Add("p_wareky", "10"); //(T- lmacwh1 wareky) 프로시저 파라미터와 동일하게
            requestDic.Add("p_rqshpd", this.Rqshpd.ToString("yyyy-MM-dd"));
            requestDic.Add("p_dlwrky", "A21"); //프로시저 파라미터와 동일하게
            requestDic.Add("p_ruteky", "A21"); //프로시저 파라미터와 동일하게

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

                _listSearchResult = JsonConvert.DeserializeObject<List<LOBSM030Model>>(responseResult);

                this.RowTotal = _listSearchResult.Count;
                this.SearchResult.AddRange(_listSearchResult, System.Collections.Specialized.NotifyCollectionChangedAction.Reset);
                _listSearchResult.Clear();

                sw.Stop();

            }

            IsEnabled = true;
        }

        public ObservableRangeCollection<LOBSM030Model> SearchResult
        {
            get => _searchResult;
            set => SetProperty(ref this._searchResult, value);
        }

        public GridControl Grid
        {
            get { return _gridControl; }
            set { _gridControl = value; }
        }

        public DateTime Rqshpd { get => _rqshpd; set => SetProperty(ref _rqshpd, value); }
        public int RowTotal { get => _rowTotal; set => SetProperty(ref _rowTotal, value); }
        public string TranName { get => _tranName; set => SetProperty(ref _tranName, value); }
        public bool IsTranToggle { get => _isTranToggle; set => SetProperty(ref _isTranToggle, value); }
        public LOBSM030Model SelectedLOBSM030Model { get => _selectedLOBSM030Model; set => SetProperty(ref _selectedLOBSM030Model, value); }

    }
}
