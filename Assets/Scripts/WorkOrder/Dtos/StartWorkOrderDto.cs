using System;
using System.Diagnostics;

namespace WorkOrder.Dtos
{
    public class StartWorkOrderDto
    {
        public string BuildingCode { get; set; }
        public string RecordSn { get; set; }
        public string DeviceCode { get; set; }
        public string NowTime { get; set; }

        public StartWorkOrderDto() { }

        public void Initialize(string recordSn, OrderDeviceDto device)
        {
            BuildingCode = device.buildingCode;
            RecordSn = recordSn;
            DeviceCode = device.deviceCode;
            NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }
    }
}
