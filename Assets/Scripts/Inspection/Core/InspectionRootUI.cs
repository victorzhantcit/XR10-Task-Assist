using Inspection.Dtos;
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
using WorkOrder.Core;

#if WINDOWS_UWP
using Windows.Networking.Connectivity; // UWP 連線偵測使用
#endif

namespace Inspection.Core
{
    public class InspectionRootUI : MonoBehaviour
    {
        private readonly string DATE_FORMAT_DB = "yyyy-MM-dd HH:mm:ss";
        private readonly string DATE_FORMAT_LOCAL = "yyyy-MM-dd HH:mm";

        [Header("System")]
        [SerializeField] private ServiceManager _service;
        [SerializeField] private QRCodeDetector _qrCodeDetector;

        [Header("UI / Slate")]
        [SerializeField] private InspectionListView _inspectionListView;
        [SerializeField] private InspectionInfoView _inspectionInfoView;
        [SerializeField] private InspectionDeviceView _inspectionDeviceView;

        private readonly Stack<ViewType> _viewHistory = new Stack<ViewType>();
        private List<InspectionDto> _inspections;
        private InspectionDto _currentInspection;
        private OrderDeviceDto _currentDevice;

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

        public enum ViewType
        {
            Main,
            Info,
            Device,
            Screenshot,
            Hide
        }


        private void Awake() => _viewHistory.Push(ViewType.Main);
        private void Start() => Initialize();
        private void OnDestroy() => UnregisterEvents();

        private void Initialize()
        {
            RegisterEvents();
            SetView(ViewType.Main);
            SyncDataFromDatabase();
        }

        private void RegisterEvents()
        {
            if (_isEventRegistered) return;
            _isEventRegistered = true;
            Debug.Log("Register events");
            _inspectionListView.OnInspctionItemClicked += HandleInspectionItemClicked;
            _inspectionInfoView.OnDeviceItemClicked += HandleDeviceItemClicked;
            //_dataCenter.OrderDownloaded += HandleOrderDownloaded;
        }

        private void UnregisterEvents()
        {
            if (!_isEventRegistered) return;
            if (_inspectionListView != null) _inspectionListView.OnInspctionItemClicked -= HandleInspectionItemClicked;
            if (_inspectionInfoView != null) _inspectionInfoView.OnDeviceItemClicked -= HandleDeviceItemClicked;
            //if (_dataCenter != null) _dataCenter.OrderDownloaded -= HandleOrderDownloaded;
            Debug.Log("Unregister events");
            _isEventRegistered = false;
        }

        private void SetView(ViewType viewType)
        {
            _inspectionListView.SetVisible(viewType == ViewType.Main);
            _inspectionInfoView.SetVisible(viewType == ViewType.Info);
            _inspectionDeviceView.SetVisible(viewType == ViewType.Device);
            // 如果新頁面比最後紀錄來的更深層，則紀錄新的View
            if (viewType > _viewHistory.Peek()) _viewHistory.Push(viewType);
        }

        public void SwitchToPreviousView()
        {
            // 確保 _viewHistory 不會彈出到空 (始終保留第一個 View)
            if (_viewHistory.Count > 1) _viewHistory.Pop();
            SetView(_viewHistory.Peek());
        }

        private void HandleOrderDownloaded(bool success, List<InspectionDto> inspectionList, bool isLocalData)
        {
            if (isLocalData == false)
                ShowDialog((success) ? "已同步資料為最新狀態" : "資料下載失敗，請檢查網路或連繫管理員！", null, () => _inspectionListView.SetUpdateButtonEnable(true));
            UpdateInspectionData(inspectionList);
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

            try
            {
                var deviceIdentify = JsonConvert.DeserializeObject<StartInspectionDto>(parseResult);

                ValidateDeviceQrContent(deviceIdentify);
                Debug.Log("QR Code Matched");
            }
            catch (Exception ex)
            {
                ValidateDeviceQrContent(null);
                Debug.LogException(ex);
            }
        }

        public void UpdatePromptDialog(bool enabled, string message = "", bool isQrHint = false) 
            => _promptDialog.Setup(enabled, message, isQrHint, () => OnPromptClosed());

        private void OnPromptClosed()
        {
            EnableQRScanner(false);
            ShowSlateContent(true);
        }

        public void EnableQRScanner(bool enabled)
        {
            string hintText = enabled ? $"掃描 {_currentDevice.code} \n巡檢 QR Code" : string.Empty;

            UpdatePromptDialog(enabled, hintText, enabled);
            if (enabled)
                _qrCodeDetector.OnQrCodeDetected += HandleQrCodeDetected;
            else
                _qrCodeDetector.OnQrCodeDetected -= HandleQrCodeDetected;
            _qrCodeDetector.EnableArMarker(enabled);
        }

