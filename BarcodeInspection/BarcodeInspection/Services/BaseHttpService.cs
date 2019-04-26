using BarcodeInspection.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BarcodeInspection.Services
{
    public class BaseHttpService
    {
        private static readonly BaseHttpService instance = new BaseHttpService();

        private readonly string GET_CONNECT_URL = GlobalSetting.Instance.MobileGetEndpoint.ToString();
        private readonly string SET_CONNECT_URL = GlobalSetting.Instance.MobileSetEndpoint.ToString();
        private readonly string CONNECT_URL_AUTH = GlobalSetting.Instance.MobileAuthEndpoint.ToString();
        private string CONNECT_URL = string.Empty;

        public static BaseHttpService Instance
        {
            get
            {
                return instance;
            }
        }

        public async Task<string> SendRequestAsync(HttpCommand httpCommand, object requestData = null)
        {
            string result = string.Empty;


            // Default to GET
            //var method = httpMethod ?? HttpMethod.Get;
            if (httpCommand.Equals(HttpCommand.GET))
            {
                CONNECT_URL = GET_CONNECT_URL;
            }
            else
            {
                CONNECT_URL = SET_CONNECT_URL;
            }

            // Serialize request data
            var data = requestData == null
                ? null
                : JsonConvert.SerializeObject(requestData);

#if DEBUG
            if (!Connectivity.ConnectionProfiles.Contains(ConnectionProfile.WiFi))
            {
                // Active Wi-Fi connection.
                return "ERROR-Wifi 연결 오류\n다시 처리해 주세요.";
            }
#else
            if (!Connectivity.ConnectionProfiles.Contains(ConnectionProfile.Cellular)) //라이브
            {
                // Active Wi-Fi connection.
                return "ERROR-인터넷 연결 오류\n다시 처리해 주세요.";
            }
#endif

             
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(CONNECT_URL)))
                {
                    if (data != null)
                    {
                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                    }

                    // Add headers to request
                    //if (headers != null)
                    //{
                    //    foreach (var h in headers)
                    //    {
                    //        request.Headers.Add(h.Key, h.Value);
                    //    }
                    //}

                    using (var handler = new HttpClientHandler())
                    {
                        using (var client = new HttpClient(handler))
                        {
                            Console.WriteLine(client.Timeout.ToString());
                            //client.Timeout = new TimeSpan(0, 3, 0); //3분
                            //using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
                            using (var response = await client.SendAsync(request))
                            {
                                var content = response.Content == null
                                    ? null
                                    : await response.Content.ReadAsStringAsync();

                                if (response.IsSuccessStatusCode)
                                {
                                    //result = JsonConvert.DeserializeObject<T>(content);
                                    result = content;
                                }
                            }

                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>
                {
                    {"BaseHttpService", "SendRequestAsync" },
                    {"UserID", Settings.Userid}
                };
                //Crashes.TrackError(ex, properties);

                return "ERROR " + ex.Message.ToString();
            }
        }

        public async Task<string> AuthorizationAsync(string jsonString)
        {
            HttpResponseMessage response = null;
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(CONNECT_URL_AUTH.ToString());
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //client.Timeout = new TimeSpan(0, 3, 0); //3분

                    using (var request = new HttpRequestMessage())
                    {
                        //request.RequestUri = new Uri(CONNECT_URL_AUTH.ToString());
                        request.Method = HttpMethod.Post;
                        //request.Headers.Add("id", id);
                        //request.Headers.Add("pw", pw);

                        //request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", id, pw))));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(jsonString)));

                        response = await client.SendAsync(request);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR " + ex.Message.ToString();
            }
            finally
            {
                if (response != null) response.Dispose();
            }
        }

        public async Task<string> GetRequestAsync(string requestParam)
        {
            HttpContent content = new StringContent(requestParam, Encoding.UTF8, "application/json");

            HttpResponseMessage response = null;
            try
            {
                using (var client = new HttpClient())
                {
                    //client.BaseAddress = new Uri(CONNECT_URL_GET.ToString());
                    client.Timeout = new TimeSpan(0, 3, 0); //3분

                    using (var request = new HttpRequestMessage())
                    {
                        //request.RequestUri = new Uri(CONNECT_URL_GET.ToString());
                        request.RequestUri = new Uri(GET_CONNECT_URL.ToString());
                        request.Method = HttpMethod.Post;
                        //2019.04.23 임시 주석 request.Headers.Add("jwt", Global.token);
                        request.Headers.Add("jwt", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJTQ09QRSI6IklOQk9VTkR8T1VUQk9VTkR8UFVUQVdBWXxJTlZFTlRPUlkiLCJpc3MiOiJiY3dtcyIsIlNDT1BFMiI6IklOQk9VTkR8T1VUQk9VTkR8UFVUQVdBWSIsImV4cCI6MTUxNzYzOTQzMH0.mKAIxCGMyreLY0D5GIWgaMocU3vqqPRGGWcjf_o_79FZ1kRz4CUWXMQv5OSBzm_gwg8_GE7u4Khq3FC6ZwTIMfQpkotMV5SJYyMounerBRep2dyZlldWoL6HFozLLa_2yerAhsbNGHTCfmPuridEqR7E85tG70vumBn70sZR0fc");
                        request.Content = content;

                        response = await client.SendAsync(request);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR " + ex.Message.ToString();
            }
            finally
            {
                if (response != null) response.Dispose();
            }
        }

        public async Task<string> SetRequestAsync(string requestParam)
        {
            //var postData = new List<KeyValuePair<string, string>>();
            //if (!string.IsNullOrEmpty(requestParam))
            //{
            //    postData.Add(new KeyValuePair<string, string>("requestParam", requestParam));
            //}
            //HttpContent content = new FormUrlEncodedContent(postData); //url길이 제한

            //How to #2
            HttpContent content = new StringContent(requestParam, Encoding.UTF8, "application/json");

            HttpResponseMessage response = null;
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(SET_CONNECT_URL.ToString());
                    client.Timeout = new TimeSpan(0, 3, 0); //3분

                    using (var request = new HttpRequestMessage())
                    {
                        //request.RequestUri = new Uri(CONNECT_URL_SET.ToString());
                        request.Method = HttpMethod.Post;
                        //2019.04.23 임시 주석 request.Headers.Add("jwt", Global.token);
                        request.Headers.Add("jwt", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJTQ09QRSI6IklOQk9VTkR8T1VUQk9VTkR8UFVUQVdBWXxJTlZFTlRPUlkiLCJpc3MiOiJiY3dtcyIsIlNDT1BFMiI6IklOQk9VTkR8T1VUQk9VTkR8UFVUQVdBWSIsImV4cCI6MTUxNzYzOTQzMH0.mKAIxCGMyreLY0D5GIWgaMocU3vqqPRGGWcjf_o_79FZ1kRz4CUWXMQv5OSBzm_gwg8_GE7u4Khq3FC6ZwTIMfQpkotMV5SJYyMounerBRep2dyZlldWoL6HFozLLa_2yerAhsbNGHTCfmPuridEqR7E85tG70vumBn70sZR0fc");
                        request.Content = content;

                        response = await client.SendAsync(request);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR " + ex.Message.ToString();
            }
            finally
            {
                if (response != null) response.Dispose();
            }
        }
    }
}
