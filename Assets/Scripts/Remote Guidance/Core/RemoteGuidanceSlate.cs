using FMSolution.FMETP;
using Guidance.Dtos;
using Guidance.Utils;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using MRTK.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using User.Core;
using User.Dtos;
using Slider = MixedReality.Toolkit.UX.Slider;

namespace Guidance.Core.XR
{
    public class RemoteGuidanceSlate : MonoBehaviour
    {
        public string QuickStartRoomName = string.Empty;

        [Header("Modules")]
        [SerializeField] private ModelManager _model; 
        [SerializeField] private WebSocketService _wsService;

        [Header("Hand Menu")]
        [SerializeField] private PressableButton _undoMarkerButton;
        [SerializeField] private PressableButton _redoMarkerButton;
        [SerializeField] private PressableButton _eraseAllMarkerButton;
        [SerializeField] private PressableButton _arMeshToggleButton;

        [Header("Dialog")]
        [SerializeField] private CanvasInputFieldDialog _submitDialog;
        [SerializeField] private CanvasSliderDialog _sliderDialog;
        [SerializeField] private DialogPoolHandler _dialogPoolHandler;

        [Header("Lobby")]
        [SerializeField] private TabView _tabView;
        [SerializeField] private RectTransform _sidePanel;
        [SerializeField] private TMP_Text[] _userNameDisplay;
        [SerializeField] private PressableButton _leaveRoomButton;
        [SerializeField] private VirtualizedScrollRectList _userVirtualList;
        [SerializeField] private VirtualizedScrollRectList _meetingVirtualList;

        [Header("Meeting Room")]
        [SerializeField] private TMP_Text _roomNameDisplay;
        [SerializeField] private Transform _remoteAudioGroup;
        [SerializeField] private RemoteSource _remoteSourcePrefab;
        [SerializeField] private VirtualizedScrollRectList _audioVirtualList;
        [SerializeField] private Transform _gridRemoteViewPlate;
        [SerializeField] private Transform _remoteVideoGridLayout;
        [SerializeField] private Transform _mainRemoteViewPlate;

        [Header("Settings")]
        [SerializeField] private TMP_Text _wsidText;
        [SerializeField] private TMP_Text _markerShiftValue;
        [SerializeField] private TMP_Text[] _deviceCamaraArgs;
        
        private bool _reconnectToRoom = false;
        private List<UsernameData> _uuidList = new();
        private List<RoomInfo> _roomInfoList = new(); 
        private Coroutine _lobbyCoroutine;
        private bool _isLobbyRunning = false;

        private void Start()
        {
            _userVirtualList.OnVisible = OnVisibleUserInList;
            _meetingVirtualList.OnVisible = OnVisibleMeetingInList;
            _audioVirtualList.OnVisible = OnVisibleAudioInList;
            InitializeUserName();
        }

        public void OpenSlate() => SetVisible(true);
        public void CloseSlate() => SafelyCloseSlateHandling();

        private void SetVisible(bool visible)
        {
            if (visible == this.gameObject.activeSelf) return;


            if (visible) this.gameObject.SetActive(true);
            StartCoroutine(Initialize(visible));
            QuickStartedDetect();
        }

        private void QuickStartedDetect()
        {
            bool isNormalInit = string.IsNullOrEmpty(QuickStartRoomName);
            if (isNormalInit) return;

            string roomName = QuickStartRoomName;
            _dialogPoolHandler.ShowDialog("������i�椤�u��",
                $"�O�_�ֳt�Ыط|ĳ�H\n{roomName}",
                () => StartQuickMeeting(roomName),
                () => StartCoroutine(Initialize(true))
            );
            QuickStartRoomName = string.Empty;
        }

        private void StartQuickMeeting(string roomName)
        {
            ExitAndPrepareForNewRoom(roomName);
        }

        private IEnumerator Initialize(bool visible)
        {
            if (_wsService.IsInitialized)
                yield return StartCoroutine(HandleRoomExit());

            if (visible) InitializeLobby();
            else DeactiveLobby();
        }

        private void InitializeLobby()
        {
            ShowMeetingLobby(true);
            _leaveRoomButton.enabled = false;
        }

        private void DeactiveLobby()
        {
            _wsService.RoomName = "";
            _wsService.CloseConnection();
            _leaveRoomButton.enabled = false;
            StopLobbyCoroutine();
            this.gameObject.SetActive(false);
        }

