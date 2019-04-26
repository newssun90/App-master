using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BarcodeInspection.Droid
{
    class BarcodeGraphic : GraphicOverlay.Graphic
    {
        const float ID_TEXT_SIZE = 50.0f;
        const float ID_Y_OFFSET = 50.0f;
        const float ID_X_OFFSET = -50.0f;
        const float BOX_STROKE_WIDTH = 5.0f;

        readonly Color[] COLOR_CHOICES = {
            Color.White,
            Color.Yellow, //현재 화면에서 중복 스캔한 경우
            Color.Red, //Fixed Type에서 스캔해야 할 목록에 없는 바코드를 스캔한 경우
            Color.Green //Fixed Type에서 저장 완료한 바코드를 스캔한 경우
            //Color.Blue,
            //Color.Cyan,

            //Color.Magenta,
            //Color.Red,
            //Color.White,
            //Color.Yellow,
            //Color.Navy,
            //Color.Olive
        };

        static int mCurrentColorIndex = 0;

        //Paint mPositionPaint;
        Paint mIdPaint;
        Paint mBoxPaint;

        Barcode mBarcode;
        string mMessage = string.Empty;

        public int Id { get; set; }

        public BarcodeGraphic(GraphicOverlay overlay) : base(overlay)
        {
            mCurrentColorIndex = (mCurrentColorIndex + 1) % COLOR_CHOICES.Length;
            var selectedColor = COLOR_CHOICES[mCurrentColorIndex];

            //mPositionPaint = new Paint();
            //mPositionPaint.Color = selectedColor;

            mIdPaint = new Paint();
            mIdPaint.Color = selectedColor;
            mIdPaint.TextSize = ID_TEXT_SIZE;

            mBoxPaint = new Paint();
            mBoxPaint.Color = selectedColor;
            mBoxPaint.SetStyle(Paint.Style.Stroke);
            mBoxPaint.StrokeWidth = BOX_STROKE_WIDTH;
        }

        /**
        * Updates the face instance from the detection of the most recent frame.  Invalidates the
        * relevant portions of the overlay to trigger a redraw.
        * 화면 표시에 색을 지정해 준다.
        * Color.White : 보통의 경우
        * Color.Yellow : 중복 바코드 스캔
        * Color.Red : 스캔 리스트 목록에 없는 경우
        * Color.Green : 저장 완료한 바코드를 스캔한 경우
        */
        public void UpdateBarcode(Barcode barcode, string message, int colorIndex)
        {
            Console.WriteLine("Barcode Format: {0}", barcode.Format);
            Console.WriteLine("Value Format: {0}", barcode.ValueFormat);

            mIdPaint.Color = COLOR_CHOICES[colorIndex];
            mBoxPaint.Color = COLOR_CHOICES[colorIndex];

            /*
            int page = Page(); //화면에 몇줄까지 사용할 것인지?

            switch (rowPosition % page)
            {
                case 0:
                    _rowPosition = 50;
                    break;
                default:
                    _rowPosition = ((rowPosition % page) * 100) + 50;
                    break;
            }
            */

            mMessage = message;
            mBarcode = barcode;
            PostInvalidate();
        }

        /**
     * Draws the face annotations for position on the supplied canvas.
     */
        public override void Draw(Canvas canvas)
        {
            Barcode barcode = mBarcode;
            if (barcode == null)
            {
                return;
            }

            string DisplayValue = string.Empty;

            if (string.IsNullOrEmpty(mMessage))
            {
                DisplayValue = String.Format("{0}", barcode.DisplayValue);
            }
            else
            {
                //DisplayValue = String.Format("{0}" + Environment.NewLine + "{1}", barcode.DisplayValue, mMessage);
                DisplayValue = barcode.DisplayValue.ToString() + System.Environment.NewLine + mMessage.ToString();
            }

            //화면의 바코드
            RectF rect = new RectF(barcode.BoundingBox);
            rect.Left = TranslateX(rect.Left);
            rect.Top = TranslateY(rect.Top);
            rect.Right = TranslateX(rect.Right);
            rect.Bottom = TranslateY(rect.Bottom);

            canvas.DrawRect(rect, mBoxPaint);

            canvas.DrawText(DisplayValue, rect.Left + 50, rect.Bottom, mIdPaint);
            //canvas.DrawText(barcode.DisplayValue, 50, _rowPosition, mIdPaint);

            //canvas.DrawText(DisplayValue, 10, _rowPosition, mIdPaint); //사용하던거
            //canvas.DrawText(DisplayValue.ToString(), 10, 70, mIdPaint); //상단에 고정해서 표시
            //canvas.DrawRect(barcode.BoundingBox.Left + ID_X_OFFSET, barcode.BoundingBox.Top, barcode.BoundingBox.Right, barcode.BoundingBox.Bottom, mBoxPaint);
        }

    }
}