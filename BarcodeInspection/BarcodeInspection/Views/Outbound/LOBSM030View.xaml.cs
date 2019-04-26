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
	public partial class LOBSM030View : ContentPage
	{
		public LOBSM030View ()
		{
			InitializeComponent ();
            ThemeManager.ThemeName = Themes.Light;

            (this.BindingContext as LOBSM030ViewModel).Grid = this.grid;
		}

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await (this.BindingContext as LOBSM030ViewModel).GetLOBSM030();
        }
    }
}