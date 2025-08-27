using Inspection.Dtos;
using Inspection.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskAssist.Dtos;
using TaskAssist.Utils;
using Unity.Extensions;
using UnityEngine;

namespace Inspection.Core
{
    public class InspectionIBMS
    {
        // Local storage
        private readonly string _inspectionListFile = "InspectionList.json";
        private readonly string _inspectionPhotosFile = "InspectionPhotos.json";
        private readonly string _waitForUploadFile = "Inspection_WaitForUpload.json";

        // Server info
        private string SERVER_PORT = string.Empty;
        private string BUILDING_CODE = string.Empty;
        private string API_Orders => $"{SERVER_PORT}/api/Eqpt/InspOrdersAll";
        private string API_Photo => $"{SERVER_PORT}/api/Eqpt/Photo";
        private string API_StartInsp => $"{SERVER_PORT}/api/Eqpt/StartInsp";
        private string API_UpdateDevice => $"{SERVER_PORT}/api/Eqpt/UpdateInspDevice";
        private string API_SubmitDevice => $"{SERVER_PORT}/api/Eqpt/SubmitInspDevice";
        private string API_SubmitOrder => $"{SERVER_PORT}/api/Eqpt/SubmitInspOrder";

        public bool IsReturnPureString(string url) {
            string cleanedUrl = url.Trim();
            List<string> returnPureStringAPIs = new List<string>
            {
                API_StartInsp
            };

            return returnPureStringAPIs.Contains(url);
        }

        private IBMSPhotosStorage _photoMap = new IBMSPhotosStorage();

        public InspectionIBMS(string serverPort, string buildingCode)
        {
            SERVER_PORT = serverPort;
            BUILDING_CODE = buildingCode;
            LocalFileSystem.CheckOrCreateFile(_inspectionListFile, new List<InspectionDto>());
            LocalFileSystem.CheckOrCreateFile(_inspectionPhotosFile, new IBMSPhotosStorage());
            InitPhotoMap();
        }

        private async void InitPhotoMap()
        {
            _photoMap = await GetLocalPhotosAsync();
        }

        #region LoadInspectionList
        public void LoadInspectionList(Action<List<InspectionDto>> onLoaded, bool skipLocal = false)
        {
            APIHelper.LoadDataAsync(
                loadLocalData: GetLocalInspectionListAsync,
                loadServerData: GetServerInspectionListAsync,
                onDataLoaded: onLoaded,
                saveData: SaveInspectionList,
                skipLocal: skipLocal
            );
        }

        public async Task<List<InspectionDto>> GetLocalInspectionListAsync()
            => await LocalFileSystem.GetLocalDataAsync<List<InspectionDto>>(_inspectionListFile);

        public void SaveInspectionList(List<InspectionDto> data)
            => LocalFileSystem.SaveData(data, _inspectionListFile);

        private async Task<List<InspectionDto>> GetServerInspectionListAsync()
        {
            DateTime now = DateTime.Now;
            int timeSpanDays = 90;
            string startDate = now.AddDays(-timeSpanDays).ToString("yyyy-MM-dd");
            string endDate = now.AddDays(timeSpanDays).ToString("yyyy-MM-dd");
            List<KeyValue> requestData = new List<KeyValue>
            {
                new KeyValue("BuildingCode", BUILDING_CODE),
                new KeyValue("StartDate", startDate),
                new KeyValue("EndDate", endDate)
            };

            var inspOrders = await APIHelper.SendServerFormRequestAsync<List<InspectionDto>>(API_Orders, HttpMethod.POST, requestData);

            if (inspOrders == null) return null;

            for (int i = 0; i < inspOrders.Count; i++)
                await ParseAndLoadPhoto(inspOrders[i].photoSns);

            SaveInspectionList(inspOrders);
            return inspOrders;
        }
        #endregion

        #region Photo
        public async Task<IBMSPhotosStorage> GetLocalPhotosAsync()
            => await LocalFileSystem.GetLocalDataAsync<IBMSPhotosStorage>(_inspectionPhotosFile);

        public void SavePhotoMap(IBMSPhotosStorage photoMap)
            => LocalFileSystem.SaveData(photoMap, _inspectionPhotosFile);

        // API: /api/Eqpt/GetPhoto
        private async Task<PhotoResponseDto> GetServerPhotoAsync(string photoSn, bool refreshedToken = false)
        {
            List<KeyValue> requestData = new List<KeyValue> {{ new KeyValue("Sn", photoSn) }};
            return await APIHelper.SendServerFormRequestAsync<PhotoResponseDto>(API_Photo, HttpMethod.GET, requestData, refreshedToken);
        }

