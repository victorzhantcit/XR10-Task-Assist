using FMSolution.FMETP;
using FMSolution.FMWebSocket;
using Guidance.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Guidance.Core
{
    [RequireComponent(typeof(FMWebSocketManager))]
    public class WebSocketService : MonoBehaviour
    {
        public string WsId => _fmWebSocket.Settings.wsid;
        public bool IsInitialized => _fmWebSocket.Initialised;
        public string ServerPort => $"{_fmWebSocket.Settings.IP}:{_fmWebSocket.Settings.port}";
        public bool HasServerConnection => _fmWebSocket.Settings.ConnectionStatus == FMWebSocketConnectionStatus.FMWebSocketConnected;
        public List<ConnectedFMWebSocketClient> ConnectedClients => _fmWebSocket.ConnectedClients;
        public bool IsRoomMaster => _fmWebSocket.Settings.wsRoomMaster;
        public bool IsConnectedToOthers => _fmWebSocket.ConnectedClients.Count > 1;
        public bool IsMasterWithOthers => IsRoomMaster && IsConnectedToOthers;
        public string FirstClient => IsConnectedToOthers ? _fmWebSocket.ConnectedClients[1].wsid : "";
        public string RoomName
        {
            get => _fmWebSocket.RoomName;
            set => _fmWebSocket.RoomName = value;
        }

        [SerializeField] private FMWebSocketManager _fmWebSocket;
        [SerializeField] private GameViewEncoder _gameViewEncoder;
        [SerializeField] private GameViewDecoder _mainRemoteDecoder;
        [SerializeField] private MicEncoder _microphoneEncoder;

        private void Start() => ActivateFMETPComponents(false);

        public void InitSocketService(string roomName, Action callback)
        {
            ActivateFMETPComponents(true);
            _fmWebSocket.Action_JoinOrCreateRoom(roomName);
            StartCoroutine(WaitForInitialization(callback));
        }

        private IEnumerator WaitForInitialization(Action callback)
        {
            yield return new WaitUntil(() => _fmWebSocket.Initialised && !string.IsNullOrEmpty(WsId));
            callback();
        }

        public void JoinRoom(string roomName)
        {
            _fmWebSocket.Action_JoinOrCreateRoom(roomName);
        }

        public void SetLocalEncoder(ushort viewLabel, ushort microphoneLabel)
        {
            _gameViewEncoder.label = viewLabel;
            _microphoneEncoder.label = microphoneLabel;
        }

        public void SetMainRemoteEncoder(ushort viewLabel)
        {
            _mainRemoteDecoder.label = viewLabel;
        }

        public void SetMixedRealityVideoScale(float scale)
        {
            _gameViewEncoder.MixedRealityScaleX = scale;
            _gameViewEncoder.MixedRealityScaleY = scale;
        }

        public void SetMixedRealityVideoOffset(float? offsetX = null, float? offsetY = null)
        {
            if (offsetX.HasValue) _gameViewEncoder.MixedRealityOffsetX = offsetX.Value;
            if (offsetY.HasValue) _gameViewEncoder.MixedRealityOffsetY = offsetY.Value;
        }

        public void CloseConnection()
        {
            if (!_fmWebSocket.Initialised) return;
            _fmWebSocket.Action_Close();
            _fmWebSocket.Settings.wsid = null;

            StartCoroutine(WaitUntilWebSocketClosed(() => ActivateFMETPComponents(false)));
        }

        private IEnumerator WaitUntilWebSocketClosed(Action action = null)
        {
            yield return new WaitUntil(() => _fmWebSocket.Settings.ConnectionStatus == FMWebSocketConnectionStatus.Disconnected);
            action?.Invoke();
        }

        private void ActivateFMETPComponents(bool enable) 
            => _fmWebSocket.enabled = _gameViewEncoder.enabled = _mainRemoteDecoder.enabled = _microphoneEncoder.enabled = enable;

        public void RequestRoomMaster() => _fmWebSocket.Action_RequestRoomMaster();

        public void SendToClient(string targetWsId, WebsocketMsg message)
        {
            string jsonMsg = JsonConvert.SerializeObject(message);
            _fmWebSocket.SendToTarget(jsonMsg, targetWsId);
        }

        public void BroadcastToClients(WebsocketMsg message)
        {
            string jsonMsg = JsonConvert.SerializeObject(message);
            _fmWebSocket.SendToOthers(jsonMsg);
        }

        public void AddByteDataReceivedListener(UnityAction<byte[]> action) => _fmWebSocket.OnReceivedByteDataEvent.AddListener(action);

        public void RemoveByteDataReceivedListener(UnityAction<byte[]> action) => _fmWebSocket.OnReceivedByteDataEvent.RemoveListener(action);

        public IEnumerator FetchClients(Action<List<UsernameData>> onSuccess, Action<string> onError)
        {
            string apiUrl = $"http://{ServerPort}/clients";
            yield return FetchData(apiUrl, jsonResponse =>
            {
                try
                {
                    var userNameArray = JsonConvert.DeserializeObject<List<UsernameData>>(jsonResponse);
                    onSuccess?.Invoke(userNameArray);
                }
                catch (Exception ex) { onError?.Invoke($"Failed to parse response: {ex.Message}"); }
            }, onError);
        }

        public IEnumerator FetchClient(string clientWsId, Action<UsernameData> onSuccess, Action<string> onError)
        {
            string apiUrl = $"http://{ServerPort}/clients/get-username?wsid={clientWsId}";
            yield return FetchData(apiUrl, jsonResponse =>
            {
                try
                {
                    var clientName = JsonConvert.DeserializeObject<UsernameData>(jsonResponse);
                    onSuccess?.Invoke(clientName);
                }
                catch (Exception ex) { onError?.Invoke($"Failed to parse response: {ex.Message}"); }
            }, onError);
        }

        public IEnumerator FetchRooms(Action<List<RoomInfo>> onSuccess, Action<string> onError)
        {
            string apiUrl = $"http://{ServerPort}/rooms";
            yield return FetchData(apiUrl, jsonResponse =>
            {
                try
                {
                    var roomArray = JsonConvert.DeserializeObject<List<RoomInfo>>(jsonResponse);
                    onSuccess?.Invoke(roomArray);
                }
                catch (Exception ex) { onError?.Invoke($"Failed to parse response: {ex.Message}"); }
        }, onError);
        }

        private IEnumerator FetchData(string apiUrl, Action<string> onSuccess, Action<string> onFailure)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                    onSuccess.Invoke(webRequest.downloadHandler.text);
                else
                    onFailure.Invoke(webRequest.error);
            }
        }

        public IEnumerator PostUsername(string username)
        {
            // 準備要傳送的數據
            string apiUrl = $"http://{ServerPort}/set-username";           
            string jsonBody = JsonConvert.SerializeObject(new UsernameData(WsId, username));

            // 創建一個新的 UnityWebRequest 來發送 POST 請求
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 發送請求
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Username sent successfully: " + request.downloadHandler.text);
            else
                Debug.LogError("Failed to send username: " + request.error);
        }
    }
}
