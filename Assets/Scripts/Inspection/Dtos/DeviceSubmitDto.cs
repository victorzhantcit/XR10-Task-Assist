using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inspection.Dtos
{
    public class DeviceSubmitDto
    {
        public string BuildingCode { get; set; }
        public string RecordSn { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceCount { get; set; }
        public string FormSns { get; set; }
        public string Summarize { get; set; }
        public string Result { get; set; }
        public string Items { get; set; }
        public string Photos { get; set; }
        public string NowTime { get; set; }

        private static Func<string, string> _getPhotoBase64Func;

        // 檢查上傳格式，包含檢查項目選擇、圖片、總結的檢查
        [JsonIgnore]
        public string InvalidResult = string.Empty;

        [JsonConstructor]
        public DeviceSubmitDto()
        {

        }

        public DeviceSubmitDto(Func<string, string> getPhotoBase64Func)
        {
            _getPhotoBase64Func = getPhotoBase64Func;
        }

        public bool CheckAndSetDeviceUploadDto(string recordSn, OrderDeviceDto device, int deviceCount, string photoSns)
        {
            string checkItemInvalid = ValidateCheckItems(device.items);
            if (!string.IsNullOrEmpty(checkItemInvalid))
            {
                InvalidResult = checkItemInvalid;
                return false;
            }

            if (string.IsNullOrEmpty(device.respond) || device.respond == "Start")
            {
                InvalidResult = "請輸入總結，無問題請寫ok";
                return false;
            }

            if (string.IsNullOrEmpty(photoSns))
            {
                InvalidResult = "請附加拍照證明！";
                return false;
            }

            // 將字串分割成 List<string>
            List<string> photoSnsList = photoSns
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // 如果清單為空，返回錯誤
            if (photoSnsList.Count == 0)
            {
                InvalidResult = "請附加拍照證明！";
                return false;
            }

            BuildingCode = device.buildingCode;
            RecordSn = recordSn;
            DeviceCode = device.deviceCode;
            DeviceCount = deviceCount.ToString();
            FormSns = string.Join(",", device.formSns);
            Summarize = HasAnyWarningItems(device.items);
            Result = device.respond;
            Items = ConvertCheckItemsToDtoList(device.items);
            Photos = ConvertPhotoSns(photoSnsList);
            NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            return true;
        }

        private string HasAnyWarningItems(List<CheckItemDto> deviceItems)
        {
            bool anySelectedWarning = deviceItems.Any(item => item.selected != "0");
            return anySelectedWarning ? "1" : "0";
        }

        // 檢查所有 CheckItem 是否有效
        private string ValidateCheckItems(List<CheckItemDto> checkItems)
        {
            // 檢查是否為 null 或空列表
            if (checkItems == null || checkItems.Count == 0)
                return "檢查項目列表為空！";

            // 檢查每個 CheckItem
            for (int i = 0; i < checkItems.Count; i++)
            {
                CheckItemDto item = checkItems[i];
                if (item.status.ContainsKey(item.selected)) continue;

                return $"第 {i + 1} 項未選擇檢查結果！";
            }

            return string.Empty; // 所有項目都有效
        }

        // 將 CheckItem 列表轉換為 UploadCheckItemDto 列表
        private string ConvertCheckItemsToDtoList(List<CheckItemDto> checkItems)
        {
            var uploadItemList = new List<SubmitCheckItemDto>();

            // 遍歷每個 CheckItem 並進行轉換
            for (int i = 0; i < checkItems.Count; i++)
            {
                CheckItemDto item = checkItems[i];
                var uploadItem = new SubmitCheckItemDto();

                // 初始化 UploadCheckItemDto，並將其添加到列表中
                uploadItem.InitializeContent(item);
                uploadItemList.Add(uploadItem);
            }

            // 將列表轉換為 JSON 字符串並返回
            return JsonConvert.SerializeObject(uploadItemList);
        }

        private string ConvertPhotoSns(List<string> photoSnsList)
        {
            List<string> photoBase64List = new List<string>();

            for (int i = 0; i < photoSnsList.Count; i++)
            {
                string photoBase64 = _getPhotoBase64Func(photoSnsList[i]);
                photoBase64List.Add(photoBase64);
            }

            return "[" + string.Join(',', photoBase64List) + "]";
        }

    }

    public class SubmitCheckItemDto
    {
        public string ItemName { get; set; }
        public Dictionary<string, string> Status { get; set; }
        public string Method { get; set; }
        public string Running { get; set; }
        public string Reference { get; set; }
        public string Selected { get; set; }
        public string Note { get; set; }

        // 將 CheckItem 的屬性值賦值到 DTO 中
        public void InitializeContent(CheckItemDto item)
        {
            // 將 CheckItem 的屬性值複製到 UploadCheckItemDto
            ItemName = item.itemName;
            Status = new Dictionary<string, string>(item.status); // 深拷貝 Dictionary
            Method = item.method;
            Running = item.running;
            Reference = item.reference;
            Selected = item.selected;
            Note = item.note;
        }
    }
}
