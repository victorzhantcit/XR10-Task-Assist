using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;
using User.Core;
using User.Dtos;

namespace Unity.Extensions
{
    public enum HttpMethod
    {
        GET,
        POST,
        PUT // 添加 PUT 方法
    }

    public static class APIHelper
    {
        // 提供外部取得 API Log 訊息
        public static event Action<string, bool> OnAPILogCallback;

        // 儲存 ServerIP -> LoginAPI,Token 的對應關係
        private static Dictionary<string, ServerCacheData> _serverCacheMap = new Dictionary<string, ServerCacheData>();

        /// <summary>
        /// 註冊 Web API 資訊
        /// </summary>
        /// <param name="loginAPI">Web API 的登入 API</param>
        /// <param name="token">從登入 API 取得的 Token</param>
        public static void RegisterLoginAPI(string loginAPI, string token = null)
        {
            string serverIP = GetPrefix(loginAPI);
            _serverCacheMap[serverIP] = new ServerCacheData(serverIP, loginAPI, token);
        }

        /// <summary>
        /// 取消註冊指定的 Web API 資訊
        /// </summary>
        /// <param name="relatedUrl">指定的 Web API Url</param>
        public static void UnregisterLoginAPI(string relatedUrl)
        {
            string serverIP = GetPrefix(relatedUrl);
            if (_serverCacheMap.TryGetValue(serverIP, out ServerCacheData data))
                _serverCacheMap.Remove(serverIP);
        }

        /// <summary>
        /// 取消註冊所有 Web API 資訊
        /// </summary>
        public static void UnregisterAllLoginAPI()
        {
            _serverCacheMap.Clear();
        }

        /// <summary>
        /// 設定 Web API 使用的 Token
        /// </summary>
        /// <param name="relatedUrl">Web API Url</param>
        /// <param name="newToken">新的 Token</param>
        /// <exception cref="Exception"></exception>
        public static void SetAPIToken(string relatedUrl, string newToken)
        {
            string serverIP = GetPrefix(relatedUrl);
            if (_serverCacheMap.TryGetValue(serverIP, out ServerCacheData data))
                data.Token = newToken;
            throw new Exception("Only registered server can refresh token");
        }

        /// <summary>
        /// 從 Url 取得已註冊的 Web API 資訊
        /// </summary>
        /// <param name="relatedUrl">Web API Url</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ServerCacheData GetCacheData(string relatedUrl)
        {
            string serverIP = GetPrefix(relatedUrl);
            if (_serverCacheMap.TryGetValue(serverIP, out ServerCacheData data))
                return data;
            throw new Exception("Detect un-registered server: " + serverIP + " source: " + relatedUrl);
        }

        /// <summary>
        /// 取得 Url 中，IP PORT 等資訊
        /// </summary>
        /// <param name="relatedUrl">Web API Url</param>
        /// <returns></returns>
        private static string GetPrefix(string relatedUrl)
        {
            Uri uri = new Uri(relatedUrl);
            // 若 port 是預設值（http: 80, https: 443），就省略
            if ((uri.Scheme == "http" && uri.Port == 80) || (uri.Scheme == "https" && uri.Port == 443))
                return $"{uri.Scheme}://{uri.Host}";
            else
                return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }

