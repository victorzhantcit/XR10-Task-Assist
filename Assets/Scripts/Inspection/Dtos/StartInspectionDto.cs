using System;

namespace Inspection.Dtos
{
    public class StartInspectionDto
    {
        public string BuildingCode { get; set; }
        public string RecordSn { get; set; }
        public string DeviceCode { get; set; }
        public string NowTime { get; set; }

        public void Initialize(string recordSn, OrderDeviceDto device)
        {
            BuildingCode = device.buildingCode;
            RecordSn = recordSn;
            DeviceCode = device.deviceCode;
            NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }
    }
}
