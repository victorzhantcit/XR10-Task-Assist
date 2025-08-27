using Guidance.Core.XR;
using MRTK.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaskAssist.Core;
using TaskAssist.Dtos;
using TaskAssist.Utils;
using TMPro;
using Unity.Extensions;
using UnityEngine;
using WorkOrder.Dtos;

#if WINDOWS_UWP
using Windows.Networking.Connectivity; // UWP 連線偵測使用
#endif

namespace WorkOrder.Core
{
    public class WorkOrderRootUI : MonoBehaviour
    {
        public readonly string DATE_FORMAT_DB = "yyyy-MM-dd HH:mm:ss";
        public readonly string DATE_FORMAT_LOCAL = "yyyy-MM-dd HH:mm";

        [Header("System")]
        [SerializeField] private ServiceManager _service;
        [SerializeField] private QRCodeDetector _qrCodeDetector;

        [Header("Remote Guidance")]
        [SerializeField] private RemoteGuidanceSlate _remoteGuidanceSlate;

        [Header("UI / Slate")]
        [SerializeField] private WorkOrderListView _workOrderListView;
        [SerializeField] private WorkOrderInfoView _workOrderInfoView;
        [SerializeField] private WorkOrderDeviceView _workOrderDeviceView;

        private readonly Stack<ViewType> _viewHistory = new Stack<ViewType>();
        private List<WorkOrderDto> _workOrders;
        private WorkOrderDto _currentWorkOrder;
        private OrderDeviceDto _currentDevice;
        public string CurrentRecordSn => _currentWorkOrder.recordSn;
        public WorkOrderDto CurrentWorkOrder => _currentWorkOrder;
        public OrderDeviceDto CurrentDevice => _currentDevice;

        [Header("UI / Dialog")]
        [SerializeField] private DialogPoolHandler _dialogPoolHandler;
        [SerializeField] private CanvasInputFieldDialog _submitDialog;

        [Header("UI / QRCode & Hint")]
        [SerializeField] private TransparentPromptDialog _promptDialog;

        [Header("Network Quality Display")]
        [SerializeField] private TMP_Text _qualityText;
        [SerializeField] private SpriteRenderer _qualityIcon;
        [SerializeField] Sprite[] _wifiStatusSprites; // 訊號圖示表示由小到大

        private UnityEngine.Ping ping;
        private bool _previousConnection = true;
        private bool _isEventRegistered = false;
        private Coroutine _networkChecking = null;

        //private List<string> _currentDevicePhotoSns = new List<string>(); 

        private enum ViewType
        {
            Main,
            Info,
            Device,
            Hide
        }


        private void Awake() => _viewHistory.Push(ViewType.Main);
        private void Start() => Initialize();
        private void OnDestroy() => UnregisterEvents();

        private void Initialize()
        {
            RegisterEvents();
            SyncDataFromDatabase();
            SetRemoteSupportTitle();
        }

        private void RegisterEvents()
        {
            if (_isEventRegistered) return;
            _isEventRegistered = true;
            Debug.Log("Register events");
            _workOrderListView.OnOrderItemClicked += HandleOrderItemClicked;
            _workOrderInfoView.OnDeviceItemClicked += HandleDeviceItemClicked;
        }

        private void UnregisterEvents()
        {
            if (!_isEventRegistered) return;
            if (_workOrderListView != null) _workOrderListView.OnOrderItemClicked -= HandleOrderItemClicked;
            Debug.Log("Unregister events");
            _isEventRegistered = false;
        }

        private void HandleOrderDownloaded(bool success, List<WorkOrderDto> workOrders, bool isLocalData)
        {
            if (isLocalData == false)
                ShowDialog((success) ? "已同步資料為最新狀態" : "資料下載失敗，請檢查網路或連繫管理員！", null, () => _workOrderListView.SetUpdateButtonEnable(true));
            UpdateWorkOrderData(workOrders);
        }

        private void HandleOrderItemClicked(int index)
        {
            _currentWorkOrder = _workOrders[index];
            _workOrderInfoView.SetInfo(_currentWorkOrder);
            SetView(ViewType.Info);
            SetRemoteSupportTitle(_currentWorkOrder.recordSn);
        }

