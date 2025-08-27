using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using User.Dtos;

namespace User.Core
{
    /// <summary>
    /// 負責使用者資訊的讀寫，保管現在登入使用者資訊
    /// </summary>
    public static class SecureDataManager
    {
        private static string LoggedInID = string.Empty;
        private static string FODLER = Path.Combine(Application.persistentDataPath, "User");

        public static UserRole UserRole;
        public static string UserName;

        public static bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;


        private static string GenerateKey()
        {
            // 生成動態密鑰 (基於設備唯一 ID)
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId));
                return Convert.ToBase64String(hash).Substring(0, 16); // 取前 16 字元作為密鑰
            }
        }

        // 使用當前時間生成初始化向量 (IV)
        private static string GenerateIV() => DateTime.Now.ToString("yyyyMMddHHmmssfff").Substring(0, 16);

        private static string Encrypt(string plainText, string key, string iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        private static string Decrypt(string cipherText, string key, string iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var memoryStream = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var reader = new StreamReader(cryptoStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static void SaveDataToFile(string username, UserData userData)
        {
            // 序列化資料
            string jsonData = JsonConvert.SerializeObject(userData);

            // 生成密鑰與 IV
            string key = GenerateKey();
            string iv = GenerateIV();

            // 加密資料
            string encryptedData = Encrypt(jsonData, key, iv);

            StorageData storageData = new StorageData(username, encryptedData, iv);

            // 儲存加密資料到檔案
            string filePath = Path.Combine(FODLER, $"{userData.Id}.json");
            jsonData = JsonConvert.SerializeObject(storageData);

            if (!Directory.Exists(FODLER))
                Directory.CreateDirectory(FODLER);

            File.WriteAllText(filePath, jsonData);

            LoggedInID = userData.Id;
            Debug.Log("資料已安全儲存到檔案！");
        }

        private static StorageData LoadStorageFileJson(string id)
        {
            // 讀取加密資料與 IV
            string filePath = Path.Combine(FODLER, $"{id}.json");

            if (!File.Exists(filePath))
            {
                Debug.LogWarning("找不到任何已儲存的檔案！");
                return null;
            }

            string userFile = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<StorageData>(userFile);
        }

        public static UserData LoadDataFromFile(string id)
        {
            StorageData storageData = LoadStorageFileJson(id);

            // 生成密鑰
            string key = GenerateKey();

            // 解密資料
            string encryptedData = Decrypt(storageData.Data, key, storageData.Date);

            // 反序列化回物件
            UserData userData = JsonConvert.DeserializeObject<UserData>(encryptedData);

            LoggedInID = userData.Id;
            Debug.Log($"使用者資料讀取成功");
            return userData;
        }

        public static UserData LoadLoggedInData() => LoadDataFromFile(LoggedInID);

        public static Dictionary<string, string> GetUserNames()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            if (!Directory.Exists(FODLER))
            {
                Debug.LogWarning($"Folder does not exist: {FODLER}, create folder");
                Directory.CreateDirectory(FODLER);
                return results;
            }

            string[] files = Directory.GetFiles(FODLER, "*.json");

            foreach (string file in files)
            {
                try
                {
                    string jsonContent = File.ReadAllText(file);
                    var storageData = JsonConvert.DeserializeObject<StorageData>(jsonContent);
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    results[fileName] = storageData.UserName;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error reading file {file}: {ex.Message}");
                }
            }

            return results;
        }

        public static List<int> GetUserPinCodes(string id)
        {
            List<int> result = new List<int>();

            try
            {
                UserData userData = LoadDataFromFile(id);
                // 處理字串，過濾掉非數字的字符
                result = userData.Pin
                    .Where(char.IsDigit) // 僅保留數字字符
                    .Select(c => int.Parse(c.ToString())) // 將字符轉為整數
                    .ToList();
                //Debug.Log("Parsed numbers: " + string.Join(", ", result));
            }
            catch (Exception ex)
            {
                // 捕捉其他潛在的錯誤
                Debug.LogWarning("An error occurred: " + ex.Message);
            }
            return result;
        }

        public static string GetLoggedInUserName()
        {
            StorageData storageData = LoadStorageFileJson(LoggedInID);
            if (storageData == null) return null;
            return storageData.UserName;
        }
    }
}
