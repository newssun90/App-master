using AudioToolbox;
using AVFoundation;
using BarcodeInspection.Helpers;
using BarcodeInspection.iOS.Enums;
using BarcodeInspection.iOS.Extensions;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreText;
using Foundation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UIKit;
namespace BarcodeInspection.iOS
{
    public partial class CameraViewController : UIViewController, IAVCaptureMetadataOutputObjectsDelegate
    {
        public event Action<string, string> OnScanCompleted;

        //개별 500밀리초
        SystemSound toneDoubleBeep = new SystemSound(1255); //경고음(더블음), 1255, 
        SystemSound toneBeep = new SystemSound(1071); //평소스캔(단일음)1256, 1071, 1072

        SystemSound doubleVibrator = new SystemSound(1011); //더블진동
        SystemSound vibrator = new SystemSound(4095); // 단일진동

        //mp3 Sound
        //SystemSound systemSoundBeep;
        //SystemSound systemSoundCaution;

        private AVAudioPlayer audioPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename("Sounds/beep07.mp3"));
        private AVAudioPlayer audioCautionPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename("Sounds/beep06.mp3"));

        private readonly AVCaptureMetadataOutput metadataOutput = new AVCaptureMetadataOutput();

        /// <summary>
        /// Communicate with the session and other session objects on this queue.
        /// </summary>
        private readonly DispatchQueue sessionQueue = new DispatchQueue("session queue");

        private readonly AVCaptureSession session = new AVCaptureSession();

        private SessionSetupResult setupResult = SessionSetupResult.Success;

        private AVCaptureDeviceInput videoDeviceInput;

        public AVCaptureDevice defaultVideoDevice;

        private bool isSessionRunning;

        private readonly List<MetadataObjectLayer> metadataObjectOverlayLayers = new List<MetadataObjectLayer>();

        private UITapGestureRecognizer openBarcodeURLGestureRecognizer;

        public static CGRect flashFrame;
        public static CGRect closeFrame = new CGRect(0, 0, 64, 64);
        public static CGRect sessionPresetsFrame;

        //----------------------------------------------
        public bool IsContinue;
        public bool IsFixed;
        public int rowPosition;
        public IList<string> AllScanBarcode; //현재 배송군에서 스캔해야 할 바코드 리스트
        public IList<string> ScanCompletedBarcode; //스캔 완료한 바코드 리스트
        public IList<string> SaveCompletedBarcode; //부분 저장 완료한 바코드 리스트
        //----------------------------------------------

        public CameraViewController(IList<string> allScanBarcode, IList<string> scanCompletedBarcode, IList<string> saveCompletedBarcode, bool isContinue, bool isFixed) : base("CameraViewController", null)
        {
            this.AllScanBarcode = allScanBarcode;
            this.ScanCompletedBarcode = scanCompletedBarcode;
            this.SaveCompletedBarcode = saveCompletedBarcode;
            this.IsContinue = isContinue;
            this.IsFixed = isFixed;
        }


        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        protected UITapGestureRecognizer OpenBarcodeURLGestureRecognizer
        {
            get
            {
                if (this.openBarcodeURLGestureRecognizer == null)
                {
                    this.openBarcodeURLGestureRecognizer = new UITapGestureRecognizer(this.OpenBarcodeUrl);
                }

                return this.openBarcodeURLGestureRecognizer;
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            //systemSoundBeep = new SystemSound(NSUrl.FromFilename("Sounds/beep07.mp3"));
            //systemSoundCaution = new SystemSound(NSUrl.FromFilename("Sounds/beep06.mp3"));

            //audioPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename("Sounds/beep07.mp3"));
            //audioCautionPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename("Sounds/beep06.mp3"));

            // Perform any additional setup after loading the view, typically from a nib.
            this.PreviewView.AddGestureRecognizer(this.OpenBarcodeURLGestureRecognizer);

            // Set up the video preview view.
            this.PreviewView.Session = session;

            // Check video authorization status. Video access is required and audio
            // access is optional. If audio access is denied, audio is not recorded
            // during movie recording.
            switch (AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video))
            {
                case AVAuthorizationStatus.Authorized:
                    // The user has previously granted access to the camera.
                    break;

                case AVAuthorizationStatus.NotDetermined:
                    // The user has not yet been presented with the option to grant
                    // video access. We suspend the session queue to delay session
                    // setup until the access request has completed.
                    this.sessionQueue.Suspend();
                    AVCaptureDevice.RequestAccessForMediaType(AVMediaType.Video, (granted) =>
                    {
                        if (!granted)
                        {
                            this.setupResult = SessionSetupResult.NotAuthorized;
                        }

                        this.sessionQueue.Resume();
                    });
                    break;

                default:
                    // The user has previously denied access.
                    this.setupResult = SessionSetupResult.NotAuthorized;
                    break;
            }

            // Setup the capture session.
            // In general it is not safe to mutate an AVCaptureSession or any of its
            // inputs, outputs, or connections from multiple threads at the same time.
            //
            // Why not do all of this on the main queue?
            // Because AVCaptureSession.StartRunning() is a blocking call which can
            // take a long time. We dispatch session setup to the sessionQueue so
            // that the main queue isn't blocked, which keeps the UI responsive.
            this.sessionQueue.DispatchAsync(this.ConfigureSession);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            this.sessionQueue.DispatchAsync(() =>
            {
                switch (this.setupResult)
                {
                    case SessionSetupResult.Success:
                        // Only setup observers and start the session running if setup succeeded.
                        this.AddObservers();
                        this.session.StartRunning();

                        this.isSessionRunning = session.Running;

                        break;

                    case SessionSetupResult.NotAuthorized:
                        DispatchQueue.MainQueue.DispatchAsync(() =>
                        {
                            var message = "AVCamBarcode doesn't have permission to use the camera, please change privacy settings";
                            var alertController = UIAlertController.Create("AVCamBarcode", message, UIAlertControllerStyle.Alert);
                            alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null));
                            alertController.AddAction(UIAlertAction.Create("Settings", UIAlertActionStyle.Default, action =>
                            {
                                UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString));
                            }));

                            this.PresentViewController(alertController, true, null);
                        });
                        break;

                    case SessionSetupResult.ConfigurationFailed:
                        DispatchQueue.MainQueue.DispatchAsync(() =>
                        {
                            var message = "Unable to capture media";
                            var alertController = UIAlertController.Create("AVCamBarcode", message, UIAlertControllerStyle.Alert);
                            alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null));

                            this.PresentViewController(alertController, true, null);
                        });
                        break;
                }
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            closeButton.Frame = closeFrame;
            //SessionPresetsButton.Frame = sessionPresetsFrame;

            //this.View.BringSubviewToFront(closeButton);
            //this.View.BringSubviewToFront(flashButton);

            //if (flashFrame.Width == 0)
            //{
            //    flashButton.Frame = new CGRect(this.View.Frame.Size.Width - 64, 0, 64, 64);
            //}
            //else
            //{
            //    flashButton.Frame = flashFrame;
            //}

            //flashButton = new UIButton(new CGRect(UIScreen.MainScreen.Bounds.Size.Width - 10 - 60, 10, 60, 60));
            //flashButton.Frame = new CGRect(this.View.Frame.Size.Width - 64, 0, 64, 64);
            //flashButton.SetImage(UIImage.FromBundle("flashbuttonoff.png"), UIControlState.Normal);
            //flashButton.SetImage(UIImage.FromBundle("flashbuttonon.png"), UIControlState.Selected);
            //flashButton.Selected = false;
            //flashButton.Hidden = false;
            //flashButton.SetBackgroundImage(null, UIControlState.Normal);
            //flashButton.SetBackgroundImage(null, UIControlState.Selected);

            flashButton.TouchUpInside += delegate
            {
                this.toggleTorch();
            };


            closeButton.TouchUpInside += delegate {
                //if (successCallback != null)
                //{
                //    successCallback.barcodeDetected(null);

                //}
                //else
                //{
                //    handleResult(null);
                //}

                OnScanCompleted?.Invoke(string.Empty, "EXIT");
                DismissViewController(true, null); //화면 종료
            };

            //if (flashFrame.Width == 0)
            //{
            //    flashButton.Frame = new CGRect(this.View.Frame.Size.Width - 64, 0, 64, 64);
            //}
            //else
            //{
            //    flashButton.Frame = flashFrame;
            //}

        }

        /// <summary>
        /// 화면 종료 될때 호출됨.
        /// </summary>
        /// <param name="animated"></param>
        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            this.sessionQueue.DispatchAsync(() =>
            {
                if (this.setupResult == SessionSetupResult.Success)
                {
                    this.session.StopRunning();
                    this.isSessionRunning = this.session.Running;
                    this.RemoveObservers();
                }
            });

            //OnScanCompleted?.Invoke(string.Empty, "EXIT");
            OnScanCompleted = null;
        }

        #region Session Management
        private readonly DispatchQueue metadataObjectsQueue = new DispatchQueue("metadata objects queue");
        private void ConfigureSession()
        {
            if (setupResult == SessionSetupResult.Success)
            {
                this.session.BeginConfiguration();

                // Add video input
                // Choose the back wide angle camera if available, otherwise default to the front wide angle camera
                defaultVideoDevice = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaType.Video, AVCaptureDevicePosition.Back) ??
                                                     AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaType.Video, AVCaptureDevicePosition.Front) ??
                                                     null;

                if (defaultVideoDevice == null)
                {
                    Console.WriteLine("Could not get video device");
                    this.setupResult = SessionSetupResult.ConfigurationFailed;
                    this.session.CommitConfiguration();
                    return;
                }

                NSError error;
                var videoDeviceInput = AVCaptureDeviceInput.FromDevice(defaultVideoDevice, out error);
                if (this.session.CanAddInput(videoDeviceInput))
                {
                    this.session.AddInput(videoDeviceInput);
                    this.videoDeviceInput = videoDeviceInput;

                    if (this.videoDeviceInput.Device.LockForConfiguration(out error))
                    {
                        if (this.videoDeviceInput.Device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                        {
                            this.videoDeviceInput.Device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                        }

                        if (this.videoDeviceInput.Device.AutoFocusRangeRestrictionSupported)
                        {
                            this.videoDeviceInput.Device.AutoFocusRangeRestriction = AVCaptureAutoFocusRangeRestriction.Near; //Near는 바코드 스캔용
                        }

                        if (this.videoDeviceInput.Device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                        {
                            this.videoDeviceInput.Device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                        }

                        //Console.WriteLine(this.videoDeviceInput.Device.ExposureMode.ToString());
                        this.videoDeviceInput.Device.UnlockForConfiguration();
                    }


                    this.session.SessionPreset = AVCaptureSession.Preset1920x1080;
                    //this.session.SessionPreset = AVCaptureSession.Preset1280x720;

                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        // Why are we dispatching this to the main queue?
                        // Because AVCaptureVideoPreviewLayer is the backing layer for PreviewView and UIView
                        // can only be manipulated on the main thread
                        // Note: As an exception to the above rule, it's not necessary to serialize video orientation changed
                        // on the AVCaptureVideoPreviewLayer's connection with other session manipulation
                        //
                        // Use the status bar orientation as the internal video orientation. Subsequent orientation changes are
                        // handled by CameraViewController.ViewWillTransition(to:with:).

                        var initialVideoOrientation = AVCaptureVideoOrientation.Portrait;
                        var statusBarOrientation = UIApplication.SharedApplication.StatusBarOrientation;
                        if (statusBarOrientation != UIInterfaceOrientation.Unknown)
                        {
                            AVCaptureVideoOrientation videoOrintation;
                            if (Enum.TryParse(statusBarOrientation.ToString(), out videoOrintation))
                            {
                                initialVideoOrientation = videoOrintation;
                            }
                        }

                        this.PreviewView.VideoPreviewLayer.Connection.VideoOrientation = initialVideoOrientation;
                    });
                }
                else if (error != null)
                {
                    Console.WriteLine($"Could not create video device input: {error}");
                    this.setupResult = SessionSetupResult.ConfigurationFailed;
                    this.session.CommitConfiguration();
                    return;
                }
                else
                {
                    Console.WriteLine("Could not add video device input to the session");
                    this.setupResult = SessionSetupResult.ConfigurationFailed;
                    this.session.CommitConfiguration();

                    return;
                }


                // Add metadata output
                if (this.session.CanAddOutput(metadataOutput))
                {
                    this.session.AddOutput(metadataOutput);

                    // Set this view controller as the delegate for metadata objects
                    this.metadataOutput.SetDelegate(this, this.metadataObjectsQueue);

                    AVMetadataObjectType supportBarcodeFormat = AVMetadataObjectType.None;

                    //hm.ji, 지원하는 바코드 지정, Defualt로 모든바코드
                    // this.metadataOutput.MetadataObjectTypes = this.metadataOutput.AvailableMetadataObjectTypes; // Use all metadata object types by default

                    string jsonList = Xamarin.Forms.Application.Current.Properties["SupportBarcodeFormat"].ToString();
                    List<Helpers.BarcodeFormat> tmpList = JsonConvert.DeserializeObject<List<Helpers.BarcodeFormat>>(jsonList);

                    supportBarcodeFormat = ConvertToIOS(tmpList);

                    this.metadataOutput.MetadataObjectTypes = supportBarcodeFormat;

                    //this.metadataOutput.MetadataObjectTypes = AVMetadataObjectType.AztecCode |
                    //                                          AVMetadataObjectType.Code128Code |
                    //                                          AVMetadataObjectType.Code39Code |
                    //                                          AVMetadataObjectType.Code39Mod43Code |
                    //                                          AVMetadataObjectType.Code93Code |
                    //                                          AVMetadataObjectType.EAN13Code |
                    //                                          AVMetadataObjectType.EAN8Code |
                    //                                          AVMetadataObjectType.PDF417Code |
                    //                                          AVMetadataObjectType.QRCode |
                    //                                          AVMetadataObjectType.UPCECode |
                    //                                          AVMetadataObjectType.Interleaved2of5Code |
                    //                                          AVMetadataObjectType.ITF14Code |
                    //                                          AVMetadataObjectType.DataMatrixCode;
                    // Set an initial rect of interest that is 80% of the views's shortest side
                    // and 25% of the longest side. This means that the region on interest will
                    // appear in the same spot regardless of whether the app starts in portrait
                    // or landscape

                    //var width = 0.7; //높이 기본값 : 0.25, 전체 사용하려면 1
                    //var height = 0.9; //넓이, 초기 값은 0.8
                    //var x = (1 - width) / 2;
                    //var y = (1 - height) / 2;
                    //var initialRectOfInterest = new CGRect(x + 0.01, y, width, height);

                    CGRect initialRectOfInterest;

                    if (Settings.ScanMode.Equals("FULL"))
                    {
                        var width = 1.0; //높이 기본값 : 0.25, 전체 사용하려면 1
                        var height = 1.0; //넓이, 초기 값은 0.8
                        var x = (1 - width) / 2;
                        var y = (1 - height) / 2;
                        initialRectOfInterest = new CGRect(x, y, width, height);
                    }
                    else if (Settings.ScanMode.Equals("WIDE"))
                    {
                        var width = 0.25; //높이 기본값 : 0.25, 전체 사용하려면 1
                        var height = 0.9; //넓이, 초기 값은 0.8
                        var x = (1 - width) / 2;
                        var y = (1 - height) / 2;
                        initialRectOfInterest = new CGRect(x, y, width, height);
                    }
                    else if (Settings.ScanMode.Equals("SPLIT"))
                    {
                        var width = 0.15;
                        var height = 1.0;
                        var x = (1 - width) / 2;
                        var y = (1 - height) / 2;
                        initialRectOfInterest = new CGRect(0.09, 0, width, height);
                    }
                    else
                    {
                        var width = 0.15;
                        var height = 1.0;
                        var x = (1 - width) / 2;
                        var y = (1 - height) / 2;
                        initialRectOfInterest = new CGRect(0.09, 0, width, height);
                    }

                    this.metadataOutput.RectOfInterest = initialRectOfInterest;

                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        var initialRegionOfInterest = this.PreviewView.VideoPreviewLayer.MapToLayerCoordinates(initialRectOfInterest);
                        this.PreviewView.SetRegionOfInterestWithProposedRegionOfInterest(initialRegionOfInterest);
                    });
                }
                else
                {
                    Console.WriteLine("Could not add metadata output to the session");
                    this.setupResult = SessionSetupResult.ConfigurationFailed;
                    this.session.CommitConfiguration();

                    return;
                }

                this.session.CommitConfiguration();
            }
        }
        #endregion

        public AVMetadataObjectType ConvertToIOS(List<Helpers.BarcodeFormat> barcodeFormat)
        {
            AVMetadataObjectType types = AVMetadataObjectType.None;

            var mapping = new Dictionary<Helpers.BarcodeFormat, AVMetadataObjectType>
            {
                { Helpers.BarcodeFormat.Code128, AVMetadataObjectType.Code128Code},
                { Helpers.BarcodeFormat.Code39, AVMetadataObjectType.Code39Code},
                { Helpers.BarcodeFormat.Code93, AVMetadataObjectType.Code93Code},
                //{ Helpers.BarcodeFormat.Codabar, AVMetadataObjectType.codbar}, //사용 중단되고 있음.
                { Helpers.BarcodeFormat.DataMatrix, AVMetadataObjectType.DataMatrixCode},
                { Helpers.BarcodeFormat.Ean13, AVMetadataObjectType.EAN13Code},
                { Helpers.BarcodeFormat.Ean8, AVMetadataObjectType.EAN8Code},
                { Helpers.BarcodeFormat.Itf, AVMetadataObjectType.Interleaved2of5Code},
                { Helpers.BarcodeFormat.QrCode, AVMetadataObjectType.QRCode},
                { Helpers.BarcodeFormat.UpcA, AVMetadataObjectType.EAN13Code}, //UpcA = Ean13
                { Helpers.BarcodeFormat.UpcE, AVMetadataObjectType.UPCECode},
                { Helpers.BarcodeFormat.Pdf417, AVMetadataObjectType.PDF417Code},
                { Helpers.BarcodeFormat.AztecCode, AVMetadataObjectType.AztecCode},
                { Helpers.BarcodeFormat.Itf14, AVMetadataObjectType.ITF14Code}
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


        #region Presets

        //partial void SelectSessionPreset(UIButton sender)
        //{
        //    var controller = new ItemSelectionViewController<NSString>(this,
        //                                                               SessionPresetItemSelectionIdentifier,
        //                                                               new List<NSString>(AvailableSessionPresets()),
        //                                                               new List<NSString> { this.session.SessionPreset },
        //                                                               false);

        //    this.PresentItemSelectionViewController(controller);
        //}

        //private NSString[] AvailableSessionPresets()
        //{
        //    return GetAllSessionPresets().Where(preset => session.CanSetSessionPreset(preset)).ToArray();
        //}

        //private static IEnumerable<NSString> GetAllSessionPresets()
        //{
        //    yield return AVCaptureSession.PresetPhoto;
        //    yield return AVCaptureSession.PresetLow;
        //    yield return AVCaptureSession.PresetMedium;
        //    yield return AVCaptureSession.PresetHigh;
        //    yield return AVCaptureSession.Preset352x288;
        //    yield return AVCaptureSession.Preset640x480;
        //    yield return AVCaptureSession.Preset1280x720;
        //    yield return AVCaptureSession.PresetiFrame960x540;
        //    yield return AVCaptureSession.PresetiFrame1280x720;
        //    yield return AVCaptureSession.Preset1920x1080;
        //    yield return AVCaptureSession.Preset3840x2160;
        //}

        #endregion


        #region KVO and Notifications

        private NSObject interruptionEndedNotificationToken;

        private NSObject wasInterruptedNotificationToken;

        private NSObject runtimeErrorNotificationToken;

        private IDisposable runningChangeToken;

        private void AddObservers()
        {
            this.runningChangeToken = this.session.AddObserver("running", NSKeyValueObservingOptions.New, this.OnRunningChanged);

            // Observe the previewView's regionOfInterest to update the AVCaptureMetadataOutput's
            // RectOfInterest when the user finishes resizing the region of interest.
            this.PreviewView.RegionOfInterestChanged += this.OnRegionOfInterestChanged;

            var notificationCenter = NSNotificationCenter.DefaultCenter;

            this.runtimeErrorNotificationToken = notificationCenter.AddObserver(AVCaptureSession.RuntimeErrorNotification, this.OnRuntimeErrorNotification, this.session);

            // A session can only run when the app is full screen. It will be interrupted
            // in a multi-app layout, introduced in iOS 9, see also the documentation of
            // AVCaptureSessionInterruptionReason.Add observers to handle these session
            // interruptions and show a preview is paused message.See the documentation
            // of AVCaptureSessionWasInterruptedNotification for other interruption reasons.
            this.wasInterruptedNotificationToken = notificationCenter.AddObserver(AVCaptureSession.WasInterruptedNotification, this.OnSessionWasInterrupted, this.session);
            this.interruptionEndedNotificationToken = notificationCenter.AddObserver(AVCaptureSession.InterruptionEndedNotification, this.OnSessionInterruptionEnded, this.session);
        }

        private void RemoveObservers()
        {
            this.runningChangeToken?.Dispose();
            this.runtimeErrorNotificationToken?.Dispose();
            this.wasInterruptedNotificationToken?.Dispose();
            this.interruptionEndedNotificationToken?.Dispose();
            this.PreviewView.RegionOfInterestChanged -= this.OnRegionOfInterestChanged;
        }

        private void OnRegionOfInterestChanged(object sender, EventArgs e)
        {
            var newRegion = (sender as PreviewView).RegionOfInterest;
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                // Ensure we are not drawing old metadata object overlays.
                this.RemoveMetadataObjectOverlayLayers();

                // Translate the preview view's region of interest to the metadata output's coordinate system.
                var metadataOutputRectOfInterest = this.PreviewView.VideoPreviewLayer.MapToMetadataOutputCoordinates(newRegion);

                this.sessionQueue.DispatchAsync(() =>
                {
                    // Update the AVCaptureMetadataOutput with the new region of interest
                    metadataOutput.RectOfInterest = metadataOutputRectOfInterest;
                });
            });
        }

        private void OnRunningChanged(NSObservedChange change)
        {
            var isSessionRunning = ((NSNumber)change.NewValue).BoolValue;

            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                //this.CameraButton.Enabled = isSessionRunning && AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video).Length > 1;
                //this.MetadataObjectTypesButton.Enabled = isSessionRunning;
                //this.SessionPresetsButton.Enabled = isSessionRunning;

                //this.ZoomSlider.Enabled = isSessionRunning;
                //this.ZoomSlider.MaxValue = (float)NMath.Min(this.videoDeviceInput.Device.ActiveFormat.VideoMaxZoomFactor, 8);
                //this.ZoomSlider.Value = (float)(this.videoDeviceInput.Device.VideoZoomFactor);

                // After the session stop running, remove the metadata object overlays,
                // if any, so that if the view appears again, the previously displayed
                // metadata object overlays are removed.
                if (!isSessionRunning)
                {
                    this.RemoveMetadataObjectOverlayLayers();
                }

                // When the session starts running, the aspect ration of the video preview may also change if a new session present was applied .
                // To keep the preview view's region of interest within the visible portion of the video preview, the preview view's region of 
                // interest will need to be updates.
                if (isSessionRunning)
                {
                    this.PreviewView.SetRegionOfInterestWithProposedRegionOfInterest(this.PreviewView.RegionOfInterest);
                }
            });
        }

        private void OnRuntimeErrorNotification(NSNotification notification)
        {
            var args = new AVCaptureSessionRuntimeErrorEventArgs(notification);
            if (args.Error != null)
            {
                var error = (AVError)(long)args.Error.Code;
                Console.WriteLine($"Capture session runtime error: {error}");

                // Automatically try to restart the session running if media services were
                // reset and the last start running succeeded. Otherwise, enable the user
                // to try to resume the session running.

                if (error == AVError.MediaServicesWereReset)
                {
                    this.sessionQueue.DispatchAsync(() =>
                    {
                        if (this.isSessionRunning)
                        {
                            this.session.StartRunning();
                            this.isSessionRunning = session.Running;
                        }
                    });
                }
            }
        }

        private void OnSessionWasInterrupted(NSNotification notification)
        {
            // In some scenarios we want to enable the user to resume the session running.
            // For example, if music playback is initiated via control center while
            // using AVMetadataRecordPlay, then the user can let AVMetadataRecordPlay resume
            // the session running, which will stop music playback. Note that stopping
            // music playback in control center will not automatically resume the session
            // running. Also note that it is not always possible to resume

            var reasonIntegerValue = ((NSNumber)notification.UserInfo[AVCaptureSession.InterruptionReasonKey]).Int32Value;
            var reason = (AVCaptureSessionInterruptionReason)reasonIntegerValue;
            Console.WriteLine($"Capture session was interrupted with reason {reason}");

            if (reason == AVCaptureSessionInterruptionReason.VideoDeviceNotAvailableWithMultipleForegroundApps)
            {
                // Simply fade-in a label to inform the user that the camera is unavailable.
                //this.CameraUnavailableLabel.Hidden = false;
                //this.CameraUnavailableLabel.Alpha = 0;
                //UIView.Animate(0.25d, () => this.CameraUnavailableLabel.Alpha = 1);
            }
        }

        private void OnSessionInterruptionEnded(NSNotification notification)
        {
            Console.WriteLine("Capture session interruption ended");

            //if (this.CameraUnavailableLabel.Hidden)
            //{
            //    UIView.Animate(0.25, () =>
            //    {
            //        this.CameraUnavailableLabel.Alpha = 0;
            //    }, () =>
            //    {
            //        this.CameraUnavailableLabel.Hidden = true;
            //    });
            //}
        }

        #endregion


        #region Drawing Metadata Object Overlay Layers

        private NSTimer removeMetadataObjectOverlayLayersTimer;

        //partial void SelectMetadataObjectTypes(UIButton sender)
        //{
        //    var controller = new ItemSelectionViewController<AVMetadataObjectType>(this,
        //                                                                           MetadataObjectTypeItemSelectionIdentifier,
        //                                                                           this.metadataOutput.AvailableMetadataObjectTypes.GetFlags().ToList(),
        //                                                                           this.metadataOutput.MetadataObjectTypes.GetFlags().ToList(),
        //                                                                           true);

        //    this.PresentItemSelectionViewController(controller);
        //}

        /// <summary>
        /// 2.스캔결과값 
        /// </summary>
        /// <param name="metadataObject"></param>
        /// <returns></returns>
        private MetadataObjectLayer CreateMetadataOverlay(AVMetadataObject metadataObject)
        {
            // Transform the metadata object so the bounds are updated to reflect those of the video preview layer.
            var transformedMetadataObject = this.PreviewView.VideoPreviewLayer.GetTransformedMetadataObject(metadataObject);

            // Create the initial metadata object overlay layer that can be used for either machine readable codes or faces.
            //빔 컬러
            var metadataObjectOverlayLayer = new MetadataObjectLayer
            {
                LineWidth = 3,
                LineJoin = CAShapeLayer.JoinRound,
                MetadataObject = transformedMetadataObject,
                FillColor = UIColor.Red.CGColor,
                StrokeColor = UIColor.Red.CGColor
                //FillColor = this.View.TintColor.ColorWithAlpha(0.3f).CGColor,
                //StrokeColor = this.View.TintColor.ColorWithAlpha(0.7f).CGColor
            };

            var barcodeMetadataObject = transformedMetadataObject as AVMetadataMachineReadableCodeObject;
            if (barcodeMetadataObject != null)
            {
                var barcodeOverlayPath = this.BarcodeOverlayPathWithCorners(barcodeMetadataObject.Corners);
                metadataObjectOverlayLayer.Path = barcodeOverlayPath;

                // If the metadata object has a string value, display it.
                string textLayerString = null;

                //스캔된 바코드 값
                //바코드 타입 : barcodeMetadataObject.Type
                if (!string.IsNullOrEmpty(barcodeMetadataObject.StringValue))
                {
                    textLayerString = barcodeMetadataObject.StringValue;
                }
                else
                {
                    // TODO: add Descriptor (line 618 in original iOS sample)
                }

                //스캔된 바코드 값
                if (!string.IsNullOrEmpty(textLayerString))
                {
                    Console.WriteLine("========Result========");
                    Console.WriteLine(barcodeMetadataObject.Type + ", " + barcodeMetadataObject.StringValue);

                    //this.PreviewView.customOverlay.tableSource.tableItems.Add(new TableItem { Heading = "heading", SubHeading = "subheading", ImageName = "barcode36x36.png" });

                    //this.InvokeOnMainThread(() => { this.PreviewView.customOverlay.RowAdd("aa", "bb"); });

                    var barcodeOverlayBoundingBox = barcodeOverlayPath.BoundingBox;

                    var font = UIFont.BoldSystemFontOfSize(16).ToCTFont();

                    var textLayer = new CATextLayer();
                    textLayer.TextAlignmentMode = CATextLayerAlignmentMode.Center;
                    //글자표시 박스, 높이가 작아서 글자 안보임 +50추가
                    textLayer.Bounds = new CGRect(0, 0, barcodeOverlayBoundingBox.Size.Width + 20, barcodeOverlayBoundingBox.Size.Height + 50);
                    //textLayer.Bounds = new CGRect(0, 0, 300, 100);
                    textLayer.ContentsScale = UIScreen.MainScreen.Scale;
                    textLayer.Position = new CGPoint(barcodeOverlayBoundingBox.GetMidX(), barcodeOverlayBoundingBox.GetMidY());
                    textLayer.Wrapped = true;
                    textLayer.Transform = CATransform3D.MakeFromAffine(this.PreviewView.Transform).Invert();

                    //-------------------------------------
                    //스캔된 바코드 biz logic
                    //-------------------------------------

                    //연속 스캔
                    if (this.IsContinue)
                    {
                        if (this.IsFixed)
                        {
                            if (this.AllScanBarcode.Contains(textLayerString))
                            {
                                //1. 저장 했는지?
                                if (this.SaveCompletedBarcode.Contains(textLayerString))
                                {
                                    //저장 완료
                                    //Color.Green
                                    textLayer.String = "저장 완료\n" + textLayerString;
                                    textLayer.AttributedString = new NSAttributedString(textLayer.String, new CTStringAttributes
                                    {
                                        Font = font,
                                        StrokeWidth = 0,
                                        StrokeColor = UIColor.Black.CGColor,
                                        ForegroundColor = UIColor.Green.CGColor
                                    });

                                    //경고
                                    audioCautionPlayer.Play();
                                    doubleVibrator.PlaySystemSoundAsync();
                                }
                                //2. Scan 완료 했는지?
                                else if (this.ScanCompletedBarcode.Contains(textLayerString))
                                {
                                    //스캔 완료
                                    //Color.Yellow
                                    textLayer.String = "스캔 완료\n" + textLayerString;
                                    textLayer.AttributedString = new NSAttributedString(textLayer.String, new CTStringAttributes
                                    {
                                        Font = font,
                                        StrokeWidth = 0,
                                        StrokeColor = UIColor.Black.CGColor,
                                        ForegroundColor = UIColor.Yellow.CGColor
                                    });

                                    //경고
                                    audioCautionPlayer.Play();
                                    doubleVibrator.PlaySystemSoundAsync();
                                }
                                else
                                {
                                    //------------
                                    //정상처리 작업
                                    //------------
                                    OnScanCompleted?.Invoke(barcodeMetadataObject.Type.ToString(), textLayerString);

                                    textLayer.String = textLayerString;
                                    textLayer.AttributedString = new NSAttributedString(textLayer.String, new CTStringAttributes
                                    {
                                        Font = font,
                                        StrokeWidth = 0,
                                        StrokeColor = UIColor.Black.CGColor,
                                        ForegroundColor = UIColor.White.CGColor
                                    });

                                    //정상
                                    audioPlayer.Play();
                                    SystemSound.Vibrate.PlaySystemSoundAsync();

                                    if (!this.ScanCompletedBarcode.Contains(textLayerString))
                                    {
                                        this.ScanCompletedBarcode.Add(textLayerString);
                                    }

                                    if (this.AllScanBarcode.Count == this.SaveCompletedBarcode.Count + this.ScanCompletedBarcode.Count)
                                    {
                                        this.session.StopRunning();

                                        OnScanCompleted?.Invoke(string.Empty, "EXIT");
                                        DismissViewController(true, null); //화면 종료
                                    }
                                    else
                                    {
                                        //연속스캔 사이의 간격 지정
                                        //이 함수 호출하는 부분에서 처리
                                    }
                                }
                            }
                            else
                            {
                                //스캔 대상X
                                //Color.Red
                                textLayer.String = "스캔 대상X\n" + textLayerString;
                                textLayer.AttributedString = new NSAttributedString(textLayer.String, new CTStringAttributes
                                {
                                    Font = font,
                                    StrokeWidth = 0,
                                    StrokeColor = UIColor.Black.CGColor,
                                    ForegroundColor = UIColor.Red.CGColor
                                });

                                //경고
                                audioCautionPlayer.Play();
                                doubleVibrator.PlaySystemSoundAsync();
                            }
                        }
                        //비고정(스캔 대상 없음)
                        else
                        {
                            //현재로서는 biz로직 없음


                        }
                    }
                    //단일 스캔
                    else
                    {
                        this.session.StopRunning();

                        textLayer.String = textLayerString;
                        textLayer.AttributedString = new NSAttributedString(textLayer.String, new CTStringAttributes
                        {
                            Font = font,
                            StrokeWidth = 0,
                            StrokeColor = UIColor.Black.CGColor,
                            ForegroundColor = UIColor.White.CGColor
                        });

                        //정상
                        audioPlayer.Play();
                        SystemSound.Vibrate.PlaySystemSoundAsync();

                        OnScanCompleted?.Invoke(barcodeMetadataObject.Type.ToString(), textLayerString);
                        DismissViewController(true, null); //화면 종료
                    }

                    //화면에 표시
                    textLayer.SetFont(font);
                    metadataObjectOverlayLayer.AddSublayer(textLayer);
                }
            }
            else if (transformedMetadataObject is AVMetadataFaceObject)
            {
                metadataObjectOverlayLayer.Path = CGPath.FromRect(transformedMetadataObject.Bounds);
            }

            return metadataObjectOverlayLayer;
        }

        private CGPath BarcodeOverlayPathWithCorners(CGPoint[] corners)
        {
            var path = new CGPath();

            if (corners.Any())
            {
                path.MoveToPoint(CGAffineTransform.MakeIdentity(), corners[0]);

                for (int i = 1; i < corners.Length; i++)
                {
                    path.AddLineToPoint(corners[i]);
                }

                path.CloseSubpath();
            }

            return path;
        }

        private void RemoveMetadataObjectOverlayLayers()
        {
            this.metadataObjectOverlayLayers.ForEach(layer => layer.RemoveFromSuperLayer());
            this.metadataObjectOverlayLayers.Clear();

            this.removeMetadataObjectOverlayLayersTimer?.Invalidate();
            this.removeMetadataObjectOverlayLayersTimer = null;
        }

        private void AddMetadataOverlayLayers(IEnumerable<MetadataObjectLayer> layers)
        {
            // Add the metadata object overlays as sublayers of the video preview layer. We disable actions to allow for fast drawing.
            CATransaction.Begin();
            CATransaction.DisableActions = true;

            foreach (var layer in layers)
            {
                this.PreviewView.VideoPreviewLayer.AddSublayer(layer);
                this.metadataObjectOverlayLayers.Add(layer); // Save the new metadata object overlays.
            }

            CATransaction.Commit();

            // Create a timer to destroy the metadata object overlays.
            // 화면에 글자가 보여지는 시간
            this.removeMetadataObjectOverlayLayersTimer = NSTimer.CreateScheduledTimer(TimeSpan.FromMilliseconds(1000), (param) => this.RemoveMetadataObjectOverlayLayers());

            //★★★ 연속스캔 간격은 여기서 지정한다.
            Thread.Sleep(1000);

            //Task.Delay(1000).Wait();
        }

        private void OpenBarcodeUrl(UITapGestureRecognizer openBarcodeURLGestureRecognizer)
        {
            foreach (var metadataObjectOverlayLayer in this.metadataObjectOverlayLayers)
            {
                var location = openBarcodeURLGestureRecognizer.LocationInView(this.PreviewView);
                if (metadataObjectOverlayLayer.Path.ContainsPoint(location, false))
                {
                    var barcodeMetadataObject = metadataObjectOverlayLayer.MetadataObject as AVMetadataMachineReadableCodeObject;
                    if (barcodeMetadataObject != null)
                    {
                        if (!string.IsNullOrEmpty(barcodeMetadataObject.StringValue))
                        {
                            var url = NSUrl.FromString(barcodeMetadataObject.StringValue);
                            if (UIApplication.SharedApplication.CanOpenUrl(url))
                            {
                                UIApplication.SharedApplication.OpenUrl(url);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        private readonly AutoResetEvent resetEvent = new AutoResetEvent(true);

        /// <summary>
        ///  1.스캔 완료 되면 이곳으로 가장 먼저 온다.
        /// </summary>
        /// <param name="captureOutput"></param>
        /// <param name="metadataObjects"></param>
        /// <param name="connection"></param>
        [Export("captureOutput:didOutputMetadataObjects:fromConnection:")]
        public void DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
        {
            // resetEvent is used to drop new notifications if old ones are still processing, to avoid queuing up a bunch of stale data.
            //★★★, 20180831, hm.ji, 연속 스캔하기 위해서는 아래 값을 0으로 반듯이 해야 한다.
            if (this.resetEvent.WaitOne(0))
            {
                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    this.RemoveMetadataObjectOverlayLayers();
                    this.AddMetadataOverlayLayers(metadataObjects.Select(this.CreateMetadataOverlay));

                    //OnScanCompleted?.Invoke("EXIT");
                    //DismissViewController(true, null);

                    //if (this.AllScanBarcode.Count == this.SaveCompletedBarcode.Count + this.ScanCompletedBarcode.Count)
                    //{
                    //    Task.Delay(500).Wait();
                    //}
                    //else
                    //{
                    //    //연속스캔 사이의 간격 지정 
                    //Task.Delay(1000).ContinueWith((t) => resetEvent.Set());
                    //Thread.Sleep(1000);
                    //}
                    //Task.Delay(1000).Wait();

                    resetEvent.Set();
                });
            }
        }

        private const string MetadataObjectTypeItemSelectionIdentifier = "MetadataObjectTypes";

        private const string SessionPresetItemSelectionIdentifier = "SessionPreset";

        public void toggleTorch()
        {
            if (this.defaultVideoDevice.IsTorchModeSupported(AVCaptureTorchMode.On))
            {
                NSError error = new NSError();

                if (this.defaultVideoDevice.LockForConfiguration(out error))
                {
                    if (this.defaultVideoDevice.TorchMode == AVCaptureTorchMode.On)
                    {
                        this.defaultVideoDevice.TorchMode = AVCaptureTorchMode.Off;
                        flashButton.Selected = false;
                    }
                    else
                    {
                        this.defaultVideoDevice.TorchMode = AVCaptureTorchMode.On;
                        flashButton.Selected = true;
                    }

                    //if (this.defaultVideoDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    //    this.defaultVideoDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;

                    this.defaultVideoDevice.UnlockForConfiguration();
                }
                else
                {

                }

                //this.PreviewView.customOverlay.RowAdd("aa", "bb");
                //this.InvokeOnMainThread(() => { this.PreviewView.customOverlay.RowAdd("aa", "bb"); });
            }
        }
    }
}