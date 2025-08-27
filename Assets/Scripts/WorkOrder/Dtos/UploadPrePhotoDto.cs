using System;
using WorkOrder.Core;
using WorkOrder.Dtos;
using static WorkOrder.Dtos.DeviceWorkSubmitDto;

public class PrePhotoSubmitDto
{
    public string RecordSn { get; set; }
    public string BuildingCode { get; set; }
    public string DeviceCode { get; set; }
    public string Photos { get; set; }
    public string WorkRecordsdSn { get; set; }
    public string NowTime { get; set; }

    public bool SetupAndValidate(string recordSn, OrderDeviceDto deviceDto, Func<string, string> getPhotoBase64)
    {
        if (string.IsNullOrEmpty(deviceDto.prePhotoSns)) return false;

        RecordSn = recordSn;
        BuildingCode = deviceDto.buildingCode;
        DeviceCode = deviceDto.deviceCode;
        Photos = ConvertPhotoSns(deviceDto.prePhotoSns, getPhotoBase64);
        WorkRecordsdSn = deviceDto.workRecordsdSn.ToString();
        NowTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        return true;
    }

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

public class PrePhotoSubmitResponseDto
{
    public string Result;
    public string PhotoSns;
    public string WorkRecordsdSn;
}