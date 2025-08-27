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

        // �ˬd�W�Ǯ榡�A�]�t�ˬd���ؿ�ܡB�Ϥ��B�`�����ˬd
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
                InvalidResult = "�п�J�`���A�L���D�мgok";
                return false;
            }

            if (string.IsNullOrEmpty(photoSns))
            {
                InvalidResult = "�Ъ��[����ҩ��I";
                return false;
            }

            // �N�r����Φ� List<string>
            List<string> photoSnsList = photoSns
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // �p�G�M�欰�šA��^���~
            if (photoSnsList.Count == 0)
            {
                InvalidResult = "�Ъ��[����ҩ��I";
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

        // �ˬd�Ҧ� CheckItem �O�_����
        private string ValidateCheckItems(List<CheckItemDto> checkItems)
        {
            // �ˬd�O�_�� null �ΪŦC��
            if (checkItems == null || checkItems.Count == 0)
                return "�ˬd���ئC���šI";

            // �ˬd�C�� CheckItem
            for (int i = 0; i < checkItems.Count; i++)
            {
                CheckItemDto item = checkItems[i];
                if (item.status.ContainsKey(item.selected)) continue;

                return $"�� {i + 1} ��������ˬd���G�I";
            }

            return string.Empty; // �Ҧ����س�����
        }

        // �N CheckItem �C���ഫ�� UploadCheckItemDto �C��
        private string ConvertCheckItemsToDtoList(List<CheckItemDto> checkItems)
        {
            var uploadItemList = new List<SubmitCheckItemDto>();

            // �M���C�� CheckItem �öi���ഫ
            for (int i = 0; i < checkItems.Count; i++)
            {
                CheckItemDto item = checkItems[i];
                var uploadItem = new SubmitCheckItemDto();

                // ��l�� UploadCheckItemDto�A�ñN��K�[��C��
                uploadItem.InitializeContent(item);
                uploadItemList.Add(uploadItem);
            }

            // �N�C���ഫ�� JSON �r�Ŧ�ê�^
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

        // �N CheckItem ���ݩʭȽ�Ȩ� DTO ��
        public void InitializeContent(CheckItemDto item)
        {
            // �N CheckItem ���ݩʭȽƻs�� UploadCheckItemDto
            ItemName = item.itemName;
            Status = new Dictionary<string, string>(item.status); // �`���� Dictionary
            Method = item.method;
            Running = item.running;
            Reference = item.reference;
            Selected = item.selected;
            Note = item.note;
        }
    }
}