        public void ValidateDeviceQrContent(StartInspectionDto qrDeviceData)
        {
            // QR 並非目標設備，繼續掃描，是目標設備則設定狀態為處理中並進入設備檢查介面
            if (qrDeviceData == null || !IsQrMatchTargetDevice(qrDeviceData))
                ShowDialog($"此並非設備 {_currentDevice.code} 的 QR Code", null, () => ShowSlateContent(true));
            else
                ShowDialog($"正確的設備，是否開始檢查？", null, () => PostStartInspection(), () => ShowSlateContent(true));
        }

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

        private bool IsQrMatchTargetDevice(StartInspectionDto qrDeviceData) =>
            qrDeviceData.BuildingCode == _currentInspection.buildingCode &&
            qrDeviceData.DeviceCode == _currentDevice.deviceCode;

        public void UpdateInspectionData(List<InspectionDto> inspectionList)
        {
            if (inspectionList == null) return;

            _inspections = inspectionList;
            _inspectionListView.UpdateData(_inspections);
        }

        public void SyncDataFromDatabase()
        {
            _service.GetLocalInspOrders(inspOrders => HandleOrderDownloaded(inspOrders != null, inspOrders, true));

            _inspectionListView.SetUpdateButtonEnable(false);

            if (_service.IsNetworkAvailable) HandleSyncWhenOnline();
            else HandleSyncWhenOffline();
        }

        //private List<(InspectionDto inspection, int deviceIndex)> GetNotUploadedDevices()
        //{
        //    if (_inspections == null) return new List<(InspectionDto inspection, int deviceIndex)>();

        //    return _inspections
        //        .Where(inspection => inspection.IsDeviceNotUploaded)
        //        .SelectMany((inspection, inspectionIndex) => inspection.orderDevices
        //            .Select((device, deviceIndex) => (inspection, device, deviceIndex))
        //            .Where(tuple => tuple.device.IsNotUploaded)
        //            .Select(tuple => (tuple.inspection, tuple.deviceIndex)))
        //        .ToList();
        //}

        private void HandleSyncWhenOnline()
        {
            _service.GetNotUploadInspOrders(waitForUploads =>
            {
                Debug.Log($"WaitForUpload? {waitForUploads.Count > 0}");
                if (waitForUploads.Count > 0)
                    ReconnectNotUploadedCheck(waitForUploads);
                else
                    LoadServerInspectionList();
            });
        }

        private void LoadServerInspectionList()
        {
            _service.LoadServerInspOrders(inspOrders => HandleOrderDownloaded(inspOrders != null, inspOrders, false));
        }