        private void HandleDeviceItemClicked(int index)
        {
            _currentDevice = _currentWorkOrder.orderDevices[index];

            if (_currentDevice.IsPending || _currentDevice.IsPause) StartValidateDevice();
            else EnterDeviceView();


            SetRemoteSupportTitle(_currentWorkOrder.recordSn, _currentDevice.code);
        }

        private void EnterDeviceView()
        {
            _workOrderDeviceView.SetDetails(_currentDevice);
            SetView(ViewType.Device);
        }

        private void StartValidateDevice()
        {
            ShowSlateContent(false);
            EnableQRScanner(true);
        }

        // 因為面板會跑協程 因此不能直接使用 gameObject.SetActive(bool)
        public void ShowSlateContent(bool enabled)
        {
            if (gameObject.activeSelf == enabled) return;
            if (enabled)
            {
                this.gameObject.SetActive(true);
                // 確保所有子物件可視
                foreach (Transform slateChild in this.transform)
                    slateChild.gameObject.SetActive(true);
                if (_networkChecking == null) _networkChecking = StartCoroutine(NetworkDetectingLoop());
                SwitchToPreviousView();
            }
            else
            {
                SetView(ViewType.Hide);
                // 確保所有子物件可視
                foreach (Transform slateChild in this.transform)
                    slateChild.gameObject.SetActive(false);
                if (_networkChecking != null)
                {
                    StopCoroutine(_networkChecking);
                    _networkChecking = null;
                }

                this.gameObject.SetActive(false);
            }
        }

        public void EnableQRScanner(bool enabled)
        {
            Debug.Log("EnableQRScanner" + enabled);
            string hintText = enabled ? $"掃描 {_currentDevice.code} \n巡檢 QR Code" : string.Empty;

            UpdatePromptDialog(enabled, hintText, enabled);
            if (enabled)
                _qrCodeDetector.OnQrCodeDetected += HandleQrCodeDetected;
            else
                _qrCodeDetector.OnQrCodeDetected -= HandleQrCodeDetected;
            _qrCodeDetector.EnableArMarker(enabled);
        }

        public void HandleQrCodeDetected(string qrContent)
        {
            Debug.Log("QR Code content: " + qrContent);

            UpdatePromptDialog(false);
            EnableQRScanner(false);

            // 取得 qrContent 中的 jsonData，範例: qrContent = https://ip:port/?queryPara={jsonData}
            Match match = Regex.Match(qrContent, @"\{(.*)\}");

            if (!match.Success)
            {
                ValidateDeviceQrContent(null);
                Debug.Log("QR Code Not Match");
                return;
            }

            string parseResult = match.Value;
            Debug.Log("Parse " + parseResult);
            Debug.Log("Current device " + JsonConvert.SerializeObject(_currentDevice));
            try
            {
                var deviceIdentify = JsonConvert.DeserializeObject<StartWorkOrderDto>(parseResult);

                ValidateDeviceQrContent(deviceIdentify);
                Debug.Log("QR Code Matched");
            }
            catch (Exception ex)
            {
                ValidateDeviceQrContent(null);
                Debug.LogException(ex);
            }
        }

        public void ValidateDeviceQrContent(StartWorkOrderDto qrDeviceData)
        {

            // QR 並非目標設備，繼續掃描，是目標設備則設定狀態為處理中並進入設備檢查介面
            if (qrDeviceData == null || !IsQrMatchTargetDevice(qrDeviceData))
                ShowDialog($"此並非設備 {_currentDevice.code} 的 QR Code", null, () => ShowSlateContent(true));
            else
                ShowDialog($"正確的設備，是否開始檢查？", null, () => PostStartWorkOrder(), () => ShowSlateContent(true));
        }

        private bool IsQrMatchTargetDevice(StartWorkOrderDto qrDeviceData) =>
            qrDeviceData.BuildingCode == _currentWorkOrder.buildingCode &&
            qrDeviceData.DeviceCode == _currentDevice.deviceCode;

