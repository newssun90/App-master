using Android.Content;
using BarcodeInspection.Droid;
using BarcodeInspection.Views.Common;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(AutoUpdateView), typeof(AutoUpdateViewRenderer))]
namespace BarcodeInspection.Droid
{
    public class AutoUpdateViewRenderer : PageRenderer
    {
        public AutoUpdateViewRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            var activity = this.Context;
            var intent = new Intent(activity, typeof(AutoUpdateActivity));

            try
            {
                AutoUpdateActivity.OnUpdateCompleted += () =>
                {
                    Element.Navigation.PopModalAsync();
                };

                activity.StartActivity(intent);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}