        /// <summary>
        /// 創建基本的 Json Request Header
        /// </summary>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="content">傳送內容</param>
        /// <param name="token">使用的 Bearer Token</param>
        /// <returns></returns>
        private static UnityWebRequest CreateJsonRequest(string url, HttpMethod method, string content, string token)
        {
            UnityWebRequest request;

            if (method == HttpMethod.POST || method == HttpMethod.PUT)
            {
                request = new UnityWebRequest(url, method == HttpMethod.POST ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbPUT);
                if (!string.IsNullOrEmpty(content))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(content);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
                }
            }
            else
            {
                request = UnityWebRequest.Get($"{url}?{content}");
            }

            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"bearer {token}");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// 創建基本的 multipart/form-data Request Header
        /// </summary>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="formFields">傳入 Key-Value List，以轉換為 form-data 格式</param>
        /// <param name="token">使用的 Bearer Token</param>
        /// <returns></returns>
        public static UnityWebRequest CreateMultipartRequest(string url, HttpMethod method, List<KeyValue> formFields, string token)
        {
            UnityWebRequest request;

            if (method == HttpMethod.GET)
            {
                // GET 請求：將 formFields 的內容拼接到 URL 查詢字串中（處理空內容）
                string query = formFields != null && formFields.Count > 0
                    ? string.Join("&", formFields.Select(f => $"{f.Key}={UnityWebRequest.EscapeURL(f.Value)}"))
                    : string.Empty;

                string urlWithQuery = url + (string.IsNullOrEmpty(query) ? string.Empty : $"?{query}");
                request = UnityWebRequest.Get(urlWithQuery);
            }
            else
            {
                // 動態生成 boundary
                string boundary = "----UnityMultipartBoundary" + System.DateTime.Now.Ticks.ToString("x");
                StringBuilder multipartContent = new StringBuilder();

                // 添加表單字段部分
                if (formFields != null)
                {
                    foreach (var field in formFields)
                    {
                        multipartContent.AppendLine($"--{boundary}");
                        multipartContent.AppendLine($"Content-Disposition: form-data; name=\"{field.Key}\"");
                        multipartContent.AppendLine();
                        multipartContent.AppendLine(field.Value);
                    }
                }

                // 結尾 boundary
                multipartContent.AppendLine($"--{boundary}--");

                // 將內容轉為字節數組
                byte[] bodyRaw = Encoding.UTF8.GetBytes(multipartContent.ToString());

                // POST 和 PUT 請求：將內容放入 Body
                request = new UnityWebRequest(url, method == HttpMethod.POST ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbPUT);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");
            }

            // 添加 Authorization 標頭（如果需要）
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// 發送 UnityWebRequest 並等待回應
        /// </summary>
        /// <param name="request"></param>
        /// <param name="debugUrl"></param>
        /// <returns></returns>
        private static Task<UnityWebRequest> SendWebRequestAsync(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();

            var operation = request.SendWebRequest();
            operation.completed += _ => tcs.SetResult(request);

            return tcs.Task;
        }

        /// <summary>
        /// 發送格式為 Json 的 Web API 
        /// </summary>
        /// <typeparam name="TResponse">回應格式</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="data">Json Data</param>
        /// <param name="token">Web API 使用的憑證</param>
        /// <param name="returnPureString">後端是否回傳純字串 (response body 不帶引號)</param>
        /// <returns></returns>
        public static async Task<ResponseResult<TResponse>> SendJsonRequestAsync<TResponse>(
            string url,
            HttpMethod method,
            string data = null,
            string token = null,
            bool returnPureString = false,
            bool returnHttpStatus = false) => await SendRequest<TResponse>(url, method, jsonData: data, formFields: null, token, isJson: true, returnPureString, returnHttpStatus);

        /// <summary>
        /// 發送格式為 multipart/form-data 的 Web API 
        /// </summary>
        /// <typeparam name="TResponse">回應格式</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="data">multipart/form-data Data</param>
        /// <param name="token">Web API 使用的憑證</param>
        /// <param name="returnPureString">後端是否回傳純字串 (response body 不帶引號)</param>
        /// <returns></returns>
        public static async Task<ResponseResult<TResponse>> SendFormRequestAsync<TResponse>(
            string url,
            HttpMethod method,
            List<KeyValue> data = null,
            string token = null,
            bool returnPureString = false,
            bool returnHttpStatus = false) => await SendRequest<TResponse>(url, method, jsonData: null, formFields: data, token, isJson: false, returnPureString, returnHttpStatus);

        /// <summary>
        /// 將 Request 整理後，發送請求至後端並取得回應
        /// </summary>
        /// <typeparam name="TResponse">回應格式</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="jsonData">Json 格式的資料</param>
        /// <param name="formFields">Multipart/form-data 格式的資料</param>
        /// <param name="token">Web API 的憑證</param>
        /// <param name="isJson">是否採用 Json 格式的資料送出，若否則採用 multipart/form-data</param>
        /// <param name="returnPureString">後端是否回傳純字串 (response body 不帶引號)</param>
        /// <returns></returns>
        private static async Task<ResponseResult<TResponse>> SendRequest<TResponse>(
            string url,
            HttpMethod method,
            string jsonData = null,
            List<KeyValue> formFields = null,
            string token = null,
            bool isJson = true,
            bool returnPureString = false,
            bool returnHttpStatus = false)
        {
            UnityWebRequest request;
            string debugContent = string.Empty;

            if (isJson)
            {
                // JSON 傳輸
                request = CreateJsonRequest(url, method, jsonData, token);
                debugContent = jsonData;
            }
            else
            {
                // Form 傳輸
                request = CreateMultipartRequest(url, method, formFields, token);
                debugContent = formFields != null
                    ? string.Join("\n", formFields.Select(f => f.Key + " " + f.Value))
                    : "<formFields is null>";
            }

            var result = new ResponseResult<TResponse>();

            try
            {
                // Log
                OnAPILogCallback?.Invoke(
                    $"{method.ToString()} {url}\n" +
                    $"Content Type: {request.GetRequestHeader("Content-Type")}\n" +
                    $"Token: {!string.IsNullOrEmpty(request.GetRequestHeader("Authorization"))}\n" +
                    $"Content:\n{debugContent}",
                    false
                );

                var response = await SendWebRequestAsync(request);

                result.StatusCode = (int)response.responseCode;
                result.RawResponse = response.downloadHandler.text;

                if (response.result == UnityWebRequest.Result.ConnectionError || response.result == UnityWebRequest.Result.ProtocolError)
                {
                    result.ErrorMessage = $"HTTP Error {response.responseCode}: {response.error}";
                    return result;
                }

                try
                {
                    if (returnHttpStatus)
                    {
                        result.Data = JsonConvert.DeserializeObject<TResponse>(result.StatusCode.ToString());
                        return result;
                    }

                    if (returnPureString)
                        result.Data = JsonConvert.DeserializeObject<TResponse>($"\"{result.RawResponse}\"");
                    else
                        result.Data = JsonConvert.DeserializeObject<TResponse>(result.RawResponse);

                    return result;
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = $"JSON Deserialization Error: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Request Exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 取得 Request 格式為 Multipart/form-data 的 Web API 回應，若 Token 過期自動刷新 (嘗試1次)
        /// </summary>
        /// <typeparam name="T">Response 的物件類型</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="fields">KeyValue List，用來轉為 Multipart/form-data 格式</param>
        /// <param name="refreshedToken">是否有刷新過 Token</param>
        /// <returns></returns>
        public static async Task<T> SendServerFormRequestAsync<T>(
            string url,
            HttpMethod method,
            List<Unity.Extensions.KeyValue> fields = null,
            bool refreshedToken = false,
            bool returnPureString = false,
            bool returnHttpStatus = false)
        {
            ServerCacheData serverCache = GetCacheData(url);
            var response = await SendFormRequestAsync<T>(url, method, data: fields, token: serverCache.Token, returnPureString: returnPureString, returnHttpStatus: returnHttpStatus);

            if (response.IsSuccess)
                return response.Data;

            if (!refreshedToken && (response.StatusCode == 401 || response.StatusCode == 403))
            {
                // 嘗試刷新 Token 並重試
                bool refreshed = await RefreshTokenAsync();
                OnAPILogCallback?.Invoke("Token expired. Refreshing token...", true);
                if (refreshed)
                    return await SendServerFormRequestAsync<T>(url, method, fields, returnPureString);
                else
                {
                    OnAPILogCallback?.Invoke("Failed to refresh token.", true);
                    return default(T);
                }
            }

            OnAPILogCallback?.Invoke($"Failed to refresh token.\n" +
                $"Unexpected HTTP error: {response.ErrorMessage}\n" +
                $"Url: {url}\n" +
                $"serverCache.LoginAPI: {serverCache.LoginAPI}\n" +
                $"RequestData: {response.Data}\n" +
                $"RawResponse: {response.RawResponse}",
                true);
            return default(T);
        }

        /// <summary>
        /// 嘗試刷新 Token
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> RefreshTokenAsync()
        {
            UserData loggedInUser = SecureDataManager.LoadLoggedInData();
            UserLoginIBMSPlatformDto loginData = new UserLoginIBMSPlatformDto(loggedInUser.Id, loggedInUser.Password);
            return await AuthService.LoginUserOnIBMSPlatform(loginData);
        }

        /// <summary>
        /// 取得 Requeset 格視為 Json Data 的 Web API 回應，若 Token 過期自動刷新 (嘗試1次)
        /// </summary>
        /// <typeparam name="T">Response 的物件類型</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http 方法</param>
        /// <param name="jsonData">Json 格式的 Content</param>
        /// <param name="refreshedToken">是否有刷新過 Token</param>
        /// <returns></returns>
        public static async Task<T> SendServerJsonRequestAsync<T>(
            string url,
            HttpMethod method,
            string jsonData = null,
            bool refreshedToken = false)
        {
            ServerCacheData serverCache = GetCacheData(url);
            var response = await SendJsonRequestAsync<T>(url, method, data: jsonData, token: serverCache.Token, returnPureString: typeof(T) == typeof(string));

            if (response.IsSuccess)
                return response.Data;

            if (!refreshedToken && (response.StatusCode == 401 || response.StatusCode == 403))
            {
                // 嘗試刷新 Token 並重試
                bool refreshed = await RefreshTokenAsync();
                OnAPILogCallback?.Invoke("Token expired. Refreshing token...", true);
                if (refreshed)
                    return await SendServerJsonRequestAsync<T>(url, method, jsonData, true);
                else
                {
                    OnAPILogCallback?.Invoke("Failed to refresh token.", true);
                    return default(T);
                }
            }

            OnAPILogCallback?.Invoke($"Failed to refresh token.\n" +
                $"Unexpected HTTP error: {response.ErrorMessage}\n" +
                $"Url: {url}\n" +
                $"serverCache.LoginAPI: {serverCache.LoginAPI}\n" +
                $"RequestData: {jsonData}\n" +
                $"RawResponse: {response.RawResponse}",
                true);
            return default(T);
        }

        /// <summary>
        /// 載入資料的統一邏輯：先取得本地、再取得後端資料，每次取得資料回呼資料載入，後端資料取得後儲存至本地
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onDataLoaded">取得資料後的回呼</param>
        /// <param name="loadLocalData">加載本地數據的方法 (可選)</param>
        /// <param name="loadServerData">加載伺服器數據的方法 (可選)</param>
        /// <param name="saveData">保存數據的方法 (可選)</param>
        /// <param name="skipLocal">是否跳過讀取本地數據</param>
        public static async void LoadDataAsync<T>(
            Action<T> onDataLoaded,
            Func<Task<T>> loadLocalData = null,
            Func<Task<T>> loadServerData = null,
            Action<T> saveData = null, 
            bool skipLocal = false)
        {
            bool hasSentCallback = false;

            // 優先從本地加載數據
            if (!skipLocal)
            {
                var localData = await loadLocalData();
                if (localData != null)
                {
                    onDataLoaded?.Invoke(localData);
                    hasSentCallback = true;
                }
            }

            // 從伺服器加載數據
            var serverData = await loadServerData();
            if (serverData != null)
            {
                saveData?.Invoke(serverData); // 保存數據
                onDataLoaded?.Invoke(serverData);
            }
            else
            {
                //Debug.LogWarning("Failed to load data from server.");
                if (!hasSentCallback)
                    onDataLoaded?.Invoke(default(T));
            }
        }
    }

    public class ResponseResult<T>
    {
        public int StatusCode { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public string RawResponse { get; set; } // 原始回應
        public string RequestDetails { get; set; } // 請求詳細資訊
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }

    public class ServerCacheData
    {
        public string ServerIP { get; private set; }
        public string LoginAPI { get; private set; }
        public string Token { get; set; }

        public ServerCacheData(string serverIP, string loginAPI, string cacheToken)
        {
            ServerIP = serverIP;
            LoginAPI = loginAPI;
            Token = cacheToken;
        }
    }
}
