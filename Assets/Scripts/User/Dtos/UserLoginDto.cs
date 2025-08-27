using Newtonsoft.Json;

namespace User.Dtos
{
    public class UserLoginIBMSPlatformDto
    {
        [JsonProperty("account")]
        public string Id = string.Empty;
        [JsonProperty("pw")]
        public string Password = string.Empty;

        [JsonConstructor]
        public UserLoginIBMSPlatformDto() { }

        public UserLoginIBMSPlatformDto(string id, string password)
        {
            this.Id = id;
            this.Password = password;
        }

        public string Print() => $"Id : {Id}, Password: {Password}";
    }
}
