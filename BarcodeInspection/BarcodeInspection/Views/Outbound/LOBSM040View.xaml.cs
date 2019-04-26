using BarcodeInspection.Models.Outbound;
using BarcodeInspection.ViewModels.Outbound;
using DevExpress.Mobile.DataGrid.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BarcodeInspection.Views.Outbound
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LOBSM040View : ContentPage
	{
		public LOBSM040View (List<LOBSM030Model> lobsm030Models, bool isTranToggle)
		{
			InitializeComponent ();

            ThemeManager.ThemeName = Themes.Light;
            (this.BindingContext as LOBSM040ViewModel).Grid = this.grid;
            (this.BindingContext as LOBSM040ViewModel).Init(lobsm030Models, isTranToggle);
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await (this.BindingContext as LOBSM040ViewModel).GetLOBSM040();
        }
    }
}