        private void SafelyCloseSlateHandling()
        {
            if (!_wsService.IsInitialized)
                SetVisible(false);
            else
                _dialogPoolHandler.ShowDialog("��e�٦b�|ĳ���A�O�_�h�X�|ĳ�H", null,
                    () => StartCoroutine(HandleRoomExit(() => SetVisible(false))), () => Debug.Log("Cancel close remote slate"));
        }

        public void SaveExit()
        {
            if (_wsService.IsInitialized) StartCoroutine(HandleRoomExit(() => QuitApplication()));
            else QuitApplication();
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void LeaveRoomAndDisconnect()
        {
            StartCoroutine(HandleRoomExit(() =>
            {
                _wsService.RoomName = "";
                _wsService.CloseConnection();
                _leaveRoomButton.enabled = false;
                _isLobbyRunning = false;
                ShowMeetingLobby(true);
            }));
        }

        public void HandleWebSocketDataReceived(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData)) return;

            try
            {
                var receive = JsonConvert.DeserializeObject<WebsocketMsg>(jsonData);

                switch (receive.SocketType)
                {
                    case WebsocketType.GameViewDecoder:
                        string remoteWsId = receive.Data.ToString();
                        AddRemoteView(remoteWsId);
                        break;
                    case WebsocketType.RenameRoom:
                        string newRoomName = receive.Data.ToString();

                        _reconnectToRoom = true;
                        ExitAndPrepareForNewRoom(newRoomName);
                        break;
                    case WebsocketType.ClientDisconnectNotify:
                        string leavingWsId = receive.Data.ToString();
                        if (leavingWsId == _wsService.WsId) return;

                        RemoveRemoteView(leavingWsId);
                        break;
                    case WebsocketType.TransferRoomMaster:
                        _wsService.RequestRoomMaster();
                        break;
                    case WebsocketType.RemoteMarker:
                        ShapeData penData = JsonConvert.DeserializeObject<ShapeData>(JsonConvert.SerializeObject(receive.Data));
                        HandleRemoteMarkerRequest(penData);
                        break;
                    case WebsocketType.UndoMarker:
                        if (_undoMarkerButton.enabled)
                            HandleMarkerUndo();
                        break;
                    case WebsocketType.RedoMarker:
                        if (_redoMarkerButton.enabled)
                            HandleMarkerRedo();
                        break;
                    case WebsocketType.EraseAllMarker:
                        HandleMarkerEraseAll();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                //Log("Receive message from server:" + jsonData);
            }
        }

        private void OnVisibleUserInList(GameObject objectInList, int index)
        {
            OnlineUserUI onlineUserUI = objectInList.GetComponent<OnlineUserUI>();

            onlineUserUI.SetUp(_uuidList[index].username);
        }

        private void OnVisibleMeetingInList(GameObject objectInList, int index)
        {
            MeetingRoomUI meetingRoomUI = objectInList.GetComponent<MeetingRoomUI>();
            RoomInfo roomInfo = JsonConvert.DeserializeObject<RoomInfo>(
                JsonConvert.SerializeObject(_roomInfoList[index])
            );

            if (_uuidList != null)
            {
                UsernameData findRoomMaster = _uuidList.Find(user => user.wsid == roomInfo.RoomMasterWSID);

                if (findRoomMaster != null)
                    roomInfo.RoomMasterWSID = findRoomMaster.username;
            }

            meetingRoomUI.SetUp(roomInfo, () => {
                bool isJoinedRoom = roomInfo.RoomName == _wsService.RoomName;

                if (isJoinedRoom)
                    ShowMeetingLobby(false);
                else
                    ExitAndPrepareForNewRoom(roomInfo.RoomName);
            });

        }

        private void OnVisibleAudioInList(GameObject objectInList, int index)
        {
            if (index < 0 || index >= _remoteAudioGroup.childCount) return;

            CanvasSliderDialog audioSlider = objectInList.GetComponent<CanvasSliderDialog>();
            AudioDecoder audio = _remoteAudioGroup.GetChild(index).GetComponent<AudioDecoder>();

            audioSlider.Setup(audio.name, audio.Volume, (args) =>
            {
                audio.Volume = args.NewValue;
                audioSlider.UpdateValueLabel(audio.Volume, true);
            });

            StartCoroutine(GetUserName(audio.name, userNameData => audioSlider.UpdateHeaderLabel(userNameData.username)));
            audioSlider.UpdateValueLabel(audio.Volume, true);
        }

