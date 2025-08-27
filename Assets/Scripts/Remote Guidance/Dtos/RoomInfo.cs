using Newtonsoft.Json;

namespace Guidance.Dtos
{
    // 用於接收房間的類
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

        // 覆寫 Equals 方法
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

        // 覆寫 GetHashCode 方法
        public override int GetHashCode()
        {
            // 合併 RoomMasterWSID 和 StartTime 的哈希碼，避免哈希碰撞
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
