using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Vision;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BarcodeInspection.Droid
{
    public class CameraInfo
    {
        public static Android.Hardware.Camera GetCamera(CameraSource cameraSource)
        {
            var javaHero = cameraSource.JavaCast<Java.Lang.Object>();
            var fields = javaHero.Class.GetDeclaredFields();
            foreach (var field in fields)
            {
                if (field.Type.CanonicalName.Equals("android.hardware.Camera", StringComparison.OrdinalIgnoreCase))
                {
                    field.Accessible = true;

                    var camera = field.Get(javaHero);
                    var cCamera = (Android.Hardware.Camera)camera;
                    return cCamera;
                }
            }
            return null;
        }
    }
}