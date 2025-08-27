using System;

namespace WorkOrder.Dtos
{
    public class WorkOrderSubmitDto
    {
        public string BuildingCode { get; set; }
        public string RecordSn { get; set; }
        public string CompletionReport { get; set; }
        public string NowTime { get; set; }

        public bool SetupAndValidate(WorkOrderDto workOrderDto, string respond)
        {
            if (string.IsNullOrEmpty(respond)) return false;

            BuildingCode = workOrderDto.buildingCode;
            RecordSn = workOrderDto.recordSn;
            CompletionReport = respond;
            NowTime = DateTime.Now.ToString();
            return true;
        }
    }
}
