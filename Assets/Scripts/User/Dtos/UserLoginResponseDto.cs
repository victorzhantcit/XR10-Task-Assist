using Newtonsoft.Json;
using System.Collections.Generic;

namespace User.Dtos
{
    public class UserLoginResponseDto
    {
        [JsonProperty("statusCode")]
        public int StatusCode;
        [JsonProperty("token")]
        public string Token;
        [JsonProperty("expireTime")]
        public string ExpireTime;
        [JsonProperty("roles")]
        public List<string> Roles;

        [JsonProperty("name")]
        public string Username;

        [JsonIgnore]
        public bool IsWorker => Roles.Contains("worker");
        [JsonIgnore]
        public bool IsInspector => Roles.Contains("insp");

        public string Print()
        {
            return $"statusCode : {StatusCode}, token: {Token}";
        }
    }
}