        // 多圖片請以","分隔代號 因為目前API只能吃一個Sn 回傳一張Base64圖片
        public async Task ParseAndLoadPhoto(string photoSnsString)
        {
            //Debug.Log("[*] PhotoSnsString: " + photoSnsString);
            if (_photoMap == null)
                _photoMap = await GetLocalPhotosAsync();

            if (string.IsNullOrEmpty(photoSnsString) || _photoMap.PhotoHashMap.ContainsKey(photoSnsString))
            {
                return;
            }

            string[] photoSnsArray = photoSnsString.Split(',');

            for (int i = 0; i < photoSnsArray.Length; i++)
            {
                string cachePhotoSn = photoSnsArray[i];

                if (_photoMap.ContainsPhotoSns(cachePhotoSn))
                {
                    Debug.Log($"[InspectionIBMS] Photo cache contains photoSn {cachePhotoSn}");
                    continue;
                }

                PhotoResponseDto photo = await GetServerPhotoAsync(cachePhotoSn);
                string photoBase64 = photo.Photo;

                if (string.IsNullOrEmpty(photoBase64))
                {
                    Debug.LogWarning($"[InspectionIBMS] PhotoSn {cachePhotoSn} server response is null or empty");
                    continue;
                }

                _photoMap.AddPhoto(cachePhotoSn, photoBase64);
                SavePhotoMap(_photoMap);
            }
        }

        public Texture2D GetPhotoTexture(string photoSn)
        {
            string photoBase64 = _photoMap.GetPhotoData(photoSn);

            if (string.IsNullOrEmpty(photoBase64))
                return null;

            byte[] imageBytes = Convert.FromBase64String(photoBase64);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(imageBytes))
                return texture;

            return null;
        }

        public string GetPhotoBase64(string sns)
        {
            if (_photoMap.PhotoHashMap.ContainsKey(sns))
                return _photoMap.PhotoHashMap[sns];

            return null;
        }

        public string AddPhoto(string photoBase64)
        {
            string newLocalSns = _photoMap.SavePhotoAndReturnSns(photoBase64);

            SavePhotoMap(_photoMap);
            return newLocalSns;
        }

        public void UpdatePhotoSns(string oldSns, string newSns)
        {
            _photoMap.UpdatePhotoSns(oldSns, newSns);
            SavePhotoMap(_photoMap);
        }

        public void RemovePhoto(string sns)
        {
            _photoMap.RemovePhotoBySns(sns);
            SavePhotoMap(_photoMap);
        }
        #endregion

        #region WaitForUpload
        public async Task<Queue<OfflineUploadDto>> GetWaitUploadQueueAsync()
        {
            var waitForUploadList = await LocalFileSystem.GetLocalDataAsync<List<OfflineUploadDto>>(_waitForUploadFile);
            
            if (waitForUploadList != null) 
                return new Queue<OfflineUploadDto>(waitForUploadList);
            else 
                return new Queue<OfflineUploadDto>();
        }

        public void SaveWaitUploadQueue(Queue<OfflineUploadDto> waitForUploadQueue)
        {
            List<OfflineUploadDto> waitForUploadList = new List<OfflineUploadDto>(waitForUploadQueue);
            LocalFileSystem.SaveData(waitForUploadList, _waitForUploadFile);
        }  

        public async Task AddWaitUpload(string url, List<KeyValue> data)
        {
            Queue<OfflineUploadDto> waitForUploadQueue = await GetWaitUploadQueueAsync();
            waitForUploadQueue.Enqueue(new OfflineUploadDto { Url = url, Data = data });
            SaveWaitUploadQueue(waitForUploadQueue);
        }
        #endregion

        #region SubmitData
        public void PostStartInspection(List<KeyValue> formData, Action<string> callback)
            => PostWithFallback<string>(API_StartInsp, formData, callback);

        public void PostUpdateDevice(List<KeyValue> formData, Action<string> callback)
            => PostWithFallback<string>(API_UpdateDevice, formData, callback);

        public void SubmitDevice(List<KeyValue> formData, Action<OrderDeviceSubmitResponseDto> callback)
            => PostWithFallback<OrderDeviceSubmitResponseDto>(API_SubmitDevice, formData, callback);

        public void SubmitOrder(List<KeyValue> formData, Action<OrderDeviceSubmitResponseDto> callback)
            => PostWithFallback<OrderDeviceSubmitResponseDto>(API_SubmitOrder, formData, callback);

        private async void PostWithFallback<T>(string url, List<KeyValue> formData, Action<T> callback)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                _ = AddWaitUpload(url, formData);
                callback?.Invoke(default(T));
                return;
            }

            T result = await APIHelper.SendServerFormRequestAsync<T>(url, HttpMethod.POST, formData, returnPureString: IsReturnPureString(url));

            callback?.Invoke(result);
        }
        #endregion
    }
}
