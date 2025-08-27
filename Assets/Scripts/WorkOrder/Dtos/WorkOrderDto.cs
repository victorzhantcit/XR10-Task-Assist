using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WorkOrder.Dtos
{
    [Serializable]
    public class WorkOrderDto
    {
        public string statusColor { get; set; }
        public string statusBGColor { get; set; }
        public int statusType { get; set; }
        public bool show { get; set; }
        public string sn { get; set; }
        public string buildingName { get; set; }
        public string buildingCode { get; set; }
        public string orderType { get; set; }
        public int? inspSn { get; set; }
        public string recordSn { get; set; }
        public string forms { get; set; }
        public string description { get; set; }
        public string executorName { get; set; }
        public string staffName { get; set; }
        public string scheduledDate { get; set; }
        public int priority { get; set; }
        public string status { get; set; }
        public string startTime { get; set; }
        public string submitTime { get; set; }
        public string rejectTime { get; set; }
        public string comment { get; set; }
        public string completeTime { get; set; }
        public int manMinute { get; set; }
        public List<OrderDeviceDto> orderDevices { get; set; }
        public string photoSns { get; set; }

        [JsonIgnore]
        public bool IsRejected => status == "Reject";
        [JsonIgnore]
        public bool IsDeviceNotUploaded => status == "NotUploaded";
        [JsonIgnore]
        public bool IsApproving => status == "Approving";

        public void SetStatusToProcessing(string startTime)
        {
            if (status == "Pending")
                this.startTime = startTime;

            status = "Processing";
        }

        public void SetStatusToDone()
        {
            status = "Done";
        }

        public void SetStatusToRejected()
        {
            status = "Reject";
        }

        public void SetStatusToSubmitted(string submitTime)
        {
            status = "Submitted";
            this.submitTime = submitTime;
        }

        public void SetStatusToNotUploaded()
        {
            status = "NotUploaded";
        }

        [JsonIgnore]
        public string translatedStatus => Translate(status, statusTranslations);

        [JsonIgnore]
        public string translatedOrderType => Translate(orderType, orderTypeTranslations);

        // �q�Ϊ�½Ķ��k
        private string Translate(string key, Dictionary<string, string> translations) => translations.TryGetValue(key, out var translatedValue) ? translatedValue : key;

        // ���A½Ķ��
        private static readonly Dictionary<string, string> statusTranslations = new Dictionary<string, string>
        {
            { "Pending", "�ݳB�z" },
            { "Changed", "��u��" },
            { "Processing", "�B�z��" },
            { "Submitted", "���u�W��" },
            { "Done", "�w����" },
            { "Approving", "�Ю֤�"},
            { "Reject", "�h��" },
            { "Completed", "�w����"},
            { "NotUploaded", "���ݤW��" },
            { "Pause", "�w�ȵ�"}
        };

        // �q������½Ķ��
        private static readonly Dictionary<string, string> orderTypeTranslations = new Dictionary<string, string>
        {
            { "Plan", "�~�׭p�e" },
            { "Single", "�{�ɨ��˳�" }
        };
    }

    public class OrderDeviceDto
    {
        public string buildingCode { get; set; }
        public string system { get; set; }
        public string type { get; set; }
        public string deviceCode { get; set; }
        public string code { get; set; }
        public string deviceDescription { get; set; }
        public string manufacturer { get; set; }
        public string modelNumber { get; set; }
        public string warrantyEndDate { get; set; }
        public string warrantyTime { get; set; }
        public string executor { get; set; }
        public string status { get; set; }
        public string startTime { get; set; }
        public string pauseTime { get; set; }
        public string submitTime { get; set; }
        public string completeTime { get; set; }
        public int estManMinute { get; set; }
        public int manMinute { get; set; }
        public string prePhotoSns { get; set; }
        public string afPhotoSns { get; set; }
        public List<FromDto> froms { get; set; }
        public List<int> formSns { get; set; }
        public List<string> formNames { get; set; }
        public List<CheckItemDto> items { get; set; }
        public int summarize { get; set; }
        public List<DeviceNumericalDto> numericalData { get; set; }
        public string dataTime { get; set; }
        public List<ConsumableDto> consumables { get; set; }
        public int workRecordsdSn { get; set; }
        public string respond { get; set; }
        public List<RecordDto> records { get; set; }
        public bool isDisabled { get; set; }

        [JsonIgnore]
        public bool IsPending => status == "Pending";
        [JsonIgnore]
        public bool IsPause => status == "Pause";
        [JsonIgnore]
        public bool IsSubmitted => status == "Submitted";
        [JsonIgnore]
        public bool IsNotUploaded => status == "NotUploaded";
        [JsonIgnore]
        public bool IsProcessing => status == "Processing";

        private bool _isOfflineVadicated = true; // ����쬰�P�_�b"�ɤW��"�ɬO�_�ݭn�I�s"�}�l����"��api

        public void SetStatusToProcessing(string startTime, bool isOfflineVadicated)
        {
            status = "Processing";
            _isOfflineVadicated = isOfflineVadicated;
            if (status == "Pending") this.startTime = startTime;            
        }

        public void UpdateStatusWhenRejected(bool isOfflineVadicated)
        {
            status = "Processing";
            _isOfflineVadicated = isOfflineVadicated;
        }

        public bool IsOfflineVadicated => _isOfflineVadicated;

        public void SetStatusToSubmitted() => status = "Submitted";

        public void SetStatusToNotUploaded() => status = "NotUploaded";

        public void SetStatusToPause() => status = "Pause";

        public void SetDefaultStatusIfNull()
        {
            if (string.IsNullOrEmpty(status))
                status = "Pending";
        }

        [JsonIgnore]
        public string translatedStatus
        {
            get => Translate(status, statusTranslations);
            set => status = ReverseTranslate(value, statusTranslations);
        }

        // �q�Ϊ�½Ķ��k
        private string Translate(string key, Dictionary<string, string> translations)
        {
            return translations.TryGetValue(key, out var translatedValue) ? translatedValue : key;
        }

        // �q�Ϊ��ϦV½Ķ��k
        private string ReverseTranslate(string translatedKey, Dictionary<string, string> translations)
        {
            foreach (var pair in translations)
            {
                if (pair.Value == translatedKey)
                {
                    return pair.Key;
                }
            }
            return translatedKey;
        }

        // ���A½Ķ��
        private static readonly Dictionary<string, string> statusTranslations = new Dictionary<string, string>
        {
            { "Pending", "�ݳB�z" },
            { "Submitted", "�w�B�z" },
            { "Processing", "�B�z��" },
            { "NotUploaded", "���ݤW��" },
            { "Pause", "�w�ȵ�"}
        };
    }

    public class FromDto
    {
        public string orderType { get; set; }
        public string fromAnOrder { get; set; }
        public string orderDescription { get; set; }
    }

    public class CheckItemDto
    {
        public string itemName { get; set; }
        public Dictionary<string, string> status { get; set; }
        public string method { get; set; }
        public string running { get; set; }
        public string reference { get; set; }
        public string selected { get; set; }
        public string note { get; set; }

        [JsonIgnore]
        public string translatedRunning
        {
            get
            {
                if (running == "True")
                    return "�Ұ�";
                else if (running == "False")
                    return "����";
                else
                    return "--";
            }
        }

        [JsonIgnore]
        public string TranslatedPositiveStatus
        {
            get => status["0"];
        }

        [JsonIgnore]
        public string TranslatedNegativeStatus
        {
            get => status["1"];
        }

        public void SetStatusPositive()
        {
            selected = "0";
        }

        public void SetStatusNegative()
        {
            selected = "1";
        }

        [JsonIgnore]
        public bool IsPositiveSelected => selected == "0";

        [JsonIgnore]
        public bool IsNegativeSelected => selected == "1";
    }

    public class DeviceNumericalDto
    {
        public string tagName { get; set; }
        public string tagUnit { get; set; }
        public string tagDescription { get; set; }
        public string value { get; set; }
    }

    public class ConsumableDto
    {
        public bool isChecked { get; set; }
        public string name { get; set; }
        public string replaceDate { get; set; }
        public int availableNum { get; set; }
        public string availableUnit { get; set; }
    }

    public class RecordDto
    {
        public string respondTime { get; set; }
        public string staffName { get; set; }
        public string respond { get; set; }
        public string status { get; set; }
        public int manMinute { get; set; }

        [JsonIgnore]
        public string translatedStatus
        {
            get => Translate(status, statusTranslations);
            set => status = ReverseTranslate(value, statusTranslations);
        }

        // �q�Ϊ�½Ķ��k
        private string Translate(string key, Dictionary<string, string> translations)
        {
            return translations.TryGetValue(key, out var translatedValue) ? translatedValue : key;
        }

        // �q�Ϊ��ϦV½Ķ��k
        private string ReverseTranslate(string translatedKey, Dictionary<string, string> translations)
        {
            foreach (var pair in translations)
            {
                if (pair.Value == translatedKey)
                {
                    return pair.Key;
                }
            }
            return translatedKey;
        }

        // ���A½Ķ��
        private static readonly Dictionary<string, string> statusTranslations = new Dictionary<string, string>
        {
            { "Pending", "�ݳB�z" },
            { "Submitted", "�w�B�z" },
            { "Processing", "�B�z��" },
            { "NotUploaded", "���ݤW��" },
            { "Pause", "�w�ȵ�"}
        };
    }
}
