using Android.Content;
using Android.Gms.Vision;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using System;
using System.Collections.Generic;
using BarcodeInspection.Helpers;

namespace BarcodeInspection.Droid
{
    public class SurfaceViewWithOverlay : SurfaceView
    {
        private string[] lines;
        private Paint backgroundPaint;
        private Paint areaOfInterestPaint;
        private Rect areaOfInterest;
        private Rect areaOfInterestCamera;
        private Paint lineBoundariesPaint;

        public int scaleNominatorX = 1; //화면 사이즈
        public int scaleNominatorY = 1; //화면 사이즈

        private int scaleDenominatorX = 1; //카메라 사이즈
        private int scaleDenominatorY = 1; //카메라 사이즈

        //Crop
        private Drawable mLeftTopIcon;
        private Drawable mRightTopIcon;
        private Drawable mLeftBottomIcon;
        private Drawable mRightBottomIcon;

        private bool mLeftTopBool = false;
        private bool mRightTopBool = false;
        private bool mLeftBottomBool = false;
        private bool mRightBottomBool = false;

        // Starting positions of the bounding box

        private float mLeftTopPosX = 0;
        private float mLeftTopPosY = 0;

        private float mRightTopPosX = 0;
        private float mRightTopPosY = 0;

        private float mLeftBottomPosX = 0;
        private float mLeftBottomPosY = 0;

        private float mRightBottomPosX = 0;
        private float mRightBottomPosY = 0;
        private float mPosX;
        private float mPosY;

        private float mLastTouchX;
        private float mLastTouchY;

        private Paint topLine;
        private Paint bottomLine;
        private Paint leftLine;
        private Paint rightLine;

        public Rect _clipBounds;

        private int mCenter;

        private static int INVALID_POINTER_ID = -1;
        private int mActivePointerId = INVALID_POINTER_ID;

        // you can ignore this 
        public ScaleGestureDetector mScaleDetector;
        public float mScaleFactor = 1.0f;

        private int _width;
        private int _height;

        CameraSource mCameraSource;

        public SurfaceViewWithOverlay(Context context) : base(context)
        {
            base.SetWillNotDraw(false);

            if (Settings.ScanMode.Equals("FULL"))
            {
                return;
            }

            areaOfInterestPaint = new Paint();
            areaOfInterestPaint.SetARGB(70, 0, 0, 0);
            areaOfInterestPaint.SetStyle(Paint.Style.Fill);

            lineBoundariesPaint = new Paint();
            lineBoundariesPaint.SetStyle(Paint.Style.Stroke);
            lineBoundariesPaint.SetARGB(255, 128, 128, 128);
            //lineBoundariesPaint.SetARGB(255, 255, 255, 0); //Yellow

            CropInit(context);
        }

