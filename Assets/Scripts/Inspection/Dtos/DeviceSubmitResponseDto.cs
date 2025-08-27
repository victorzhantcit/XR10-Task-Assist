namespace Inspection.Dtos
{
    // Order Submit的回傳也由這個Dto處理，僅會缺失PhotoSns
    public class OrderDeviceSubmitResponseDto
    {
        public string Result { get; set; }
        public string SubmitTime { get; set; }
        public string PhotoSns { get; set; }
        public int ManMinute { get; set; }
    }
}
