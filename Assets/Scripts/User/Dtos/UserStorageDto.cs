using System;

namespace User.Dtos
{
    public enum UserRole
    {
        Staff,
        QC,
        Insp,
        Maint,
        DeviceMaint,
        Security,
        Undefined
    }

    [Serializable]
    public class UserData
    {
        public string Id = string.Empty;
        public string Password = string.Empty;
        public string Pin = string.Empty;
        public UserRole Role;

        public void Setup(string id, string password, string pin, UserRole role)
        {
            Id = id;
            Password = password;
            Pin = pin;
            Role = role;
        }
    }

    public class StorageData
    {
        public string UserName;
        public string Data;
        public string Date;

        public StorageData(string userName, string data, string date)
        {
            UserName = userName;
            Data = data;
            Date = date;
        }
    }
}