        public void PostStartWorkOrder(Action supplementCallback = null)
        {
            StartWorkOrderDto startWorkData = new StartWorkOrderDto();
            startWorkData.Initialize(_currentWorkOrder.recordSn, _currentDevice);

            List<KeyValue> requestData = KeyValueConverter.ToKeyValues(startWorkData);
            _service.PostWorkOrderStart(requestData, (response) => HandleStartTimeResponse(response, supplementCallback));
        }

        private void HandleStartTimeResponse(string responseTime, Action supplementCallback = null)
        {
            bool isOfflineValidated = responseTime == null;
            string dateTimeString = string.Empty;

            if (isOfflineValidated) dateTimeString = DateTime.Now.ToString(DATE_FORMAT_LOCAL);
            else dateTimeString = DateTime.ParseExact(responseTime, DATE_FORMAT_DB, null).ToString(DATE_FORMAT_LOCAL);

            // 更新工單與設備狀態資料、保存目前資料至本地
            _currentWorkOrder.SetStatusToProcessing(dateTimeString);
            _currentDevice.SetStatusToProcessing(dateTimeString, isOfflineValidated);
            _currentDevice.records.Add(new RecordDto()
            {
                respondTime = dateTimeString,
                status = "Processing",
                staffName = "我",
                respond = "Start"
            });
            _service.SaveWorkOrders(_workOrders);

            // 更新工單與設備UI
            _workOrderInfoView.UpdateInfoViewStatusAndDate();
            ShowSlateContent(true);
            _workOrderDeviceView.SetDetails(_currentDevice);
            SetView(ViewType.Device);

            supplementCallback?.Invoke();
        }

        public void UpdatePromptDialog(bool enabled, string message = "", bool isQrHint = false)
            => _promptDialog.Setup(enabled, message, isQrHint, (isQrHint) ? () => OnPromptClosed() : null);

        private void OnPromptClosed()
        {
            EnableQRScanner(false);
            ShowSlateContent(true);
        }

        private void SetView(ViewType viewType)
        {
            _workOrderListView.SetVisible(viewType == ViewType.Main);
            _workOrderInfoView.SetVisible(viewType == ViewType.Info);
            _workOrderDeviceView.SetVisible(viewType == ViewType.Device);
            // 如果新頁面比最後紀錄來的更深層，則紀錄新的View
            if (viewType > _viewHistory.Peek()) _viewHistory.Push(viewType);
        }

        public void SwitchToPreviousView()
        {
            // 確保 _viewHistory 不會彈出到空 (始終保留第一個 View)
            if (_viewHistory.Count > 1) _viewHistory.Pop();
            SetView(_viewHistory.Peek());
        }

        public void SyncDataFromDatabase()
        {
            if (_service.IsNetworkAvailable) HandleSyncWhenOnline();
            else HandleSyncWhenOffline();
        }

        private void HandleSyncWhenOffline()
        {
            _service.GetLocalWorkOrders(workOrders =>
            {
                UpdateWorkOrderData(workOrders);
                _workOrderListView.SetUpdateButtonEnable(false);
                ShowDialog("資料同步失敗", "請等待恢復網路環境後，再透過主頁面更新資料", () => _workOrderListView.SetUpdateButtonEnable(true));

            });
        }

        public void UpdateWorkOrderData(List<WorkOrderDto> workOrders)
        {
            if (workOrders == null) return;

            _workOrders = workOrders;
            _workOrderListView.UpdateData(_workOrders);
        }

        private void HandleSyncWhenOnline()
        {
            _service.GetNotUploadWorkOrders(waitForUploads =>
            {
                Debug.Log($"WaitForUpload? {waitForUploads.Count > 0}");
                if (waitForUploads.Count > 0)
                    ReconnectNotUploadedCheck(waitForUploads);
                else
                    LoadServerWorkOrderList();
            });
        }

