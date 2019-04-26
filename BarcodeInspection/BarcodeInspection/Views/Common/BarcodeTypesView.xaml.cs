using BarcodeInspection.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BarcodeInspection.Views.Common
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BarcodeTypesView : ContentPage
    {
        public BarcodeTypesView()
        {
            InitializeComponent();
        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();

            await (this.BindingContext as BarcodeTypesViewModel).Save();

        }
    }
}