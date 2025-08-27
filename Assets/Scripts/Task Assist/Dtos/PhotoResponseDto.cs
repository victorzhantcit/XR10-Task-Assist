using Newtonsoft.Json;

namespace TaskAssist.Dtos
{
    public class PhotoResponseDto
    {
        [JsonProperty("sn")]
        public string Sn;
        [JsonProperty("buildingCode")]
        public string BuildingCode;
        [JsonProperty("recordSn")]
        public string RecordSn;
        [JsonProperty("createTime")]
        public string CreateTime;
        [JsonProperty("size")]
        public string Size;
        [JsonProperty("photo")]
        public string Photo;
    }
}
