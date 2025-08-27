using Newtonsoft.Json;
using System;
using WorkOrder.Core;

namespace WorkOrder.Dtos
{
    public class DeviceWorkSubmitDto
    {
        public string RecordSn { get; set; }
        public string RecordType { get; set; }
        public string BuildingCode { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceCount { get; set; }
        public string Respond { get; set; }
        public string Photos { get; set; }
        public string NowTime { get; set; }

        private ValidateStatus _validFormat = ValidateStatus.InvalidRespond;

        public enum ValidateStatus
        {
            InvalidRespond,
            InvalidPhoto,
            Pass
        }

        public bool SetupAndValidate(DeviceWorkSubmitType recordType, WorkOrderDto workOrder, OrderDeviceDto device, Func<string, string> getPhotoBase64)
        {
            if (InvalidAfPhotoFormat(recordType, device.afPhotoSns))
            {
                _validFormat = ValidateStatus.InvalidPhoto;
                return false;
            }

            if (InvalidRespond(device.respond))
            {
                _validFormat = ValidateStatus.InvalidRespond;
                return false;
            } 

            RecordSn = workOrder.recordSn;
            RecordType = ((int)recordType).ToString();
            BuildingCode = workOrder.buildingCode;
            DeviceCode = device.deviceCode;
            DeviceCount = workOrder.orderDevices.Count.ToString();
            Respond = device.respond;
            Photos = ConvertPhotoSns(device.afPhotoSns, getPhotoBase64);
            NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            return true;
        }

        public ValidateStatus GetValidateStatus() => _validFormat;

        private bool InvalidRespond(string respond) => respond == "Start" || string.IsNullOrEmpty(respond);
        private bool InvalidAfPhotoFormat(DeviceWorkSubmitType recordType, string afPhotoSns)
            => recordType == DeviceWorkSubmitType.Submit && string.IsNullOrEmpty(afPhotoSns);

        private string ConvertPhotoSns(string photoSns, Func<string, string> getPhotoBase64)
        {
            if (string.IsNullOrEmpty(photoSns)) return "[]";

            string[] photoSnsList = photoSns.Split(',');
            string[] photoBase64List = new string[photoSnsList.Length];

            for (int i = 0; i < photoSnsList.Length; i++)
            {
                string photoBase64 = getPhotoBase64(photoSnsList[i]);
                photoBase64List[i] = photoBase64;
            }

            return "[" + string.Join(',', photoBase64List) + "]";
        }
    }

    public class DeviceWorkSubmitResponseDto
    {
        public string Result;
        public string AfPhotoSns;
    }

    public enum DeviceWorkSubmitType
    {
        Record,
        Pause,
        Submit
    }
}
