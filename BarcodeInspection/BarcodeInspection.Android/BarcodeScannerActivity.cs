using Android.App;
using Android.Content;
using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BarcodeInspection.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BarcodeInspection.Droid
{
    [Activity(Label = "Barcode Scan", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, Theme = "@android:style/Theme.NoTitleBar")]
    [MetaData("android.support.PARENT_ACTIVITY", Value = "com.daesangit.barcodeinspection.MainActivity")]
    public class BarcodeScannerActivity : Activity
    {
        const string TAG = "Preview";

        public bool IsContinue;
        public bool IsFixed;
        public bool IsInterestArea;
        public int rowPosition;
        public IList<string> AllScanBarcode; //현재 배송군에서 스캔해야 할 바코드 리스트
        public IList<string> ScanCompletedBarcode; //스캔 완료한 바코드 리스트
        public IList<string> SaveCompletedBarcode; //부분 저장 완료한 바코드 리스트
        //public IList<string> ScandedBarcode = new List<string>(); //현재 화면에서 스캔 완료 한 바코드 리스트

        public CameraSource mCameraSource = null;
        public CameraSourcePreview mPreview;
        GraphicOverlay mGraphicOverlay;


        //ImageButton buttonFlash;
        TextView buttonFlash;

        public static event Action<Barcode> OnScanCompleted;

        public ToneGenerator toneGen1 = new ToneGenerator(Stream.Alarm, 100);
        public Vibrator vibrator = (Vibrator)Android.App.Application.Context.GetSystemService(Context.VibratorService);

        public MediaPlayer _playerBeep;
        public MediaPlayer _playerCaution;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.BarcodeTracker);

            //this.ActionBar.SetDisplayHomeAsUpEnabled(true);

            buttonFlash = FindViewById<TextView>(Resource.Id.btnFlash2);
            buttonFlash.Click += ButtonFlash_Click;

            var buttonExit = FindViewById<ImageButton>(Resource.Id.btnExit);
            buttonExit.Click += ButtonExit_Click;

            Intent intent = new Intent(this.Intent);

            rowPosition = 0;
            IsContinue = intent.GetBooleanExtra("IsContinue", false);
            IsFixed = intent.GetBooleanExtra("IsFixed", false);

            AllScanBarcode = intent.GetStringArrayListExtra("AllScanBarcode");
            ScanCompletedBarcode = intent.GetStringArrayListExtra("ScanCompletedBarcode");
            SaveCompletedBarcode = intent.GetStringArrayListExtra("SaveCompletedBarcode");

            mPreview = FindViewById<CameraSourcePreview>(Resource.Id.preview);
            mGraphicOverlay = FindViewById<GraphicOverlay>(Resource.Id.barcodeOverlay);

            //mView = FindViewById<TouchView>(Resource.Id.left_top_view);
            //mView.setRec(rec);

            //BarcodeFormat supportBarcodeFormat = new BarcodeFormat();
            //supportBarcodeFormat |= BarcodeFormat.Code128;
            //supportBarcodeFormat |= BarcodeFormat.Ean13;

            string jsonList = Xamarin.Forms.Application.Current.Properties["SupportBarcodeFormat"].ToString();
            List<Helpers.BarcodeFormat> tmpList = JsonConvert.DeserializeObject<List<Helpers.BarcodeFormat>>(jsonList);
            
            Detector detector = null;

            //Xamarin.Android에서 AztecCode를 사용하기 위해서는 바코드 지원타입을 비워놔야 한다.
            //AztecCode 지원코드는 4096인데 Xamarin.Android에선 코드 타입이 없고 전체로 하면 스캔 가능한다.
            if (tmpList.Contains(Helpers.BarcodeFormat.AztecCode))
            {
                detector = new BarcodeDetector.Builder(Application.Context)
                    .Build();
            }
            else
            {
                var barcodeFormat = ConvertToAndroid(JsonConvert.DeserializeObject<List<Helpers.BarcodeFormat>>(jsonList));
                detector = new BarcodeDetector.Builder(Application.Context)
                    .SetBarcodeFormats(barcodeFormat)
                    .Build();
            }

            detector.SetProcessor(new MultiProcessor.Builder(new GraphicBarcodeTrackerFactory(mGraphicOverlay, this)).Build());

            //FocusingProcessor를 사용해도 되기는 하나 한번 포커스 지정되고 나서 연속 스캔할때 벗어나도 계속 스캐되는 버그가 있음.  
            //detector.SetProcessor(new CentralBarcodeFocusingProcessor(detector, new GraphicBarcodeTracker(mGraphicOverlay, this)));

            if (!detector.IsOperational)
            {
                Android.Util.Log.Warn(TAG, "Barcode detector dependencies are not yet available.");
            }

            mCameraSource = new CameraSource.Builder(Application.Context, detector)
            .SetRequestedPreviewSize((int)DeviceDisplay.MainDisplayInfo.Height, (int)DeviceDisplay.MainDisplayInfo.Width) //Max Size :  1920 x 1080 (아이폰과 동일함)
            //.SetRequestedPreviewSize(1600, 1200)
            .SetFacing(CameraFacing.Back)
            .SetRequestedFps(30.0f) //안드로이 공식샘플은 15.0f 로 되어 있으나 기본값은 30.0f
            .SetAutoFocusEnabled(true)
            .Build();

            _playerBeep = MediaPlayer.Create(this, Resource.Raw.beep07); //Beep음 (Scandit과 동일함)
            _playerCaution = MediaPlayer.Create(this, Resource.Raw.beep06); //경고음.
        }

        //public override bool OnOptionsItemSelected(IMenuItem item)
        //{
        //    Console.WriteLine(item.ItemId.ToString());
        //    if (item.ItemId == 16908332)
        //    {
        //        OnBackPressed();
        //        return true;
        //    }

        //    return base.OnOptionsItemSelected(item);
        //}

        public Android.Gms.Vision.Barcodes.BarcodeFormat ConvertToAndroid(List<Helpers.BarcodeFormat> barcodeFormat)
        {
            Android.Gms.Vision.Barcodes.BarcodeFormat types = new Android.Gms.Vision.Barcodes.BarcodeFormat();

            var mapping = new Dictionary<Helpers.BarcodeFormat, Android.Gms.Vision.Barcodes.BarcodeFormat>
            {
                { Helpers.BarcodeFormat.Code128, Android.Gms.Vision.Barcodes.BarcodeFormat.Code128},
                { Helpers.BarcodeFormat.Code39, Android.Gms.Vision.Barcodes.BarcodeFormat.Code39},
                { Helpers.BarcodeFormat.Code93, Android.Gms.Vision.Barcodes.BarcodeFormat.Code93},
                { Helpers.BarcodeFormat.Codabar, Android.Gms.Vision.Barcodes.BarcodeFormat.Codabar},
                { Helpers.BarcodeFormat.DataMatrix, Android.Gms.Vision.Barcodes.BarcodeFormat.DataMatrix},
                { Helpers.BarcodeFormat.Ean13, Android.Gms.Vision.Barcodes.BarcodeFormat.Ean13},
                { Helpers.BarcodeFormat.Ean8, Android.Gms.Vision.Barcodes.BarcodeFormat.Ean8},
                { Helpers.BarcodeFormat.Itf, Android.Gms.Vision.Barcodes.BarcodeFormat.Itf},
                { Helpers.BarcodeFormat.QrCode, Android.Gms.Vision.Barcodes.BarcodeFormat.QrCode},
                { Helpers.BarcodeFormat.UpcA, Android.Gms.Vision.Barcodes.BarcodeFormat.UpcA},
                { Helpers.BarcodeFormat.UpcE, Android.Gms.Vision.Barcodes.BarcodeFormat.UpcE},
                { Helpers.BarcodeFormat.Pdf417, Android.Gms.Vision.Barcodes.BarcodeFormat.Pdf417},
                { Helpers.BarcodeFormat.Itf14, Android.Gms.Vision.Barcodes.BarcodeFormat.Itf}
//                { Helpers.BarcodeFormat.AztecCode, Android.Gms.Vision.Barcodes.BarcodeFormat.AztecCode} // Aztec을 사용하려면 전체 선택
            };

            foreach (Helpers.BarcodeFormat barcode in barcodeFormat)
            {
                if (mapping.ContainsKey(barcode))
                {
                    types |= mapping[barcode];
                }
            }

            return types;
        }


        private void ButtonExit_Click(object sender, EventArgs e)
        {
            OnBackPressed();
        }

        //public void CameraSetup()
        //{
        //    var camera = CameraInfo.GetCamera(this.mCameraSource);

        //    var prams = camera.GetParameters();
        //    if (prams.FlashMode == Android.Hardware.Camera.Parameters.FlashModeTorch)
        //    {
        //        prams.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
        //    }
        //    else
        //    {
        //        prams.FlashMode = Android.Hardware.Camera.Parameters.FlashModeTorch;
        //    }
        //   //prams.FocusMode = Android.Hardware.Camera.Parameters.FocusModeContinuousPicture; //테스트에서 자동 포커스 안되는 현상 발생
        //    camera.SetParameters(prams);
        //}

        private LinearLayout.LayoutParams getChildLayoutParams()
        {
            var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            layoutParams.Weight = 1;
            return layoutParams;
        }

        private void ButtonFlash_Click(object sender, EventArgs e)
        {
            try
            {
                var camera = CameraInfo.GetCamera(this.mCameraSource);

                if (camera == null)
                {
                    return;
                }

                var prams = camera.GetParameters();

                if (prams.FlashMode == Android.Hardware.Camera.Parameters.FlashModeTorch)
                {
                    prams.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
                    //buttonFlash.SetImageResource(Resource.Drawable.UnLight40);
                }
                else
                {
                    prams.FlashMode = Android.Hardware.Camera.Parameters.FlashModeTorch;
                    //buttonFlash.SetImageResource(Resource.Drawable.Light40);
                }

                if (prams.SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeContinuousPicture))
                {
                    prams.FocusMode = Android.Hardware.Camera.Parameters.FocusModeContinuousPicture;
                }
                else if (prams.SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeContinuousVideo))
                {
                    prams.FocusMode = Android.Hardware.Camera.Parameters.FocusModeContinuousVideo;
                }
                else if (prams.SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeAuto))
                {
                    prams.FocusMode = Android.Hardware.Camera.Parameters.FocusModeAuto;
                }

                camera.SetParameters(prams);
            }
            catch (Exception ex)
            {
                MethodBase m = MethodBase.GetCurrentMethod();

                var properties = new Dictionary<string, string>
                {
                    { m.ReflectedType.FullName, m.ReflectedType.Name}
                };
                //Crashes.TrackError(ex, properties);
            }
        }

        private void setCameraFocusMode(String mode)
        {
            try
            {
                // Camera sees it as rotated 90 degrees, so there's some confusion with what is width and what is height)
                int width = 0;
                int height = 0;
                int halfCoordinates = 1000;
                int lengthCoordinates = 2000;
                //Rect area = mPreview.mSurfaceView.GetAreaOfInterest();

                height = mCameraSource.PreviewSize.Width;
                width = mCameraSource.PreviewSize.Height;

                //////////
                Rect rect = mPreview.mSurfaceView._clipBounds;
                float left;
                float right;
                float top;
                float bottom;

                //IList<Point> point = (item as Barcode).CornerPoints;
                left = (rect.Left * (float)mCameraSource.PreviewSize.Height) / (float)mPreview.mSurfaceView.scaleNominatorX;
                right = (rect.Right * (float)mCameraSource.PreviewSize.Height) / (float)mPreview.mSurfaceView.scaleNominatorX;

                top = (rect.Top * (float)mCameraSource.PreviewSize.Width) / (float)mPreview.mSurfaceView.scaleNominatorY;
                bottom = (rect.Bottom * (float)mCameraSource.PreviewSize.Width) / (float)mPreview.mSurfaceView.scaleNominatorY;

                var camera = CameraInfo.GetCamera(this.mCameraSource);
                var prams = camera.GetParameters();

                camera.CancelAutoFocus();
                //var parameters = camera.GetParameters();
                // Set focus and metering area equal to the area of interest. This action is essential because by defaults camera
                // focuses on the center of the frame, while the area of interest in this sample application is at the top
                List<Android.Hardware.Camera.Area> focusAreas = new List<Android.Hardware.Camera.Area>();
                Rect areasRect;

                areasRect = new Rect(
                    -halfCoordinates + (int)left * lengthCoordinates / width,
                    -halfCoordinates + (int)top * lengthCoordinates / height,
                    -halfCoordinates + lengthCoordinates * (int)right / width,
                    -halfCoordinates + lengthCoordinates * (int)bottom / height
                );

                //areasRect = new Rect(-1000,-1000, 1000, 1000);

                focusAreas.Add(new Android.Hardware.Camera.Area(areasRect, 1000));

                if (prams.MaxNumFocusAreas >= focusAreas.Count)
                {
                    prams.FocusAreas = focusAreas; //null이면 장비에서 자동으로 설정된다.
                }

                if (prams.MaxNumMeteringAreas >= focusAreas.Count)
                {
                    prams.MeteringAreas = focusAreas;
                }

                camera.SetParameters(prams);
            }
            catch (Exception ex)
            {
                MethodBase m = MethodBase.GetCurrentMethod();

                var properties = new Dictionary<string, string>
                {
                    { m.ReflectedType.FullName, m.ReflectedType.Name}
                };
                //Crashes.TrackError(ex, properties);
            }

        }

        /// <summary>
        /// 스캔화면 자동 종료
        /// Finsh() 호출하지 않아도 됨.
        /// </summary>
        public override void OnBackPressed()
        {
            base.OnBackPressed();
            Barcode barcode = new Barcode
            {
                DisplayValue = "EXIT"
            };

            OnScanCompleted?.Invoke(barcode);
            OnScanCompleted = null;
        }

        internal void StartActivity(Intent intent, object onActivityResult)
        {

        }

        protected override void OnResume()
        {
            base.OnResume();
            StartCameraSource();
        }

        protected override void OnPause()
        {
            base.OnPause();
            mPreview.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            OnScanCompleted = null;
            if (mCameraSource != null)
            {
                mCameraSource.Release();
                mCameraSource.Dispose();
            }

            if (toneGen1 != null)
            {
                toneGen1.Release();
                toneGen1.Dispose();
            }

            if (vibrator != null)
            {
                vibrator.Dispose();
            }
        }

        //==============================================================================================
        // Camera Source Preview
        //==============================================================================================

        /**
     * Starts or restarts the camera source, if it exists.  If the camera source doesn't exist yet
     * (e.g., because onResume was called before the camera source was created), this will be called
     * again when the camera source is created.
     */
        public void StartCameraSource()
        {
            try
            {
                mPreview.Start(mCameraSource, mGraphicOverlay);
            }
            catch (Exception e)
            {
                Android.Util.Log.Error(TAG, "Unable to start camera source.", e);
                mCameraSource.Release();
                mCameraSource = null;
            }
        }


        //==============================================================================================
        // Graphic Face Tracker
        //==============================================================================================

        /**
         * Factory for creating a face tracker to be associated with a new face.  The multiprocessor
         * uses this factory to create face trackers as needed -- one for each individual.
         */
        class GraphicBarcodeTrackerFactory : Java.Lang.Object, MultiProcessor.IFactory
        {
            Activity _activity;

            public GraphicBarcodeTrackerFactory(GraphicOverlay overlay, Activity activity) : base()
            {
                Overlay = overlay;
                _activity = activity;
            }

            public GraphicOverlay Overlay { get; private set; }

            public Android.Gms.Vision.Tracker Create(Java.Lang.Object item)
            {
                return new GraphicBarcodeTracker(Overlay, _activity);
            }
        }

        /**
         * Face tracker for each detected individual. This maintains a face graphic within the app's
         * associated face overlay.
         */
        class GraphicBarcodeTracker : Tracker
        {
            private GraphicOverlay mOverlay;
            private BarcodeGraphic mBarcodeGraphic;
            private Activity _activity;

            public GraphicBarcodeTracker(GraphicOverlay overlay, Activity activity)
            {
                mOverlay = overlay;
                _activity = activity;
                mBarcodeGraphic = new BarcodeGraphic(overlay);
            }

            /**
            * Start tracking the detected face instance within the face overlay.
            * OnNewItem가 먼저 호출되고 나서 OnUpdate가 호출됨.
            */
            public override void OnNewItem(int idValue, Java.Lang.Object item)
            {
                mBarcodeGraphic.Id = idValue;
            }

            /**
            * Update the position/characteristics of the face within the overlay.
            * 바코드 Result부분
            */
            public override void OnUpdate(Detector.Detections detections, Java.Lang.Object item)
            {
                //2개이상 바코드를 스캔해서 경고창이 발생할 경우 화면이 Freeze된다.
                //if (detections.DetectedItems.Size() != 1)
                //{
                //    return;
                //}

                Rect rect = ((BarcodeScannerActivity)_activity).mPreview.mSurfaceView._clipBounds;
                float left;
                float right;
                float top;
                float bottom;

                //IList<Point> point = (item as Barcode).CornerPoints;
                if (!Settings.ScanMode.Equals("FULL"))
                {
                    left = (rect.Left * (float)((BarcodeScannerActivity)_activity).mCameraSource.PreviewSize.Height) / (float)((BarcodeScannerActivity)_activity).mPreview.mSurfaceView.scaleNominatorX;
                    right = (rect.Right * (float)((BarcodeScannerActivity)_activity).mCameraSource.PreviewSize.Height) / (float)((BarcodeScannerActivity)_activity).mPreview.mSurfaceView.scaleNominatorX;

                    top = (rect.Top * (float)((BarcodeScannerActivity)_activity).mCameraSource.PreviewSize.Width) / (float)((BarcodeScannerActivity)_activity).mPreview.mSurfaceView.scaleNominatorY;
                    bottom = (rect.Bottom * (float)((BarcodeScannerActivity)_activity).mCameraSource.PreviewSize.Width) / (float)((BarcodeScannerActivity)_activity).mPreview.mSurfaceView.scaleNominatorY;

                    //관심영여과 스캔한 바코드가 교차영역이 없으면 skip한다.
                    //카메라와 PreView 좌표가 넘어온다. Layout 크기와 Preview 카메라 크기가 다르면 아래 코드는 작동하지 않는다.
                    //if (!(item as Barcode).BoundingBox.Intersect(((BarcodeScannerActivity)_activity).mPreview.mSurfaceView.GetAreaOfInterestCamera()))

                    if (!(item as Barcode).BoundingBox.Intersect(new Rect((int)left, (int)top, (int)right, (int)bottom)))
                    {
                        return;
                    }
                }

                //화면 표시
                mOverlay.Add(mBarcodeGraphic);

                //1. 단일/연속 구분 
                //연속 스캔
                if (((BarcodeScannerActivity)_activity).IsContinue)
                {
                    //고정(스캔 해야할 대상이 정해져 있음)
                    if (((BarcodeScannerActivity)_activity).IsFixed)
                    {
                        if (((BarcodeScannerActivity)_activity).AllScanBarcode.Contains(item.JavaCast<Barcode>().DisplayValue))
                        {
                            //1. 저장 했는지?
                            if (((BarcodeScannerActivity)_activity).SaveCompletedBarcode.Contains(item.JavaCast<Barcode>().DisplayValue))
                            {
                                //CameraStop();
                                mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), "저장 완료", 3);
                                //long[] pattern = { 0, 100, 1000 };
                                Warning();

                                //Task.Run(() =>
                                //{
                                //    ((BarcodeScannerActivity)_activity).StartCameraSource();
                                //});
                            }
                            //2. Scan완료 했는지?
                            else if (((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Contains(item.JavaCast<Barcode>().DisplayValue))
                            {
                                //CameraStop();
                                mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), "스캔 완료", 1);
                                //long[] pattern = { 0, 100, 1000 };
                                Warning();

                                //Task.Run(() =>
                                //{
                                //    ((BarcodeScannerActivity)_activity).StartCameraSource();
                                //});
                            }
                            else
                            {
                                //------------
                                //정상처리 작업
                                //------------
                                mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), string.Empty, 0);
                                OnScanCompleted?.Invoke(item.JavaCast<Barcode>());

                                ((BarcodeScannerActivity)_activity)._playerBeep.Start();

                                if (((BarcodeScannerActivity)_activity).vibrator != null)
                                {
                                    try
                                    {
                                        ((BarcodeScannerActivity)_activity).vibrator.Vibrate(300);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }

                                if (!((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Contains(item.JavaCast<Barcode>().DisplayValue))
                                {
                                    ((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Add(item.JavaCast<Barcode>().DisplayValue);
                                }

                                if
                                (
                                    ((BarcodeScannerActivity)_activity).AllScanBarcode.Count ==
                                    ((BarcodeScannerActivity)_activity).SaveCompletedBarcode.Count +
                                    ((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Count
                                )
                                {
                                    //CameraStop();
                                    Barcode barcode = new Barcode
                                    {
                                        DisplayValue = "EXIT"
                                    };
                                    OnScanCompleted?.Invoke(barcode);

                                    Task.Delay(300).Wait();
                                    //Thread.Sleep(300);
                                    ((BarcodeScannerActivity)_activity).Finish(); //Finsh해야 스캔 화면 종료됨.
                                }
                                else
                                {
                                    //연속스캔 사이의 간격 지정
                                    Task.Delay(1000).Wait();
                                    //Thread.Sleep(1000);
                                }
                            }
                        }
                        else
                        {
                            mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), "스캔 대상X", 2);
                            Warning();
                        }
                    }
                    //비고정(스캔 대상이 없음)
                    else
                    {
                        //현재로서는 biz로직이 없음
                        //mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), 0, ((BarcodeScannerActivity)_activity).rowPosition++);
                        //OnScanCompleted(item.JavaCast<Barcode>());
                        //if (!((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Contains(item.JavaCast<Barcode>().DisplayValue))
                        //{
                        //    ((BarcodeScannerActivity)_activity).ScanCompletedBarcode.Add(item.JavaCast<Barcode>().DisplayValue);
                        //}

                        //Task.Delay(700).Wait();
                    }
                }
                //단일 스캔
                else
                {
                    //CameraStop();
                    mBarcodeGraphic.UpdateBarcode(item.JavaCast<Barcode>(), string.Empty, 1);
                    OnScanCompleted?.Invoke(item.JavaCast<Barcode>());

                    ((BarcodeScannerActivity)_activity)._playerBeep.Start();

                    if (((BarcodeScannerActivity)_activity).vibrator != null)
                    {
                        try
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            ((BarcodeScannerActivity)_activity).vibrator.Vibrate(300);
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    Task.Delay(300).Wait(); //Non-Blocking
                    //Thread.Sleep(300); //Blocking
                    ((BarcodeScannerActivity)_activity).Finish(); //Finsh해야 스캔 화면 종료됨.
                }

                if (mBarcodeGraphic != null)
                {
                    mOverlay.Remove(mBarcodeGraphic);
                }
            }


            private void Warning()
            {
                try
                {
                    ((BarcodeScannerActivity)_activity)._playerCaution.Start();

                    if (((BarcodeScannerActivity)_activity).vibrator != null)
                    {
                        try
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            ((BarcodeScannerActivity)_activity).vibrator.Vibrate(1000);
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                //Thread.Sleep(2000); // 비프음과 진동 시간을 더한만큼 대기해야 한다.
                Task.Delay(2000).Wait();
            }

            //정지는 이렇게 아니면 작동하지 않는다.
            private void CameraStop()
            {
                //Task.Run(() => ((BarcodeScannerActivity)_activity).OnPause());
                Task.Run(() => ((BarcodeScannerActivity)_activity).mPreview.Stop());

            }

            /**
            * Hide the graphic when the corresponding face was not detected.  This can happen for
            * intermediate frames temporarily (e.g., if the face was momentarily blocked from
            * view).
            */
            public override void OnMissing(Detector.Detections detections)
            {
                if (mBarcodeGraphic != null)
                {
                    mOverlay.Remove(mBarcodeGraphic);
                }
            }

            /**
            * Called when the face is assumed to be gone for good. Remove the graphic annotation from
            * the overlay.
            */
            public override void OnDone()
            {
                if (mBarcodeGraphic != null)
                {
                    mOverlay.Remove(mBarcodeGraphic);
                }
            }
        }
    }
}