        public void UpdateSubmittedDeviceData()
        {
            bool isAllDeviceSubmitted = true;
            int isNotUploadCount = 0;
            int isNotSubmittedCount = 0;

            // 檢查每個設備的狀態
            for (int i = 0; i < _currentWorkOrder.orderDevices.Count; i++)
            {
                OrderDeviceDto device = _currentWorkOrder.orderDevices[i];

                if (device.IsNotUploaded)
                {
                    Debug.Log($"device.IsNotUploaded {device.code}");
                    isNotUploadCount++;
                    isAllDeviceSubmitted = false; // 如果有未上傳設備，設置為 false
                    continue;
                }
                if (!device.IsSubmitted)
                {
                    isNotSubmittedCount++;
                    isAllDeviceSubmitted = false; // 如果有未提交設備，設置為 false
                    continue;
                }
            }

            // 非退件狀態更新
            if (isNotUploadCount > 0) _currentWorkOrder.SetStatusToNotUploaded();
            else if (isNotSubmittedCount > 0) _currentWorkOrder.SetStatusToProcessing(_currentDevice.submitTime);
            else if (isAllDeviceSubmitted) _workOrderInfoView.UpdateWorkOrderStateByDevice();

            // 儲存資料
            _service.SaveWorkOrders(_workOrders);
        }

        public Color PositiveColor => GetColorByStatus("Positive");
        public Color NegativeColor => GetColorByStatus("Negative");
        public Color DefaultColor => GetColorByStatus("Default");

        public Color GetColorByStatus(string status)
        {
            LabelColorSet labelColorSet = StatusColorConvert.GetLabelColorSet(status);
            return labelColorSet.Colors.BaseColor;
        }

        public void ShowDialog(string title, string message = null, Action confirmAction = null, Action cancelAction = null) 
            => _dialogPoolHandler.ShowDialog(title, message, confirmAction, cancelAction);

        public void ShowSubmitDialog(string title, string defaultValue, Action<string> response)
            => _submitDialog.Setup(title, defaultValue, response);

        private void UpdateItem<TComponent, TData>(List<TData> dataList, GameObject item, int index, Action<TComponent, TData, int> setContentAction) where TComponent : MonoBehaviour
        {
            if (index < 0 || index >= dataList.Count) return;
            if (item.TryGetComponent(out TComponent itemScript))
                setContentAction?.Invoke(itemScript, dataList[index], index);
        }

        private IEnumerator NetworkDetectingLoop()
        {
            while (true)
            {
                yield return CheckNetworkQuality();
                yield return new WaitForSeconds(5f);
            }
        }