        private IEnumerator GetUserName(string clientWsId, Action<UsernameData> result)
        {
            yield return new WaitForSeconds(1f);
            yield return _wsService.FetchClient(clientWsId, userNameData =>
            {
                result.Invoke(userNameData);
            },
            error => Debug.LogWarning("Error fetching client: " + error));
        }

        public void RefreshAudioControlPanel()
        {
            if (!_audioVirtualList.isActiveAndEnabled) return;

            StartCoroutine(RefreshAudioControlPanelAsync());
        }

        private IEnumerator RefreshAudioControlPanelAsync()
        {
            yield return new WaitForEndOfFrame();
            if (!_audioVirtualList.isActiveAndEnabled) yield break;
            _audioVirtualList.SetItemCount(_remoteAudioGroup.childCount);
            _audioVirtualList.ResetLayout();
        }

        private void ExitAndPrepareForNewRoom(string roomName)
        {
            // �h�X�ж���A�s�u�ܷs���|ĳ
            if (!_wsService.IsInitialized)
                InitFMWebSocket(roomName);
            else
                StartCoroutine(HandleRoomExit(() => InitFMWebSocket(roomName)));
        }

        public void StartFMConnection()
        {
            InitFMWebSocket();
        }

        private IEnumerator HandleRoomExit(Action callback = null)
        {
            // �Y���ХD�B����L�|ĳ�ѻP��
            if (_wsService.IsMasterWithOthers)
                yield return TransferRoomMaster();

            // �����s�u�ð�����򪺦^���޿�
            _wsService.CloseConnection();
            ClearRemoteViews();
            UpdateMeetingRoomStatus(false);
            callback?.Invoke();
        }

        private IEnumerator TransferRoomMaster()
        {
            // �ǿ������ХD���T�����Ĥ@�ӷ|ĳ�ѻP�̡A�Ϩ䱵���ᦨ���ХD
            var transferRoomMaster = new WebsocketMsg(WebsocketType.TransferRoomMaster, _wsService.WsId);
            _wsService.SendToClient(_wsService.FirstClient, transferRoomMaster);

            // ���ݪ���ХD��������
            yield return new WaitUntil(() => !_wsService.IsRoomMaster);
        }

        private void ClearRemoteViews()
        {
            foreach (Transform remoteViewTransform in _remoteVideoGridLayout)
            {
                RemoveRemoteView(remoteViewTransform.name);
            }
        }

        private void RemoveRemoteView(string remoteWsId)
        {
            Transform remoteSourceTransform = _remoteVideoGridLayout.transform.Find(remoteWsId);

            if (remoteSourceTransform == null) return;

            RemoteSource remoteSource = remoteSourceTransform.GetComponent<RemoteSource>();

            _wsService.RemoveByteDataReceivedListener(remoteSource.VideoProcessData);
            _wsService.RemoveByteDataReceivedListener(remoteSource.AudioProcessData);

            remoteSource.SafeDestroy();
            RefreshAudioControlPanel();
        }

        public void InitFMWebSocket(string roomName = "")
        {
            if (string.IsNullOrEmpty(roomName))
            {
                int rand = UnityEngine.Random.Range(0, 65665);
                roomName = $"Room {rand}";
            }

            UpdateMeetingRoomStatus(false);
            _wsService.InitSocketService(roomName, () => StartCoroutine(WaitForFMInit()));
        }

        private IEnumerator WaitForFMInit()
        {
            UpdateMeetingRoomStatus(true, _wsService.WsId);

            yield return _wsService.PostUsername(_userNameDisplay[0].text);

            ShowMeetingLobby(false);
            _leaveRoomButton.enabled = true;
        }

        private void UpdateMeetingRoomStatus(bool online, string socketId = "")
        {
            _wsidText.text = string.Empty;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_sidePanel);

            if (online)
                UpdateUIForOnlineState(socketId);
            _reconnectToRoom = false;
        }

