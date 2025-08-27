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
        PUT // �K�[ PUT ��k
    }

    public static class APIHelper
    {
        // ���ѥ~�����o API Log �T��
        public static event Action<string, bool> OnAPILogCallback;

        // �x�s ServerIP -> LoginAPI,Token ���������Y
        private static Dictionary<string, ServerCacheData> _serverCacheMap = new Dictionary<string, ServerCacheData>();

        /// <summary>
        /// ���U Web API ��T
        /// </summary>
        /// <param name="loginAPI">Web API ���n�J API</param>
        /// <param name="token">�q�n�J API ���o�� Token</param>
        public static void RegisterLoginAPI(string loginAPI, string token = null)
        {
            string serverIP = GetPrefix(loginAPI);
            _serverCacheMap[serverIP] = new ServerCacheData(serverIP, loginAPI, token);
        }

        /// <summary>
        /// �������U���w�� Web API ��T
        /// </summary>
        /// <param name="relatedUrl">���w�� Web API Url</param>
        public static void UnregisterLoginAPI(string relatedUrl)
        {
            string serverIP = GetPrefix(relatedUrl);
            if (_serverCacheMap.TryGetValue(serverIP, out ServerCacheData data))
                _serverCacheMap.Remove(serverIP);
        }

        /// <summary>
        /// �������U�Ҧ� Web API ��T
        /// </summary>
        public static void UnregisterAllLoginAPI()
        {
            _serverCacheMap.Clear();
        }

        /// <summary>
        /// �]�w Web API �ϥΪ� Token
        /// </summary>
        /// <param name="relatedUrl">Web API Url</param>
        /// <param name="newToken">�s�� Token</param>
        /// <exception cref="Exception"></exception>
        public static void SetAPIToken(string relatedUrl, string newToken)
        {
            string serverIP = GetPrefix(relatedUrl);
            if (_serverCacheMap.TryGetValue(serverIP, out ServerCacheData data))
                data.Token = newToken;
            throw new Exception("Only registered server can refresh token");
        }

        /// <summary>
        /// �q Url ���o�w���U�� Web API ��T
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
        /// ���o Url ���AIP PORT ����T
        /// </summary>
        /// <param name="relatedUrl">Web API Url</param>
        /// <returns></returns>
        private static string GetPrefix(string relatedUrl)
        {
            Uri uri = new Uri(relatedUrl);
            // �Y port �O�w�]�ȡ]http: 80, https: 443�^�A�N�ٲ�
            if ((uri.Scheme == "http" && uri.Port == 80) || (uri.Scheme == "https" && uri.Port == 443))
                return $"{uri.Scheme}://{uri.Host}";
            else
                return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }

        /// <summary>
        /// �Ыذ򥻪� Json Request Header
        /// </summary>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="content">�ǰe���e</param>
        /// <param name="token">�ϥΪ� Bearer Token</param>
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
        /// �Ыذ򥻪� multipart/form-data Request Header
        /// </summary>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="formFields">�ǤJ Key-Value List�A�H�ഫ�� form-data �榡</param>
        /// <param name="token">�ϥΪ� Bearer Token</param>
        /// <returns></returns>
        public static UnityWebRequest CreateMultipartRequest(string url, HttpMethod method, List<KeyValue> formFields, string token)
        {
            UnityWebRequest request;

            if (method == HttpMethod.GET)
            {
                // GET �ШD�G�N formFields �����e������ URL �d�ߦr�ꤤ�]�B�z�Ť��e�^
                string query = formFields != null && formFields.Count > 0
                    ? string.Join("&", formFields.Select(f => $"{f.Key}={UnityWebRequest.EscapeURL(f.Value)}"))
                    : string.Empty;

                string urlWithQuery = url + (string.IsNullOrEmpty(query) ? string.Empty : $"?{query}");
                request = UnityWebRequest.Get(urlWithQuery);
            }
            else
            {
                // �ʺA�ͦ� boundary
                string boundary = "----UnityMultipartBoundary" + System.DateTime.Now.Ticks.ToString("x");
                StringBuilder multipartContent = new StringBuilder();

                // �K�[���r�q����
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

                // ���� boundary
                multipartContent.AppendLine($"--{boundary}--");

                // �N���e�ର�r�`�Ʋ�
                byte[] bodyRaw = Encoding.UTF8.GetBytes(multipartContent.ToString());

                // POST �M PUT �ШD�G�N���e��J Body
                request = new UnityWebRequest(url, method == HttpMethod.POST ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbPUT);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");
            }

            // �K�[ Authorization ���Y�]�p�G�ݭn�^
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// �o�e UnityWebRequest �õ��ݦ^��
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
        /// �o�e�榡�� Json �� Web API 
        /// </summary>
        /// <typeparam name="TResponse">�^���榡</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="data">Json Data</param>
        /// <param name="token">Web API �ϥΪ�����</param>
        /// <param name="returnPureString">��ݬO�_�^�ǯ¦r�� (response body ���a�޸�)</param>
        /// <returns></returns>
        public static async Task<ResponseResult<TResponse>> SendJsonRequestAsync<TResponse>(
            string url,
            HttpMethod method,
            string data = null,
            string token = null,
            bool returnPureString = false,
            bool returnHttpStatus = false) => await SendRequest<TResponse>(url, method, jsonData: data, formFields: null, token, isJson: true, returnPureString, returnHttpStatus);

        /// <summary>
        /// �o�e�榡�� multipart/form-data �� Web API 
        /// </summary>
        /// <typeparam name="TResponse">�^���榡</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="data">multipart/form-data Data</param>
        /// <param name="token">Web API �ϥΪ�����</param>
        /// <param name="returnPureString">��ݬO�_�^�ǯ¦r�� (response body ���a�޸�)</param>
        /// <returns></returns>
        public static async Task<ResponseResult<TResponse>> SendFormRequestAsync<TResponse>(
            string url,
            HttpMethod method,
            List<KeyValue> data = null,
            string token = null,
            bool returnPureString = false,
            bool returnHttpStatus = false) => await SendRequest<TResponse>(url, method, jsonData: null, formFields: data, token, isJson: false, returnPureString, returnHttpStatus);

        /// <summary>
        /// �N Request ��z��A�o�e�ШD�ܫ�ݨè��o�^��
        /// </summary>
        /// <typeparam name="TResponse">�^���榡</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="jsonData">Json �榡�����</param>
        /// <param name="formFields">Multipart/form-data �榡�����</param>
        /// <param name="token">Web API ������</param>
        /// <param name="isJson">�O�_�ĥ� Json �榡����ưe�X�A�Y�_�h�ĥ� multipart/form-data</param>
        /// <param name="returnPureString">��ݬO�_�^�ǯ¦r�� (response body ���a�޸�)</param>
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
                // JSON �ǿ�
                request = CreateJsonRequest(url, method, jsonData, token);
                debugContent = jsonData;
            }
            else
            {
                // Form �ǿ�
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
        /// ���o Request �榡�� Multipart/form-data �� Web API �^���A�Y Token �L���۰ʨ�s (����1��)
        /// </summary>
        /// <typeparam name="T">Response ����������</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="fields">KeyValue List�A�Ψ��ର Multipart/form-data �榡</param>
        /// <param name="refreshedToken">�O�_����s�L Token</param>
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
                // ���ը�s Token �í���
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
        /// ���ը�s Token
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> RefreshTokenAsync()
        {
            UserData loggedInUser = SecureDataManager.LoadLoggedInData();
            UserLoginIBMSPlatformDto loginData = new UserLoginIBMSPlatformDto(loggedInUser.Id, loggedInUser.Password);
            return await AuthService.LoginUserOnIBMSPlatform(loginData);
        }

        /// <summary>
        /// ���o Requeset ����� Json Data �� Web API �^���A�Y Token �L���۰ʨ�s (����1��)
        /// </summary>
        /// <typeparam name="T">Response ����������</typeparam>
        /// <param name="url">Web API Url</param>
        /// <param name="method">Http ��k</param>
        /// <param name="jsonData">Json �榡�� Content</param>
        /// <param name="refreshedToken">�O�_����s�L Token</param>
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
                // ���ը�s Token �í���
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
        /// ���J��ƪ��Τ@�޿�G�����o���a�B�A���o��ݸ�ơA�C�����o��Ʀ^�I��Ƹ��J�A��ݸ�ƨ��o���x�s�ܥ��a
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onDataLoaded">���o��ƫ᪺�^�I</param>
        /// <param name="loadLocalData">�[�����a�ƾڪ���k (�i��)</param>
        /// <param name="loadServerData">�[�����A���ƾڪ���k (�i��)</param>
        /// <param name="saveData">�O�s�ƾڪ���k (�i��)</param>
        /// <param name="skipLocal">�O�_���LŪ�����a�ƾ�</param>
        public static async void LoadDataAsync<T>(
            Action<T> onDataLoaded,
            Func<Task<T>> loadLocalData = null,
            Func<Task<T>> loadServerData = null,
            Action<T> saveData = null, 
            bool skipLocal = false)
        {
            bool hasSentCallback = false;

            // �u���q���a�[���ƾ�
            if (!skipLocal)
            {
                var localData = await loadLocalData();
                if (localData != null)
                {
                    onDataLoaded?.Invoke(localData);
                    hasSentCallback = true;
                }
            }

            // �q���A���[���ƾ�
            var serverData = await loadServerData();
            if (serverData != null)
            {
                saveData?.Invoke(serverData); // �O�s�ƾ�
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
        public string RawResponse { get; set; } // ��l�^��
        public string RequestDetails { get; set; } // �ШD�ԲӸ�T
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