        private async void ReconnectNotUploadedCheck(Queue<OfflineUploadDto> waitForUpload)
        {
            if (waitForUpload.Count == 0)
            {
                Debug.Log("No pending uploads in the queue.");
                _inspectionListView.SetUpdateButtonEnable(true);
                LoadServerInspectionList();
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

            _service.SaveNotUploadInspOrders(waitForUpload); // 保存更新後的隊列狀態
            if (waitForUpload.Count > 0)
            {
                Debug.Log($"Stopping upload process when uploading! Successfully uploaded {initialCount - waitForUpload.Count} items, remain {waitForUpload.Count}.");
                _dialogPoolHandler.ShowDialog("上傳成功", $"{initialCount - waitForUpload.Count} 個待上傳操作已上傳完畢，{waitForUpload.Count} 個無法上傳");
            }
            else
            {
                Debug.Log($"Reconnect and upload complete. Successfully uploaded {initialCount - waitForUpload.Count} items");
                LoadServerInspectionList();
            }

            _inspectionListView.SetUpdateButtonEnable(true);
        }

        public void DeviceRespondInputClicked()
            => _submitDialog.Setup("設備巡檢總結", null, (message) => _inspectionDeviceView.OnDeviceRespondTextChanged(message));

        public void ReuploadRejectedButtonClicked() 
        {
            _submitDialog.Setup("完工說明", null, (message) =>
            {
                if (string.IsNullOrEmpty(message)) return;

                ResubmitOrderDto resubmitOrderDto = new ResubmitOrderDto(_currentInspection, message);
                List<KeyValue> requestData = KeyValueConverter.ToKeyValues(resubmitOrderDto);

                _service.PostInspOrderResult(requestData, (response) => HandleOrderSumbitResponse(response));
            });
        }

        private void HandleOrderSumbitResponse(OrderDeviceSubmitResponseDto response)
        {
            DateTime submitDateTime;

            if (response == null)
            {
                submitDateTime = DateTime.Now;
                ShowDialog("上傳至雲端時發生錯誤", "已保存資料於本機，請確認已連線至網路，待網路恢復後再上傳");
            }
            else
            {
                submitDateTime = DateTime.ParseExact(response.SubmitTime, DATE_FORMAT_DB, null);
                ShowDialog($"退件單 {_currentInspection.recordSn} 已重新上傳！");
            }

            _currentInspection.rejectTime = null;
            _currentInspection.comment = null;
            _currentInspection.SetStatusToSubmitted(submitDateTime.ToString(DATE_FORMAT_LOCAL));

            SwitchToPreviousView();
        }

        public void SubmitDeviceButtonClicked() => SubmitDeviceInspectResult();

        private void SubmitDeviceInspectResult(Action reuploadCallback = null)
        {
            DeviceSubmitDto deviceUploadDto = new DeviceSubmitDto(_service.GetInspPhotoBase64);

            bool validUploadFormat = deviceUploadDto.CheckAndSetDeviceUploadDto(
                _currentInspection.recordSn,
                _currentDevice,
                _currentInspection.orderDevices.Count,
                _currentDevice.afPhotoSns
            );

            if (!validUploadFormat)
            {
                ShowDialog(
                    $"{_currentInspection.recordSn} {_currentDevice.code} 巡檢資料未完整",
                    deviceUploadDto.InvalidResult
                );
                reuploadCallback?.Invoke();
                return;
            }

            List<KeyValue> requestData = KeyValueConverter.ToKeyValues(deviceUploadDto);

            _service.PostInspDeviceResult(requestData, (response) => HandleDeviceSumbitResponse(response));
        }

        private void HandleDeviceSumbitResponse(OrderDeviceSubmitResponseDto response)
        {
            Debug.Log("response is null " + response == null);
            if (response == null)
            {
                HandleOfflineDeviceSubmitData();
                return;
            }

            DateTime submitDateTime = DateTime.ParseExact(response.SubmitTime, DATE_FORMAT_DB, null);

            _currentDevice.SetStatusToSubmitted();
            _currentDevice.manMinute = response.ManMinute;
            _currentDevice.submitTime = submitDateTime.ToString(DATE_FORMAT_LOCAL);
            _currentDevice.afPhotoSns = response.PhotoSns;

            string[] responsePhotoSns = response.PhotoSns.Split(',');

            for (int i = 0; i < responsePhotoSns.Length; i++)
            {
                string remotePhotoSns = responsePhotoSns[i];
                string localPhotoSns = _inspectionDeviceView.DevicePhotoSns[i];

                _service.UpdateInspPhotoSns(localPhotoSns, remotePhotoSns);
                _inspectionDeviceView.DevicePhotoSns[i] = remotePhotoSns;
            }

            Debug.Log($"成功上傳 {_currentDevice.deviceDescription} 的巡檢紀錄！");
            UpdateSubmittedInfoView();
            SwitchToPreviousView();

            // 儲存資料
            _service.SaveInspOrders(_inspections);

            ShowDialog($"成功上傳 {_currentDevice.deviceDescription} 的巡檢紀錄！");
        }

        private void HandleOfflineDeviceSubmitData()
        {
            DateTime deviceStartTime = DateTime.ParseExact(_currentDevice.startTime, DATE_FORMAT_LOCAL, null);
            DateTime currrentDateTime = DateTime.Now;
            TimeSpan deviceManTime = currrentDateTime - deviceStartTime;

            _currentDevice.SetStatusToNotUploaded();
            _currentDevice.manMinute = (int)deviceManTime.TotalMinutes;
            _currentDevice.submitTime = currrentDateTime.ToString(DATE_FORMAT_LOCAL);
            _currentDevice.afPhotoSns = string.Join(',', _inspectionDeviceView.DevicePhotoSns);

            ShowDialog("上傳至雲端時發生錯誤", "已保存資料於本機，請確認已連線至網路，待網路恢復後再上傳");
        }

        private void UpdateSubmittedInfoView()
        {
            Debug.Log("UpdateSubmittedInfoView " + _currentInspection.sn);

            bool isAllDeviceSubmitted = true;
            int isNotUploadCount = 0;
            int isNotSubmittedCount = 0;

            // 檢查每個設備的狀態
            for (int i = 0; i < _currentInspection.orderDevices.Count; i++)
            {
                OrderDeviceDto device = _currentInspection.orderDevices[i];

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
            if (isNotUploadCount > 0) _currentInspection.SetStatusToNotUploaded();
            else if (isNotSubmittedCount > 0) _currentInspection.SetStatusToProcessing(_currentDevice.submitTime);
            else if (isAllDeviceSubmitted) 
            {
                if (string.IsNullOrEmpty(_currentInspection.rejectTime))
                    _currentInspection.SetStatusToSubmitted(_currentDevice.submitTime);
                else
                    _currentInspection.SetStatusToRejected();
            }

            // 更新 UI 顯示
            _inspectionInfoView.SetInfo(_currentInspection);
            _inspectionListView.UpdateData(_inspections);

            // 儲存資料
            _service.SaveInspOrders(_inspections);
        }

        public void PostStartInspection(Action supplementCallback = null)
        {
            StartInspectionDto startInspectData = new StartInspectionDto();

            startInspectData.Initialize(_currentInspection.recordSn, _currentDevice);

            List<KeyValue> requestData = KeyValueConverter.ToKeyValues(startInspectData);

            if (string.IsNullOrEmpty(_currentInspection.rejectTime))
                _service.PostInspStart(requestData, (response) => HandleStartTimeResponse(response, supplementCallback));
            else
                _service.PostInspDeviceUpdate(requestData, (response) => HandleUpdateRejectedInspection(response, supplementCallback));
        }

        private void HandleStartTimeResponse(string responseTime, Action supplementCallback = null)
        {
            bool isOfflineValidated = responseTime == null;
            string dateTimeString = string.Empty;

            if (isOfflineValidated) dateTimeString = DateTime.Now.ToString(DATE_FORMAT_LOCAL);
            else dateTimeString = DateTime.ParseExact(responseTime, DATE_FORMAT_DB, null).ToString(DATE_FORMAT_LOCAL);

            ShowSlateContent(true);

            _currentInspection.SetStatusToProcessing(dateTimeString);
            _currentDevice.SetStatusToProcessing(dateTimeString, isOfflineValidated);

            _inspectionInfoView.UpdateInfoViewStatusAndDate();

            _service.SaveInspOrders(_inspections);
            _inspectionDeviceView.SetDetails(_currentDevice);
            SetView(ViewType.Device);


            supplementCallback?.Invoke();
        }

        private void HandleUpdateRejectedInspection(string responseTime, Action supplementCallback = null)
        {
            bool isOfflineValidated = responseTime == null;
            string dateTimeString = DateTime.Now.ToString(DATE_FORMAT_LOCAL);

            _currentInspection.SetStatusToProcessing(dateTimeString);
            _currentDevice.UpdateStatusWhenRejected(isOfflineValidated);

            _inspectionInfoView.UpdateInfoViewStatusAndDate();

            _service.SaveInspOrders(_inspections);
            _inspectionDeviceView.SetDetails(_currentDevice);
            SetView(ViewType.Device);

            supplementCallback?.Invoke();
        }

        private void HandleSyncWhenOffline()
        {
            ShowDialog("資料同步失敗", "請等待恢復網路環境後，再透過主頁面更新資料", () => _inspectionListView.SetUpdateButtonEnable(true));
            UpdateInspectionData(_inspections);
        }

        private void HandleInspectionItemClicked(int itemIndex)
        {
            _currentInspection = _inspections[itemIndex];
            _inspectionInfoView.SetInfo(_currentInspection);
            SetView(ViewType.Info);
        }

        private void HandleDeviceItemClicked(int itemIndex)
        {
            _currentDevice = _currentInspection.orderDevices[itemIndex];

            if (_currentDevice.IsPending) StartValidateDevice();
            else EnterDeviceView();
        }

        private void StartValidateDevice()
        {
            ShowSlateContent(false);
            EnableQRScanner(true);
        }

        private void EnterDeviceView()
        {
            _inspectionDeviceView.SetDetails(_currentDevice, !string.IsNullOrEmpty(_currentInspection.rejectTime));
            SetView(ViewType.Device);
        }

        public Color PositiveColor => GetColorByStatus("c-processed");
        public Color NegativeColor => GetColorByStatus("c-pending");
        public Color NeuralColor => GetColorByStatus("c-processing");
        public Color DefaultColor => GetColorByStatus("Default");

        public Color GetColorByStatus(string status)
        {
            LabelColorSet colorSet = StatusColorConvert.GetLabelColorSet(status);
            return colorSet.Colors.BaseColor;
        }

        public void ShowDialog(string title, string message = null, Action confirmAction = null, Action cancelAction = null) 
            => _dialogPoolHandler.ShowDialog(title, message, confirmAction, cancelAction);

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

        ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
        {
            isConnected = true;
            signalStrength = GetSignalStrength(profile); // 取得網路訊號強度
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
                _service.GetNotUploadInspOrders(waitForUploads =>
                {
                    if (waitForUploads.Count > 0)
                        ReconnectNotUploadedCheck(waitForUploads);
                });
            };

            _previousConnection = isConnected;
            //Debug.Log($"Network available: {isConnected}, signal: {signalStrength}");

            int spriteIndex = Mathf.Clamp(signalStrength, 1, _wifiStatusSprites.Length - 1);
            _qualityIcon.sprite = (isConnected) ? _wifiStatusSprites[spriteIndex] : _wifiStatusSprites[0]; // 無網路連線狀態
            _qualityIcon.color = (isConnected) ? Color.white : Color.yellow;
            _qualityText.color = _qualityIcon.color;
            _qualityText.text = (isConnected) ? "Wifi 訊號" : "無訊號";
        }
    }
}
