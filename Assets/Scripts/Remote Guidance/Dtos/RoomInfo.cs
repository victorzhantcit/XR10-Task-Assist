using Newtonsoft.Json;

namespace Guidance.Dtos
{
    // �Ω󱵦��ж�����
    [System.Serializable]
    public class RoomInfo
    {
        [JsonProperty("roomName")]
        public string RoomName;
        [JsonProperty("roomMasterWSID")]
        public string RoomMasterWSID;
        [JsonProperty("clients")]
        public int Clients;
        [JsonProperty("startTime")]
        public string StartTime;

        // �мg Equals ��k
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            RoomInfo other = (RoomInfo)obj;
            return string.Equals(RoomName, other.RoomName) &&
                string.Equals(RoomMasterWSID, other.RoomMasterWSID) &&
                Clients == other.Clients &&
                string.Equals(StartTime, other.StartTime);
        }

        // �мg GetHashCode ��k
        public override int GetHashCode()
        {
            // �X�� RoomMasterWSID �M StartTime �����ƽX�A�קK���ƸI��
            return (RoomMasterWSID, StartTime).GetHashCode();
        }

        public void Init()
        {
            RoomName = "Room";
            RoomMasterWSID = "Host";
            Clients = 0;
            StartTime = "DNF";
        }
    }
}