        private IEnumerator CheckNetworkQuality()
        {
#if WINDOWS_UWP
            // 在 HoloLens 2（UWP）上使用 Windows 網路 API 檢測網路狀態
            bool isConnected = false;
            int signalStrength = 0;

            try
            {
                ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                {
                    isConnected = true;
                    signalStrength = GetSignalStrength(profile); // 取得網路訊號強度
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Network error: {ex.Message}");
            }

            // 顯示網路強度（UWP 的情况）
            DisplayNetworkQuality(isConnected, signalStrength);
#else
            // 在編輯器或非 UWP 環境中使用 Ping 檢測網路強度
            if (!_service.IsNetworkAvailable)
            {
                DisplayUnityNetworkQuality(false, 0);
                yield break;
            }

            ping = new UnityEngine.Ping("8.8.8.8"); // 開始 Ping Google DNS 伺服器
            while (!ping.isDone)
            {
                yield return null; // 等待 Ping 結果
            }

            int pingTime = ping.time; // 取得 Ping 的時間
            DisplayUnityNetworkQuality(true, pingTime);
#endif
            yield break; // 無論任何平台 最後都要終止線程
        }

#if WINDOWS_UWP
    // 取得訊號強度的簡單模擬，使用網路狀態列的值(0-5)
    private int GetSignalStrength(ConnectionProfile profile)
    {
        var signalBars = profile.GetSignalBars();
        if (signalBars.HasValue)
        {
            return signalBars.Value;
        }
        return 0; // 沒有訊號時返回 0
    }
#endif

        // 在非 UWP 環境中透過 Ping 時間，轉換數值後呼叫顯示網路品質方法
        private void DisplayUnityNetworkQuality(bool isConnect, int pingTime)
        {
            // 以標準網路強度，尋找適合的網路狀態圖示索引 0ms / 50ms / 100ms
            int signalStrength = 5 - Mathf.RoundToInt((pingTime / 200f) * 5);
            DisplayNetworkQuality(isConnect, signalStrength);
        }

        // 顯示網路品質方法，再恢復網路時檢查"等待上傳"的數量
        private void DisplayNetworkQuality(bool isConnected, int signalStrength)
        {
            if (!_previousConnection && isConnected)
            {
                _service.GetNotUploadWorkOrders(waitForUploads =>
                {
                    if (waitForUploads.Count > 0)
                        ReconnectNotUploadedCheck(waitForUploads);
                });
            }

            _previousConnection = isConnected;

            int spriteIndex = Mathf.Clamp(signalStrength, 1, _wifiStatusSprites.Length - 1);
            _qualityIcon.sprite = (isConnected) ? _wifiStatusSprites[spriteIndex] : _wifiStatusSprites[0]; // 無網路連線狀態
            _qualityIcon.color = (isConnected) ? Color.white : Color.yellow;
            _qualityText.color = _qualityIcon.color;
            _qualityText.text = (isConnected) ? "Wifi 訊號" : "無訊號";

            //Debug.Log($"Network available: {isConnected}, signal: {signalStrength}");
        }

        private void LoadServerWorkOrderList()
        {
            _service.LoadServerWorkOrders(workOrders => HandleOrderDownloaded(workOrders != null, workOrders, false));
        }

        private async void ReconnectNotUploadedCheck(Queue<OfflineUploadDto> waitForUpload)
        {
            if (waitForUpload.Count == 0)
            {
                Debug.Log("No pending uploads in the queue.");
                _workOrderListView.SetUpdateButtonEnable(true);
                LoadServerWorkOrderList();
                return;
            }

            Debug.Log($"Found {waitForUpload.Count} items pending upload. Attempting to reconnect and upload...");

            int initialCount = waitForUpload.Count;

            while (waitForUpload.Count > 0)
            {
                var uploadItem = waitForUpload.Peek(); // 取出隊列最前面的元素但不移除

                // 調用 PostRequest 進行上傳
                var uploadResponseStatusCode = await APIHelper.SendServerFormRequestAsync<int>
                (
                    uploadItem.Url,
                    HttpMethod.POST,
                    uploadItem.Data,
                    returnHttpStatus: true
                );

                if (uploadResponseStatusCode >= 200 && uploadResponseStatusCode <= 299)
                {
                    Debug.Log($"Upload successful for: {uploadItem.Url}");
                    waitForUpload.Dequeue(); // 成功後移除該項
                }
                else
                {
                    Debug.LogWarning($"Upload failed for: {uploadItem.Url}. Response was empty.");
                    break; // 如果上傳失敗，退出循環
                }
            }

            _service.SaveNotUploadWorkOrders(waitForUpload); // 保存更新後的隊列狀態
            if (waitForUpload.Count > 0)
            {
                Debug.Log($"Stopping upload process when uploading! Successfully uploaded {initialCount - waitForUpload.Count} items, remain {waitForUpload.Count}.");
                _dialogPoolHandler.ShowDialog("補上傳失敗", $"{initialCount - waitForUpload.Count} 個待上傳操作已上傳完畢，{waitForUpload.Count} 個無法上傳");
            }
            else
            {
                Debug.Log($"Reconnect and upload complete. Successfully uploaded {initialCount - waitForUpload.Count} items");
                LoadServerWorkOrderList();
            }

            _workOrderListView.SetUpdateButtonEnable(true);
        }

        public void SetRemoteSupportTitle(string workOrderRecordSn = "", string deviceCode = "")
        {
            string roomName = $"[機保01] {workOrderRecordSn} {deviceCode}";
            _remoteGuidanceSlate.QuickStartRoomName = roomName;
        }
    }
}
