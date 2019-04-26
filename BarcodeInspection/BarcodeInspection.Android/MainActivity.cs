using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android;
using Android.Support.Design.Widget;
using System.Collections.Generic;

namespace BarcodeInspection.Droid
{
    [Activity(Label = "BarcodeInspection", Icon = "@drawable/icon2", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        View layout;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            DevExpress.Mobile.Forms.Init();

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        protected override void OnStart()
        {
            base.OnStart();

            //https://developer.android.com/guide/topics/security/permissions#normal-dangerous
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M) //23이상부터
            {
                List<string> permissions = new List<string>();

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.WriteExternalStorage);
                }

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.Camera);
                }

                if (permissions.Count > 0)
                {
                    ActivityCompat.RequestPermissions(this, permissions.ToArray(), 1);
                }
            }
        }
    }
}