        private void UpdateUIForOnlineState(string socketId)
        {
            ushort encoderLabel = GetGameViewLabelByWsId(_wsService.WsId);

            _wsidText.text = $"ID: {socketId}";
            _wsService.SetLocalEncoder(encoderLabel, GetGameViewMicLabel(encoderLabel));
            _roomNameDisplay.text = _wsService.RoomName;

            if (!_reconnectToRoom)
                InitializeUserName();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_sidePanel);
        }

        private ushort GetGameViewMicLabel(ushort gameViewLabel)
        {
            return (ushort)(++gameViewLabel > ushort.MaxValue ? 1000 : gameViewLabel);
        }

        private void InitializeUserName()
        {
            string userName = SecureDataManager.GetLoggedInUserName();

            if (string.IsNullOrEmpty(userName)) userName = "Guest";
            for (int i = 0; i < _userNameDisplay.Length; i++)
                _userNameDisplay[i].text = userName;
        }

        // ���o��|�Ӧr���p "eb48"�A�N�Q���i��r���ഫ�� ushort
        private ushort GetGameViewLabelByWsId(string id)
        {
            string gameViewId = id.Substring(id.Length - 4);
            ushort label = Convert.ToUInt16(gameViewId, 16);

            if (label < 1000) label += 1000;

            return label;
        }

        public void ShowMeetingLobby(bool enable)
        {
            // �]�mUI��ܪ��A
            int indexOfView = enable ? 0 : 1;
            string tabName = _tabView.TabSections[indexOfView].SectionName;

            _tabView.ForceSetTabActiveByLabel(tabName);

            if (enable && !_isLobbyRunning)
            {
                // �}�l��{�üаO���B�檬�A
                StartLobbyCoroutine();
            }
            else if (!enable && _isLobbyRunning)
            {
                // �����{�í��m���A
                StopLobbyCoroutine();
            }
        }

        private IEnumerator UpdateLobbyView()
        {
            // �C5��ШD�@���ж��M�Ȥ�H��
            while (true)
            {
                Debug.Log("UpdateLobbyUI");
                yield return UpdateLobbyUI();
                yield return new WaitForSeconds(5f);
            }
        }

        private void StartLobbyCoroutine()
        {
            if (_lobbyCoroutine == null)
            {
                _lobbyCoroutine = StartCoroutine(UpdateLobbyView());
                _isLobbyRunning = true;
            }
        }

        private void StopLobbyCoroutine()
        {
            if (_lobbyCoroutine != null)
            {
                StopCoroutine(_lobbyCoroutine);
                _lobbyCoroutine = null;
                _isLobbyRunning = false;
            }
        }

        private IEnumerator UpdateLobbyUI()
        {
            yield return _wsService.FetchClients(uuidFromServer =>
            {
                uuidFromServer.Sort((a, b) =>
                {
                    int usernameComparison = a.username.CompareTo(b.username);

                    if (usernameComparison == 0) return a.wsid.CompareTo(b.wsid);
                    return usernameComparison;
                });

                // �ˬd�ƾڬO�_���ܤơA�קK�����n����s
                if (!IsListEqual(_uuidList, uuidFromServer))
                {
                    _uuidList = new List<UsernameData>(uuidFromServer);
                    _userVirtualList.SetItemCount(_uuidList.Count);
                    _userVirtualList.ResetLayout();
                }
            },
            error => Debug.LogWarning("Error fetching client list: " + error));

            yield return _wsService.FetchRooms(meetingsFormServer =>
            {
                meetingsFormServer = meetingsFormServer.AsEnumerable().Reverse().ToList();

                // �ˬd�ƾڬO�_���ܤơA�קK�����n����s
                if (!IsListEqual(_roomInfoList, meetingsFormServer))
                {
                    _roomInfoList = new List<RoomInfo>(meetingsFormServer);
                    _meetingVirtualList.SetItemCount(_roomInfoList.Count);
                    _meetingVirtualList.ResetLayout();
                }
            },
            error => Debug.LogWarning("Error fetching room list: " + error));
        }

        private bool IsListEqual<T>(List<T> oldList, List<T> newList)
        {
            if (oldList == null || newList == null || oldList.Count != newList.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < oldList.Count; i++)
            {
                if (!comparer.Equals(oldList[i], newList[i])) // �ϥ� EqualityComparer �Ӷi����
                {
                    return false;
                }
            }

            return true;
        }

        public void OpenChangeNameDialog()
        {
            SetSubmitDialog("���ϥΪ̦W��", _userNameDisplay[0].text, OnUserNameSubmit);
        }

        public void OpenNewRoomDialog()
        {
            SetSubmitDialog("�Ыط|ĳ", string.Empty, HandleCreateNewRoom);
        }

        public void OpenChangeRoomNameDialog()
        {
            SetSubmitDialog("���ж��W��", string.Empty, OnRoomNameSubmit);
        }

        private void SetSubmitDialog(string message, string defaultValue, Action<string> onSubmitted)
            => _submitDialog.Setup(message, defaultValue, onSubmitted);

        public void OnResizeSliderValueUpdated(SliderEventData e)
        {
            float newScale = 1 + e.NewValue;

            _mainRemoteViewPlate.transform.localScale = new Vector3(newScale, newScale, newScale);
            _sliderDialog.UpdateValueLabel(newScale);
        }

        private void OnUserNameSubmit(string newUserName)
        {
            if (_userNameDisplay[0].text == newUserName)
                return;

            if (string.IsNullOrEmpty(newUserName))
                return;

            for (int i = 0; i < _userNameDisplay.Length; i++)
                _userNameDisplay[i].text = newUserName;

            if (_wsService.IsInitialized)
                StartCoroutine(_wsService.PostUsername(newUserName));
        }

        public void HandleCreateNewRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return;

            StartCoroutine(CreateValidRoom(roomName));
        }

        private IEnumerator CreateValidRoom(string validateRoomName)
        {
            bool isValidRoomName = false;

            yield return _wsService.FetchRooms(roomInfos =>
            {
                isValidRoomName = !roomInfos.Any(room => room.RoomName == validateRoomName);
            },
            error => Debug.LogWarning("Error fetching room list: " + error));

            if (isValidRoomName)
                ExitAndPrepareForNewRoom(validateRoomName);
        }

        public void OnRoomNameSubmit(string roomName)
        {
            if (roomName != _wsService.RoomName && !string.IsNullOrEmpty(roomName))
                StartCoroutine(ChangeRoomAndNotifyOther(roomName));
        }

        private IEnumerator ChangeRoomAndNotifyOther(string changeRoomName)
        {
            WebsocketMsg renameRoomMsg = new WebsocketMsg(WebsocketType.RenameRoom, changeRoomName);
            _wsService.BroadcastToClients(renameRoomMsg);

            yield return new WaitUntil(() => !_wsService.IsConnectedToOthers);

            _reconnectToRoom = true;
            ExitAndPrepareForNewRoom(changeRoomName);

            yield break;
        }

        private void AddRemoteView(string remoteWsId)
        {
            if (remoteWsId == _wsService.WsId || _remoteVideoGridLayout.Find(remoteWsId) != null)
                return;

            RemoteSource remoteSource = Instantiate(_remoteSourcePrefab, _remoteVideoGridLayout);

            remoteSource.Init(remoteWsId, GetGameViewLabelByWsId(remoteWsId));
            remoteSource.SetAudioParent(_remoteAudioGroup);
            remoteSource.SetRemoteViewClickEvent(OnRemoteViewClicked);
            _wsService.AddByteDataReceivedListener(remoteSource.VideoProcessData);
            _wsService.AddByteDataReceivedListener(remoteSource.AudioProcessData);
            StartCoroutine(GetUserName(remoteWsId, userNameData => remoteSource.UpdateUserName(userNameData.username)));

            RefreshAudioControlPanel();
        }

        private void OnRemoteViewClicked(string remoteWsId, AudioDecoder audio)
        {
            _wsService.SetMainRemoteEncoder(GetGameViewLabelByWsId(remoteWsId));
            SwitchRemoteViewMode(true);
        }

        public void SwitchRemoteViewMode(bool isFocus)
        {
            _mainRemoteViewPlate.gameObject.SetActive(isFocus);
            _gridRemoteViewPlate.gameObject.SetActive(!isFocus);
        }

        /// <summary>
        /// �ݭnRoomMaster�v��
        /// </summary>
        /// <param name="clientWsid">�s�s�u���Ȥ�ݪ� WS ID</param>
        public void OnClientConnected(string clientWsid)
        {
            //Log($"!OnClientConnected: Joined {clientWsid}");

            // ���s�[�J�������K�[�ѽX������
            AddRemoteView(clientWsid);

            // ����Ҧ��w�s������
            var connectedClients = _wsService.ConnectedClients;

            // �V�Ҧ��w�s�������M�s�����o�e������ WS ID
            for (int i = 0; i < connectedClients.Count; i++)
            {
                string inRoomWsId = connectedClients[i].wsid;

                if (inRoomWsId == clientWsid)
                {
                    SendGameViewDecoderMessage(clientWsid, _wsService.WsId);
                }
                else
                {
                    SendGameViewDecoderMessage(inRoomWsId, clientWsid);
                    SendGameViewDecoderMessage(clientWsid, inRoomWsId);
                }
            }
        }

        /// <summary>
        /// �o�e GameViewDecoder ����
        /// </summary>
        /// <param name="targetWsId">�����T�����ؼ� WS ID</param>
        /// <param name="messageWsId">�n�o�e�� WS ID (�n�i������H)</param>
        private void SendGameViewDecoderMessage(string targetWsId, string messageWsId)
        {
            WebsocketMsg newRemoteDecoder = new WebsocketMsg(WebsocketType.GameViewDecoder, messageWsId);
            _wsService.SendToClient(targetWsId, newRemoteDecoder);
        }

        /// <summary>
        /// �ݭnRoomMaster�v��
        /// </summary>
        /// <param name="roomName"></param>
        public void OnClientDisconnected(string clientWsid)
        {
            // �����_�u�������e���ѽX
            RemoveRemoteView(clientWsid);

            // �q����L�����Ӧ����_�u���T��
            var clientDisconnected = new WebsocketMsg(WebsocketType.ClientDisconnectNotify, clientWsid);
            _wsService.BroadcastToClients(clientDisconnected);
        }

        // Setting ���O�� Marker Offset Ĳ�o��ܮ�
        public void OnOffsetSubmit(string axis)
        {
            float currentOffset = axis switch
            {
                "X" => _model.MarkerOffsetX,
                "Y" => _model.MarkerOffsetY,
                "Z" => _model.MarkerDefaultDepth,
                _ => throw new ArgumentException("Invalid axis")
            };

            SetSubmitDialog($"�аO�I {axis} �b�첾", currentOffset.ToString(), (text) =>
            {
                float value = float.Parse(text);
                switch (axis)
                {
                    case "X": _model.MarkerOffsetX = value; break;
                    case "Y": _model.MarkerOffsetY = value; break;
                    case "Z": _model.MarkerDefaultDepth = value; break;
                }
                _markerShiftValue.text = _model.MarkerOffsetInfo;
            });
        }


        public void UpdateDeviceScale(SliderEventData eventData)
        {
            _wsService.SetMixedRealityVideoScale(eventData.NewValue);
            _deviceCamaraArgs[0].text = $"Scale: {eventData.NewValue}";
        }

        public void UpdateDeviceOffsetX(SliderEventData eventData)
        {
            _wsService.SetMixedRealityVideoOffset(offsetX: eventData.NewValue);
            _deviceCamaraArgs[1].text = $"OffsetX: {eventData.NewValue}";
        }

        public void UpdateDeviceOffsetY(SliderEventData eventData)
        {
            _wsService.SetMixedRealityVideoOffset(offsetY: eventData.NewValue);
            _deviceCamaraArgs[2].text = $"OffsetY: {eventData.NewValue}";
        }

        public void IncreaseSliderValue(Slider slider)
        {
            ChangeSliderValue(slider, 0.01f);
        }

        public void DecreaseSliderValue(Slider slider)
        {
            ChangeSliderValue(slider, -0.01f);
        }

        private void ChangeSliderValue(Slider slider, float increase)
        {
            if (slider == null) return;

            float newValue = slider.Value + increase;
            newValue = Mathf.Clamp(newValue, slider.MinValue, slider.MaxValue);
            slider.Value = newValue;
        }

        // �B�z���{�аO�ШD
        private void HandleRemoteMarkerRequest(ShapeData shape)
        {
            _model.DrawShape(shape);
            UpdateMarkerOptions();
        }

        public void HandleMarkerRedo()
        {
            _model.RedoShape();
            UpdateMarkerOptions();
        }

        public void HandleMarkerUndo()
        {
            _model.UndoShape();
            UpdateMarkerOptions();
        }

        public void HandleMarkerEraseAll()
        {
            _model.EraseAllMarker();
            UpdateMarkerOptions();
        }

        private void UpdateMarkerOptions()
        {
            _undoMarkerButton.enabled = _model.CanUndoMarker;
            _redoMarkerButton.enabled = _model.CanRedoMarker;
            _eraseAllMarkerButton.enabled = _model.CanUndoMarker || _model.CanRedoMarker;
        }
    }
}