        public void SetCamera(CameraSource cameraSource)
        {
            if (cameraSource != null)
            {
                mCameraSource = cameraSource;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nominator">Layout Size</param>
        /// <param name="denominator">Camera Priview Size</param>
        public void SetScaleX(int nominator, int denominator)
        {
            scaleNominatorX = nominator;
            scaleDenominatorX = denominator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nominator">Layout Size</param>
        /// <param name="denominator">Camera Priview Size</param>
        public void SetScaleY(int nominator, int denominator)
        {
            scaleNominatorY = nominator;
            scaleDenominatorY = denominator;
        }

        //protected override void OnDraw(Canvas canvas)
        //{
        //    base.OnDraw(canvas);

        //    int width = canvas.Width;
        //    int height = canvas.Height;

        //    //화면Layout 사이즈와 동일하게 맞춘다.
        //    scaleNominatorX = canvas.Width;
        //    scaleNominatorY = canvas.Height;

        //    canvas.Save();

        //    //canvas.DrawRect(0, 0, width, height, backgroundPaint);

        //    if (areaOfInterest != null)
        //    {
        //        // Shading and clipping the area of interest
        //        //float left = (areaOfInterest.Left * (float)scaleNominatorX) / (float)scaleDenominatorX;
        //        //float right = (areaOfInterest.Right * (float)scaleNominatorX) / (float)scaleDenominatorX;
        //        //float top = (areaOfInterest.Top * (float)scaleNominatorY) / (float)scaleDenominatorY;
        //        //float bottom = (areaOfInterest.Bottom * (float)scaleNominatorY) / (float)scaleDenominatorY;

        //        float left = (areaOfInterest.Left * (float)scaleNominatorX) / (float)scaleDenominatorX;
        //        float right = (areaOfInterest.Right * (float)scaleNominatorX) / (float)scaleDenominatorX;
        //        float top = (areaOfInterest.Top * (float)scaleNominatorY) / (float)scaleDenominatorY;
        //        float bottom = (areaOfInterest.Bottom * (float)scaleNominatorY) / (float)scaleDenominatorY;

        //        canvas.DrawRect(0, 0, width, top, areaOfInterestPaint);
        //        canvas.DrawRect(0, bottom, width, height, areaOfInterestPaint);
        //        canvas.DrawRect(0, top, left, bottom, areaOfInterestPaint);
        //        canvas.DrawRect(right, top, width, bottom, areaOfInterestPaint);
        //        canvas.DrawRect(left, top, right, bottom, lineBoundariesPaint);
        //        canvas.ClipRect(left, top, right, bottom);
        //    }

        //    canvas.Restore();
        //}


        public void SetAreaOfInterest(Rect newValue)
        {
            this.areaOfInterest = newValue;
            Invalidate();
        }

        public Rect GetAreaOfInterest()
        {
            return this.areaOfInterest;
        }

        public void SetAreaOfInterestCamera(Rect newValue)
        {
            this.areaOfInterestCamera = newValue;
            Invalidate();
        }

        public Rect GetAreaOfInterestCamera()
        {
            return this.areaOfInterestCamera;
        }


        //crop

        private void CropInit(Context context)
        {
            // I need to create lines for the bouding box to connect

            topLine = new Paint();
            bottomLine = new Paint();
            leftLine = new Paint();
            rightLine = new Paint();

            SetLineParameters(Color.White, 2);

            // Here I grab the image that will work as the corners of the bounding
            // box and set their positions.
            mLeftTopIcon = context.Resources.GetDrawable(Resource.Drawable.corners);

            mCenter = mLeftTopIcon.MinimumHeight / 2;
            //mLeftTopIcon.SetBounds((int)mLeftTopPosX, (int)mLeftTopPosY, mLeftTopIcon.IntrinsicWidth + (int)mLeftTopPosX, mLeftTopIcon.IntrinsicHeight + (int)mLeftTopPosY);

            mRightTopIcon = context.Resources.GetDrawable(Resource.Drawable.corners);
            //mRightTopIcon.SetBounds((int)mRightTopPosX, (int)mRightTopPosY, mRightTopIcon.IntrinsicWidth + (int)mRightTopPosX, mRightTopIcon.IntrinsicHeight + (int)mRightTopPosY);

            mLeftBottomIcon = context.Resources.GetDrawable(Resource.Drawable.corners);
            //mLeftBottomIcon.SetBounds((int)mLeftBottomPosX, (int)mLeftBottomPosY, mLeftBottomIcon.IntrinsicWidth + (int)mLeftBottomPosX, mLeftBottomIcon.IntrinsicHeight + (int)mLeftBottomPosY);

            mRightBottomIcon = context.Resources.GetDrawable(Resource.Drawable.corners);
            //mRightBottomIcon.SetBounds((int)mRightBottomPosX, (int)mRightBottomPosY, mRightBottomIcon.IntrinsicWidth + (int)mRightBottomPosX, mRightBottomIcon.IntrinsicHeight + (int)mRightBottomPosY);

            // Create our ScaleGestureDetector
            mScaleDetector = new ScaleGestureDetector(context, new ScaleListener());
        }

        private void SetLineParameters(Color color, float width)
        {
            topLine.Color = color;
            topLine.StrokeWidth = width;

            bottomLine.Color = color;
            bottomLine.StrokeWidth = width;

            leftLine.Color = color;
            leftLine.StrokeWidth = width;

            rightLine.Color = color;
            rightLine.StrokeWidth = width;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (Settings.ScanMode.Equals("FULL"))
            {
                return;
            }

            _width = canvas.Width;
            _height = canvas.Height;

            //화면Layout 사이즈와 동일하게 맞춘다.
            scaleNominatorX = canvas.Width;
            scaleNominatorY = canvas.Height;

            canvas.Save();

            if (areaOfInterest != null)
            {
                // Shading and clipping the area of interest
                float left;
                float right;
                float top;
                float bottom;

                if (mLeftTopPosX == 0 && mLeftTopPosY == 0 && mRightTopPosX == 0 && mRightTopPosY == 0 && mLeftBottomPosX == 0 && mLeftBottomPosY == 0 && mRightBottomPosX == 0 && mRightBottomPosY == 0)
                {
                    left = (areaOfInterest.Left * (float)scaleNominatorX) / (float)scaleDenominatorX; //화면 사이즈/카메라 사이즈
                    right = (areaOfInterest.Right * (float)scaleNominatorX) / (float)scaleDenominatorX;
                    top = (areaOfInterest.Top * (float)scaleNominatorY) / (float)scaleDenominatorY;
                    bottom = (areaOfInterest.Bottom * (float)scaleNominatorY) / (float)scaleDenominatorY;

                    mLeftTopPosX = left;
                    mLeftTopPosY = top;

                    mRightTopPosX = right;
                    mRightTopPosY = top;

                    mLeftBottomPosX = left;
                    mLeftBottomPosY = bottom;

                    mRightBottomPosX = right;
                    mRightBottomPosY = bottom;
                }
                else
                {
                    if (mLeftTopPosY < 0)
                    {
                        mLeftTopPosY = 0;
                    }

                    if (mRightTopPosY < 0)
                    {
                        mRightTopPosY = 0;
                    }

                    if (mLeftBottomPosY > this._height)
                    {
                        mLeftBottomPosY = this._height;
                    }

                    if (mRightBottomPosY > this._height)
                    {
                        mRightBottomPosY = this._height;
                    }

                    left = mLeftTopPosX;
                    top = mLeftTopPosY;
                    right = mRightTopPosX;
                    bottom = mLeftBottomPosY;
                }

                if (right >= _width)
                {
                    Console.WriteLine("Right : " + right + ", " + " Width : " + _width);
                }

                if (bottom >= _height)
                {
                    Console.WriteLine("Bottom : " + bottom + ", " + " Height : " + _height);
                }

                //여집합 부분에 회색으로 표시
                canvas.DrawRect(0, 0, _width, top, areaOfInterestPaint);
                canvas.DrawRect(0, bottom, _width, _height, areaOfInterestPaint);
                canvas.DrawRect(0, top, left, bottom, areaOfInterestPaint);
                canvas.DrawRect(right, top, _width, bottom, areaOfInterestPaint);

                //Interest 영역표시
                canvas.DrawRect(left, top, right, bottom, lineBoundariesPaint);
                canvas.ClipRect(left, top, right, bottom);

                Console.WriteLine("left : " + left + ",  top : " + top + ",  right : " + right + ",  bottom : " + bottom);

                _clipBounds = canvas.ClipBounds;

                //
                //canvas.DrawLine(mLeftTopPosX + mCenter, mLeftTopPosY + mCenter, mRightTopPosX + mCenter, mRightTopPosY + mCenter, topLine);
                //canvas.DrawLine(mLeftBottomPosX + mCenter, mLeftBottomPosY + mCenter, mRightBottomPosX + mCenter, mRightBottomPosY + mCenter, bottomLine);
                //canvas.DrawLine(mLeftTopPosX + mCenter, mLeftTopPosY + mCenter, mLeftBottomPosX + mCenter, mLeftBottomPosY + mCenter, leftLine);
                //canvas.DrawLine(mRightTopPosX + mCenter, mRightTopPosY + mCenter, mRightBottomPosX + mCenter, mRightBottomPosY + mCenter, rightLine);

                mLeftTopIcon.SetBounds((int)mLeftTopPosX, (int)mLeftTopPosY, mLeftTopIcon.IntrinsicWidth + (int)mLeftTopPosX, mLeftTopIcon.IntrinsicHeight + (int)mLeftTopPosY);
                mRightTopIcon.SetBounds((int)mRightTopPosX - mRightTopIcon.IntrinsicWidth, (int)mRightTopPosY, (int)mRightTopPosX, (int)mRightTopPosY + mRightTopIcon.IntrinsicHeight);

                mLeftBottomIcon.SetBounds((int)mLeftBottomPosX, (int)mLeftBottomPosY - mLeftBottomIcon.IntrinsicHeight, mLeftBottomIcon.IntrinsicWidth + (int)mLeftBottomPosX, (int)mLeftBottomPosY);
                mRightBottomIcon.SetBounds((int)mRightBottomPosX - mRightBottomIcon.IntrinsicWidth, (int)mRightBottomPosY - mRightBottomIcon.IntrinsicHeight, (int)mRightBottomPosX, (int)mRightBottomPosY);

                mLeftTopIcon.Draw(canvas);
                mRightTopIcon.Draw(canvas);
                mLeftBottomIcon.Draw(canvas);
                mRightBottomIcon.Draw(canvas);


                //포커스 영역 설정
                setCameraFocusMode();
            }

            canvas.Restore();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (Settings.ScanMode.Equals("FULL"))
            {
                return true;
            }


            //return base.OnTouchEvent(e);
            MotionEventActions action = e.Action;
            bool intercept = true;
            int pointerIndex;
            float x;
            float y;

            float dx;
            float dy;

            switch (action)
            {
                case MotionEventActions.Down:
                    x = e.GetX();
                    y = e.GetY();

                    // in CameraPreview we have Rect rec. This is passed here to return
                    // a false when the camera button is pressed so that this view ignores
                    // the touch event.

                    //if ((x >= buttonRec.Left) && (x <= buttonRec.Right) && (y >= buttonRec.Top) && (y <= buttonRec.Bottom))
                    //{
                    //    intercept = false;
                    //    break;
                    //}

                    // is explained below, when we get to this method.
                    manhattanDistance(x, y);

                    // Remember where we started
                    mLastTouchX = x;
                    mLastTouchY = y;
                    mActivePointerId = e.GetPointerId(0);
                    break;
                case MotionEventActions.Move:
                    pointerIndex = e.FindPointerIndex(mActivePointerId);
                    x = e.GetX();
                    y = e.GetY();
                    //Log.i(TAG,"x: "+x);
                    //Log.i(TAG,"y: "+y);

                    // Only move if the ScaleGestureDetector isn't processing a gesture.
                    // but we ignore here because we are not using ScaleGestureDetector.
                    if (!mScaleDetector.IsInProgress)
                    {
                        dx = x - mLastTouchX;
                        dy = y - mLastTouchY;

                        mPosX += dx;
                        mPosY += dy;

                        Invalidate();
                    }

                    // Calculate the distance moved
                    dx = x - mLastTouchX;
                    dy = y - mLastTouchY;

                    // Move the object
                    if (mPosX >= 0 && mPosX <= _width)
                    {
                        mPosX += dx;
                    }

                    if (mPosY >= 0 && mPosY <= _height)
                    {
                        mPosY += dy;
                    }

                    // while its being pressed n it does not overlap the bottom line or right line
                    if (mLeftTopBool && ((y + mCenter * 2) < mLeftBottomPosY) && ((x + mCenter * 2) < mRightTopPosX))
                    {
                        if (dy != 0)
                        {
                            mRightTopPosY = y;
                        }
                        if (dx != 0)
                        {
                            mLeftBottomPosX = x;
                        }

                        mLeftTopPosX = x;//mPosX;
                        mLeftTopPosY = y;//mPosY;
                    }

                    if (mRightTopBool && ((y + mCenter * 2) < mRightBottomPosY) && (x > (mLeftTopPosX + mCenter * 2)))
                    {
                        if (dy != 0)
                        {
                            mLeftTopPosY = y;
                        }
                        if (dx != 0)
                        {
                            mRightBottomPosX = x;
                        }
                        mRightTopPosX = x;//mPosX;
                        mRightTopPosY = y;//mPosY;
                    }

                    if (mLeftBottomBool && (y > (mLeftTopPosY + mCenter * 2)) && ((x + mCenter * 2) < mRightBottomPosX))
                    {
                        if (dx != 0)
                        {
                            mLeftTopPosX = x;
                        }
                        if (dy != 0)
                        {
                            mRightBottomPosY = y;
                        }
                        mLeftBottomPosX = x;
                        mLeftBottomPosY = y;
                    }

                    if (mRightBottomBool && (y > (mLeftTopPosY + mCenter * 2)) && (x > (mLeftBottomPosX + mCenter * 2)))
                    {
                        if (dx != 0)
                        {
                            mRightTopPosX = x;
                        }
                        if (dy != 0)
                        {
                            mLeftBottomPosY = y;
                        }
                        mRightBottomPosX = x;
                        mRightBottomPosY = y;
                    }

                    // Remember this touch position for the next move event
                    mLastTouchX = x;
                    mLastTouchY = y;

                    // Invalidate to request a redraw
                    Invalidate();
                    break;
                case MotionEventActions.Up:
                    // when one of these is true, that means it can move when onDraw is called
                    mLeftTopBool = false;
                    mRightTopBool = false;
                    mLeftBottomBool = false;
                    mRightBottomBool = false;
                    //mActivePointerId = INVALID_POINTER_ID;
                    break;
                case MotionEventActions.Cancel:
                    mActivePointerId = INVALID_POINTER_ID;
                    break;
                case MotionEventActions.PointerUp:
                    // Extract the index of the pointer that left the touch sensor
                    //int pointerIndex = MotionEventActions.PointerIndexMask >> MotionEventActions.PointerIndexShift;
                    /*
                    pointerIndex = (int)MotionEventActions.PointerIndexMask;

                    int pointerId = e.GetPointerId(pointerIndex);
                    if (pointerId == mActivePointerId)
                    {
                        // This was our active pointer going up. Choose a new
                        // active pointer and adjust accordingly.
                        int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                        mLastTouchX = e.GetX(newPointerIndex);
                        mLastTouchY = e.GetY(newPointerIndex);
                        mActivePointerId = e.GetPointerId(newPointerIndex);
                    }
                    */
                    break;

            }

            return intercept;
        }

        private void manhattanDistance(float x, float y)
        {
            double leftTopMan = Math.Sqrt(Math.Pow((Math.Abs((double)x - (double)mLeftTopPosX)), 2) + Math.Pow((Math.Abs((double)y - (double)mLeftTopPosY)), 2));
            double rightTopMan = Math.Sqrt(Math.Pow((Math.Abs((double)x - (double)mRightTopPosX)), 2) + Math.Pow((Math.Abs((double)y - (double)mRightTopPosY)), 2));
            double leftBottomMan = Math.Sqrt(Math.Pow((Math.Abs((double)x - (double)mLeftBottomPosX)), 2) + Math.Pow((Math.Abs((double)y - (double)mLeftBottomPosY)), 2));
            double rightBottomMan = Math.Sqrt(Math.Pow((Math.Abs((double)x - (double)mRightBottomPosX)), 2) + Math.Pow((Math.Abs((double)y - (double)mRightBottomPosY)), 2));

            //Log.Info("leftTopMan", "leftTopMan: "+leftTopMan);
            //Log.Info("rightTopMan", "rightTopMan: "+rightTopMan);

            if (leftTopMan < 50)
            {
                mLeftTopBool = true;
                mRightTopBool = false;
                mLeftBottomBool = false;
                mRightBottomBool = false;
            }
            else if (rightTopMan < 50)
            {
                mLeftTopBool = false;
                mRightTopBool = true;
                mLeftBottomBool = false;
                mRightBottomBool = false;
            }
            else if (leftBottomMan < 50)
            {
                mLeftTopBool = false;
                mRightTopBool = false;
                mLeftBottomBool = true;
                mRightBottomBool = false;
            }
            else if (rightBottomMan < 50)
            {
                mLeftTopBool = false;
                mRightTopBool = false;
                mLeftBottomBool = false;
                mRightBottomBool = true;
            }
        }

        public class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            public float mScaleFactor = 1.0f;

            public override bool OnScale(ScaleGestureDetector detector)
            {
                mScaleFactor *= detector.ScaleFactor;

                // Don't let the object get too small or too large.
                mScaleFactor = Math.Max(0.1f, Math.Min(mScaleFactor, 5.0f));

                return true;
            }
        }

        public float getmLeftTopPosX()
        {
            return mLeftTopPosX;
        }
        public float getmLeftTopPosY()
        {
            return mLeftTopPosY;
        }
        public float getmRightTopPosX()
        {
            return mRightTopPosX;
        }
        public float getmRightTopPosY()
        {
            return mRightTopPosY;
        }
        public float getmLeftBottomPosX()
        {
            return mLeftBottomPosX;
        }
        public float getmLeftBottomPosY()
        {
            return mLeftBottomPosY;
        }
        public float getmRightBottomPosY()
        {
            return mRightBottomPosY;
        }
        public float getmRightBottomPosX()
        {
            return mRightBottomPosX;
        }

        public void setInvalidate()
        {
            Invalidate();
        }

        private void setCameraFocusMode()
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
                Rect rect = this._clipBounds;
                float left;
                float right;
                float top;
                float bottom;

                //IList<Point> point = (item as Barcode).CornerPoints;
                left = (rect.Left * (float)mCameraSource.PreviewSize.Height) / (float)this.scaleNominatorX;
                right = (rect.Right * (float)mCameraSource.PreviewSize.Height) / (float)this.scaleNominatorX;

                top = (rect.Top * (float)mCameraSource.PreviewSize.Width) / (float)this.scaleNominatorY;
                bottom = (rect.Bottom * (float)mCameraSource.PreviewSize.Width) / (float)this.scaleNominatorY;

                var camera = CameraInfo.GetCamera(this.mCameraSource);

                camera.CancelAutoFocus();
                var prams = camera.GetParameters();
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

                focusAreas.Add(new Android.Hardware.Camera.Area(areasRect, 1000));

                Console.WriteLine("{0}, {1}, {2}, {3}", areasRect.Left, areasRect.Top, areasRect.Right, areasRect.Bottom);

                if (prams.MaxNumFocusAreas >= focusAreas.Count)
                {
                    prams.FocusAreas = focusAreas;
                }
                if (prams.MaxNumMeteringAreas >= focusAreas.Count)
                {
                    prams.MeteringAreas = focusAreas;
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


                // Commit the camera parameters
                camera.SetParameters(prams);
            }
            catch (Exception ex)
            {
                //MethodBase m = MethodBase.GetCurrentMethod();

                //var properties = new Dictionary<string, string>
                //{
                //    { m.ReflectedType.FullName, m.ReflectedType.Name}
                //};
                //Crashes.TrackError(ex, properties);
            }
        }
    }
}