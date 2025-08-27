using Inspection.Core;
using Inspection.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskAssist.Dtos;
using TaskAssist.Utils;
using Unity.Extensions;
using UnityEngine;
using User.Core;
using User.Dtos;
using WorkOrder.Core;
using WorkOrder.Dtos;

namespace TaskAssist.Core
{
    public class ServiceManager : MonoBehaviour
    {
        // Pre-processing by unity inspector, will not change in runtime, 
        public string IBMS_PLATFORM_SERVER = "http://192.168.0.101:5020";
        public string BUILDING_CODE = "RG";

        // 專案所使用到的服務
        [SerializeField] private List<StatusColor> _statusColors;

        private InspectionIBMS _inspOrderService;
        private WorkOrderIBMS _workOrderService;

        private void Awake()
        {
            StatusColorConvert.InitMap(_statusColors);

            // LocalFileSystem 初始設定
            string _dataFolder = Path.Combine(Application.persistentDataPath, "Data");
            LocalFileSystem.SetDataFolder(_dataFolder);

            // 掛載的服務
            AuthService.Initialize(IBMS_PLATFORM_SERVER);
            _inspOrderService = new InspectionIBMS(IBMS_PLATFORM_SERVER, BUILDING_CODE);
            _workOrderService = new WorkOrderIBMS(IBMS_PLATFORM_SERVER, BUILDING_CODE);

            // APIHelper 初始設定
            APIHelper.OnAPILogCallback += DebugLogBase;
        }

        private void OnDestroy()
        {
            APIHelper.OnAPILogCallback -= DebugLogBase;
        }

        private void DebugLogBase(string message, bool isWarning = false)
        {
            string finalMessage = "[iBMS Service] " + message;
            if (!isWarning) Debug.Log(finalMessage);
            else Debug.LogWarning(finalMessage);
        }

        public bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;

        #region 使用者
        public async void ApplicationLoginUser(UserLoginIBMSPlatformDto userData, Action<bool> result)
            => result?.Invoke(await AuthService.LoginUserOnIBMSPlatform(userData));

        public async Task<UserPermissionDto> GetUserPermissionOnServer()
            => await AuthService.GetUserPermissionOnOnIBMSPlatform();

        public UserRole GetUserRoleByPermission(UserPermissionDto userPermission)
            => AuthService.GetUserRoleByPermission(userPermission);
        #endregion

        #region 巡檢
        public async void GetLocalInspOrders(Action<List<InspectionDto>> inspOrders)
            => inspOrders?.Invoke(await _inspOrderService.GetLocalInspectionListAsync());

        public void SaveInspOrders(List<InspectionDto> inspOrders)
            => _inspOrderService.SaveInspectionList(inspOrders);

        public void LoadServerInspOrders(Action<List<InspectionDto>> onLoaded)
            => _inspOrderService.LoadInspectionList(onLoaded, true);

        public async void GetNotUploadInspOrders(Action<Queue<OfflineUploadDto>> waitForUploads)
            => waitForUploads?.Invoke(await _inspOrderService.GetWaitUploadQueueAsync());

        public async Task<Queue<OfflineUploadDto>> GetNotUploadInspOrders()
            => await _inspOrderService.GetWaitUploadQueueAsync();

        public void SaveNotUploadInspOrders(Queue<OfflineUploadDto> waitForUploads)
            => _inspOrderService.SaveWaitUploadQueue(waitForUploads);

        public void PostInspOrderResult(List<KeyValue> formData, Action<OrderDeviceSubmitResponseDto> submitResponse)
            => _inspOrderService.SubmitOrder(formData, submitResponse);

        public void PostInspDeviceResult(List<KeyValue> formData, Action<OrderDeviceSubmitResponseDto> submitResponse)
            => _inspOrderService.SubmitDevice(formData, submitResponse);

        public string AddInspPhoto(string photoBase64)
            => _inspOrderService.AddPhoto(photoBase64);

        public void RemoveInspPhoto(string sns)
            => _inspOrderService.RemovePhoto(sns);

        public string GetInspPhotoBase64(string sns)
            => _inspOrderService.GetPhotoBase64(sns);

        public Texture2D GetInspPhotoTexture(string photoSn)
            => _inspOrderService.GetPhotoTexture(photoSn);

        public void UpdateInspPhotoSns(string localPhotoSns, string remotePhotoSns)
            => _inspOrderService.UpdatePhotoSns(localPhotoSns, remotePhotoSns);

        public void PostInspStart(List<KeyValue> formData, Action<string> submitResponse)
            => _inspOrderService.PostStartInspection(formData, submitResponse);

        public void PostInspDeviceUpdate(List<KeyValue> formData, Action<string> submitResponse)
            => _inspOrderService.PostUpdateDevice(formData, submitResponse);
        #endregion

        #region 工單
        public async void GetLocalWorkOrders(Action<List<WorkOrderDto>> workOrders)
            => workOrders?.Invoke(await _workOrderService.GetLocalWorkOrderListAsync());

        public void LoadServerWorkOrders(Action<List<WorkOrderDto>> onLoaded)
            => _workOrderService.LoadWorkOrderList(onLoaded, true);

        public void SaveWorkOrders(List<WorkOrderDto> workOrders)
            => _workOrderService.SaveWorkOrderList(workOrders);

        public async void GetNotUploadWorkOrders(Action<Queue<OfflineUploadDto>> waitForUploads)
            => waitForUploads?.Invoke(await _workOrderService.GetWaitUploadQueueAsync());

        public async Task<Queue<OfflineUploadDto>> GetNotUploadWorkOrders()
            => await _workOrderService.GetWaitUploadQueueAsync();

        public void SaveNotUploadWorkOrders(Queue<OfflineUploadDto> waitForUploads)
            => _workOrderService.SaveWaitUploadQueue(waitForUploads);

        public string AddWorkPhoto(string photoBase64)
            => _workOrderService.AddPhoto(photoBase64);

        public void RemoveWorkPhoto(string sns)
            => _workOrderService.RemovePhoto(sns);

        public void UpdateWorkPhotoSns(string localPhotoSns, string remotePhotoSns)
            => _workOrderService.UpdatePhotoSns(localPhotoSns, remotePhotoSns);

        public string GetWorkPhotoBase64(string sns)
            => _workOrderService.GetPhotoBase64(sns);

        public Texture2D GetWorkPhotoTexture(string photoSn)
            => _workOrderService.GetPhotoTexture(photoSn);

        public void PostWorkOrderStart(List<KeyValue> formData, Action<string> submitResponse)
            => _workOrderService.SubmitOrderStart(formData, submitResponse);

        public void PostPrePhotoSubmit(List<KeyValue> formData, Action<PrePhotoSubmitResponseDto> postPrePhotoSubmit)
            => _workOrderService.SubmitPrePhoto(formData, postPrePhotoSubmit);

        public void PostDeviceWorkSubmit(List<KeyValue> formData, Action<DeviceWorkSubmitResponseDto> deviceWorkSubmit)
            => _workOrderService.SubmitWorkDevice(formData, deviceWorkSubmit);

        public void PostWorkOrderSubmit(List<KeyValue> formData, Action<bool> workOrderSubmit)
            => _workOrderService.SubmitWorkOrder(formData, workOrderSubmit);
        #endregion
    }
}
