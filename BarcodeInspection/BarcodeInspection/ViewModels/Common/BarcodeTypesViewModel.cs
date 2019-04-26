using BarcodeInspection.Helpers;
using BarcodeInspection.Models.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BarcodeInspection.ViewModels.Common
{
    public class BarcodeTypesViewModel : ViewModelBase
    {
        private ObservableCollection<BarcodeTypesModel> _searchResult = new ObservableCollection<BarcodeTypesModel>();

        public ICommand SearchCommand { get; }
        public ICommand SaveCommand { get; }

        public BarcodeTypesViewModel()
        {
            SearchCommand = new Xamarin.Forms.Command(async () => await Search());
            SaveCommand = new Xamarin.Forms.Command(async () => await Save());

            Search();
        }

        public async Task Save()
        {
            try
            {
                List<BarcodeFormat> lstBarcodeFormat = new List<BarcodeFormat>();

                foreach (var item in SearchResult)
                {
                    if (item.IsSupport)
                    {
                        lstBarcodeFormat.Add(item.BarcodeType);
                    }
                }

                Xamarin.Forms.Application.Current.Properties["SupportBarcodeFormat"] = JsonConvert.SerializeObject(lstBarcodeFormat);

            }
            catch (Exception ex)
            {

            }
        }

        private async Task Search()
        {
            List<BarcodeTypesModel> barcodeFormat = new List<BarcodeTypesModel>();

            string jsonList = Xamarin.Forms.Application.Current.Properties["SupportBarcodeFormat"].ToString();
            List<BarcodeFormat> tmpList = JsonConvert.DeserializeObject<List<BarcodeFormat>>(jsonList);

            var enumList = Enum.GetValues(typeof(BarcodeFormat)).OfType<BarcodeFormat>().ToList();

            foreach (var item in enumList)
            {
                barcodeFormat.Add
                (
                    new BarcodeTypesModel
                    {
                        BarcodeType = item,
                        IsSupport = tmpList.Contains(item) ? true : false
                    }
                );
            }

            SearchResult = new ObservableCollection<BarcodeTypesModel>(barcodeFormat);
        }


        public ObservableCollection<BarcodeTypesModel> SearchResult { get => _searchResult; set => SetProperty(ref this._searchResult, value); }
    }
}
