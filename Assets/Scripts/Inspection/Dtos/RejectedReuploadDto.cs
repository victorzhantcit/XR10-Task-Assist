using Newtonsoft.Json;
using System;

namespace Inspection.Dtos
{
    public class ResubmitOrderDto
    {
        public string BuildingCode { get; set; }
        public string RecordSn { get; set; }
        public string CompletionReport { get; set; }
        public string NowTime { get; set; }

        [JsonConstructor]
        public ResubmitOrderDto() { }

        public ResubmitOrderDto(InspectionDto inspection, string message)
        {
            BuildingCode = inspection.buildingCode;
            RecordSn = inspection.recordSn;
            CompletionReport = message;
            NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }
    }
}
