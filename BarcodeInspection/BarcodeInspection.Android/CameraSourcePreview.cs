using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Vision;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

using BarcodeInspection.Helpers;

namespace BarcodeInspection.Droid
{
    public class CameraSourcePreview : ViewGroup
    {
        const string TAG = "CameraSourcePreview";

        private int AreaOfInterestMargin_PercentOfWidth = 4; //가로,  10 : , 1 : 전체
        private int AreaOfInterestMargin_PercentOfHeight = 30; //세로, 관심영역 표시 : 40 , 전체 : 1 , Max 45

        Context mContext;
        //public SurfaceView mSurfaceView;
        public SurfaceViewWithOverlay mSurfaceView;

        bool mStartRequested;
        protected bool SurfaceAvailable { get; set; }
        CameraSource mCameraSource;

        GraphicOverlay mOverlay;

        public CameraSourcePreview(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            mContext = context;
            mStartRequested = false;
            SurfaceAvailable = false;

            //mSurfaceView = new SurfaceView(context);
            mSurfaceView = new SurfaceViewWithOverlay(context);
            mSurfaceView.Holder.AddCallback(new SurfaceCallback(this));

            AddView(mSurfaceView);
        }

        public void Start(CameraSource cameraSource)
        {
            if (cameraSource == null)
                Stop();

            mCameraSource = cameraSource;

            if (mCameraSource != null)
            {
                mStartRequested = true;
                StartIfReady();

                mSurfaceView.SetCamera(cameraSource);
            }
        }


        public void Start(CameraSource cameraSource, GraphicOverlay overlay)
        {
            mOverlay = overlay;
            Start(cameraSource);
        }

        public void Stop()
        {
            if (mCameraSource != null)
                mCameraSource.Stop();
        }

        public void Release()
        {
            if (mCameraSource != null)
            {
                mCameraSource.Release();
                mCameraSource = null;
            }
        }

        private void StartIfReady()
        {
            if (mStartRequested && SurfaceAvailable)
            {
                mCameraSource.Start(mSurfaceView.Holder);
                if (mOverlay != null)
                {
                    var size = mCameraSource.PreviewSize;
                    var min = Math.Min(size.Width, size.Height);
                    var max = Math.Max(size.Width, size.Height);
                    if (IsPortraitMode())
                    {
                        // Swap width and height sizes when in portrait, since it will be rotated by
                        // 90 degrees
                        mOverlay.SetCameraInfo(min, max, mCameraSource.CameraFacing);
                    }
                    else
                    {
                        mOverlay.SetCameraInfo(max, min, mCameraSource.CameraFacing);
                    }
                    mOverlay.Clear();
                }
                mStartRequested = false;
            }
        }

        private class SurfaceCallback : Java.Lang.Object, ISurfaceHolderCallback
        {
            public SurfaceCallback(CameraSourcePreview parent)
            {
                Parent = parent;
            }

            public CameraSourcePreview Parent { get; private set; }

            public void SurfaceCreated(ISurfaceHolder surface)
            {
                Parent.SurfaceAvailable = true;
                try
                {
                    Parent.StartIfReady();
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error(TAG, "Could not start camera source.", ex);
                }
            }

            public void SurfaceDestroyed(ISurfaceHolder surface)
            {
                Parent.SurfaceAvailable = false;
            }

            public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height)
            {

            }
        }

        //화면 전체 사이즈
        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            //아래 2개의 값은 Preview 화면의 비율을 잡아주는데 사용.
            //BarcodeScannerActivity 값과 동일하게 설정
            //int width = 320;
            //int height = 240;
            int width = 16;
            int height = 9;

            if (mCameraSource != null)
            {
                var size = mCameraSource.PreviewSize;
                if (size != null)
                {
                    //갤럭시 S8은 이쪽으로 진입하지 못하여 카메라의 정확한 사이즈 계산 할 수 없음.
                    width = size.Width;
                    height = size.Height;
                }
            }

            // Swap width and height sizes when in portrait, since it will be rotated 90 degrees
            if (IsPortraitMode())
            {
                int tmp = width;
                width = height;
                height = tmp;
            }

            var layoutWidth = right - left;
            var layoutHeight = bottom - top;

            // Computes height and width for potentially doing fit width.
            int childWidth = layoutWidth;
            //int childHeight = (int)(((float)layoutWidth / (float)width) * (float)height);
            int childHeight = layoutHeight; //Preview full screen, hm.ji

            // If height is too tall using fit width, does fit height instead.
            if (childHeight > layoutHeight)
            {
                childHeight = layoutHeight;
                //childWidth = (int)(((float)layoutHeight / (float)height) * width);
                childWidth = layoutWidth; //Preview full screen, hm.ji
            }

            //캡쳐영억지정
            mSurfaceView.SetScaleX(layoutWidth, childWidth); //Layout, Camera Preview Size
            mSurfaceView.SetScaleY(layoutHeight, childHeight); //Layout, Camera Preview Size, bottom이 화면 전체 사이즈여서 Layout을 다른거로 사용해야 함.

            int marginWidth = (AreaOfInterestMargin_PercentOfWidth * childWidth) / 100;
            int marginHeight = (AreaOfInterestMargin_PercentOfHeight * childHeight) / 100;


            if (Settings.ScanMode.Equals("WIDE"))
            {
                mSurfaceView.SetAreaOfInterest(new Rect(marginWidth, marginHeight, childWidth - marginWidth, childHeight - marginHeight));
            }
            else if (Settings.ScanMode.Equals("SPLIT"))
            {
                mSurfaceView.SetAreaOfInterest(new Rect(0, 0, childWidth, childHeight / 3 + 20));
            }


            for (int i = 0; i < ChildCount; ++i)
            {
                GetChildAt(i).Layout(0, 0, childWidth, childHeight);
            }

            try
            {
                StartIfReady();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(TAG, "Could not start camera source.", ex);
            }
        }

        bool IsPortraitMode()
        {
            var orientation = mContext.Resources.Configuration.Orientation;
            if (orientation == Android.Content.Res.Orientation.Landscape)
                return false;
            if (orientation == Android.Content.Res.Orientation.Portrait)
                return true;

            Android.Util.Log.Debug(TAG, "isPortraitMode returning false by default");
            return false;
        }
    }
}