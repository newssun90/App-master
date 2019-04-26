using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Content;
using Android.Widget;
using Java.IO;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BarcodeInspection.Droid
{
    [Activity(Label = "자동 업데이트", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class AutoUpdateActivity : Activity
    {
        public static event Action OnUpdateCompleted;

        private ProgressBar progressBar;
        private ProgressBar progressBar2;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.AutoUpdateLayout);

            // Create your application here            
            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            progressBar2 = FindViewById<ProgressBar>(Resource.Id.progressBar2);

            progressBar.Max = 100;
            progressBar2.Max = 100;

            FileDelete();

            //CheckUpdate();

            var url = new System.Uri(string.Format(@"{0}{1}", GlobalSetting.Instance.MOBILEEndpoint.ToString(), @"/com.daesangit.barcodeinspection.apk"));

            Task task = Task.Factory.StartNew(async () =>
            {
                await DownloadFileAsync(url);

                var child = Task.Factory.StartNew(() =>
                {
                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
                    {
                        Intent intent = new Intent(Intent.ActionInstallPackage);
                        intent.SetDataAndType(FileProvider.GetUriForFile(this.ApplicationContext, "com.daesangit.barcodeinspection.fileprovider", new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.daesangit.barcodeinspection.apk")), "application/vnd.android.package-archive");
                        intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                        StartActivity(intent);
                    }
                    else
                    {
                        Intent intent = new Intent(Intent.ActionView);
                        intent.SetDataAndType(Android.Net.Uri.FromFile(new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.daesangit.barcodeinspection.apk")), "application/vnd.android.package-archive");
                        intent.SetFlags(ActivityFlags.NewTask);
                        StartActivity(intent);
                    }
                }, TaskCreationOptions.AttachedToParent);

                var third = Task.Factory.StartNew(() =>
                {
                    OnUpdateCompleted?.Invoke();
                    this.Finish(); 
                }, TaskCreationOptions.AttachedToParent);
            });

            task.Wait();

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnUpdateCompleted = null;
        }

        private void FileDelete()
        {
            File file = new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString());
            IFilenameFilter filter = new AutoUpdateFileFilter();
            File[] files = file.ListFiles(filter);

            if (files != null && files.Length > 0)
            {
                foreach (var item in files)
                {
                    if (item.IsFile)
                    {
                        if (item.Name.ToString().StartsWith("com.daesangit.barcodeinspection"))
                        {
                            using (File fileDel = new File(item.ToString()))
                            {
                                if (fileDel.Exists())
                                {
                                    fileDel.Delete();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckUpdate()
        {
            var ad = new AlertDialog.Builder(this).Create();
            try
            {
                var webClient = new WebClient();
                var url = new System.Uri(string.Format(@"{0}{1}", GlobalSetting.Instance.MOBILEEndpoint.ToString(), @"/com.gwise.voidbarcode.apk"));
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                webClient.DownloadFileAsync(url, Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.gwise.voidbarcode.apk");
                webClient.DownloadFileCompleted += (s, e) =>
                {
                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
                    {
                        Intent intent = new Intent(Intent.ActionInstallPackage);
                        intent.SetDataAndType(FileProvider.GetUriForFile(this.ApplicationContext, "com.gwise.voidbarcode.fileprovider", new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.gwise.voidbarcode.apk")), "application/vnd.android.package-archive");
                        //intent.SetData(FileProvider.GetUriForFile(this.ApplicationContext, "com.gwise.voidbarcode.fileprovider", new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.gwise.voidbarcode.apk")));
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                        StartActivity(intent);
                    }
                    else
                    {
                        Intent intent = new Intent(Intent.ActionView);
                        intent.SetDataAndType(Android.Net.Uri.FromFile(new File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.gwise.voidbarcode.apk")), "application/vnd.android.package-archive");
                        intent.AddFlags(ActivityFlags.NewTask); // ActivityFlags.NewTask 이 옵션을 지정해 주어야 업데이트 완료 후에 [열기]라는 화면이 나온다.
                        StartActivity(intent);
                    }

                    OnUpdateCompleted?.Invoke();
                    this.Finish();
                };
            }
            catch (System.Exception ex)
            {
                ad = new AlertDialog.Builder(this).Create();
                ad.SetTitle("INFO");
                ad.SetMessage(ex.Message);
                ad.SetCanceledOnTouchOutside(true);
                ad.Show();
            }
            finally
            {
            }
        }

        public async Task DownloadFileAsync(Uri url)
        {
            var _client = new HttpClient();
            try
            {
                // Step 1 : Get call
                var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));
                }

                // Step 2 : Filename
                //var fileName = response.Content.Headers?.ContentDisposition?.FileName ?? "XXX.zip";

                // Step 3 : Get total of data
                var totalData = response.Content.Headers.ContentLength.GetValueOrDefault(-1L);
                var canSendProgress = totalData != -1L;

                // Step 4 : Get total of data
                var filePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads) + "/com.daesangit.barcodeinspection.apk";

                // Step 5 : Download data
                using (var fileStream = OpenStream(filePath))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var totalRead = 0L;
                        var buffer = new byte[4095];
                        var isMoreDataToRead = true;

                        do
                        {
                            //token.ThrowIfCancellationRequested();
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreDataToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;

                                if (canSendProgress)
                                {
                                    progressBar.SetProgress(Convert.ToInt32((totalRead * 1d) / (totalData * 1d) * 100), true);
                                    progressBar2.SetProgress(Convert.ToInt32((totalRead * 1d) / (totalData * 1d) * 100), true);
                                }
                            }
                        } while (isMoreDataToRead);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private System.IO.Stream OpenStream(string path)
        {
            return new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.None, 4095);
        }

        private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            System.Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...",
                 (string)e.UserState,
                 e.BytesReceived,
                 e.TotalBytesToReceive,
                 e.ProgressPercentage);

            int length = Convert.ToInt32(e.TotalBytesToReceive.ToString());
            int prog = Convert.ToInt32(e.BytesReceived.ToString());
            int perc = Convert.ToInt32(e.ProgressPercentage.ToString());

            try
            {
                progressBar.Progress = perc;
                progressBar.TooltipText = perc.ToString();

                progressBar2.Progress = perc;
                progressBar2.TooltipText = perc.ToString();
            }
            finally
            {

            }
        }
    }

    public class AutoUpdateFileFilter : Java.Lang.Object, IFilenameFilter
    {
        public bool Accept(File dir, string name)
        {
            if (name.StartsWith("com.gwise.voidbarcode") && name.EndsWith(".apk"))
            {
                return true;
            }
            return false;
        }
    }
}