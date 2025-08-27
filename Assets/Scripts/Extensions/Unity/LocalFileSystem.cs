using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Extensions
{
    public static class LocalFileSystem
    {
        private static string DataFolder;
        private static readonly object FILE_LOCK = new object();
        private static readonly Queue<string> _saveFilePathQueue = new Queue<string>();
        private static readonly Queue<string> _saveDataQueue = new Queue<string>();
        private static bool _isSaving;

        public static void SetDataFolder(string dataFolder)
        {
            DataFolder = dataFolder;
        }

        /// <summary>
        /// �q���a��Ū�����e�äϧǦC�Ƭ����w����
        /// </summary>
        public static async Task<T> GetLocalDataAsync<T>(string fileName)
        {
            try
            {
                return await ReadAndDeserializeAsync<T>(fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load data from {fileName}: {ex.Message}");
                return default(T);
            }
        }

        private static async Task<T> ReadAndDeserializeAsync<T>(string fileName)
        {
            string filePath = Path.Combine(DataFolder, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found at path: {filePath}");
                return default(T);
            }

            try
            {
                // �ϥ� lock �T�O�h������s���w��
                string jsonContent;
                lock (FILE_LOCK)
                {
                    jsonContent = File.ReadAllText(filePath); // �P�BŪ���ɮ�
                }

                // �ϧǦC�Ƭ����w���� (���B�B�z�i�H�O��)
                return await Task.Run(() => JsonConvert.DeserializeObject<T>(jsonContent));
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                Debug.LogWarning($"Error reading or deserializing file: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// �ˬd���O�_�s�b�A���s�b�h�Ы�
        /// </summary>
        /// <typeparam name="T">���w�ɮ׮榡</typeparam>
        public static void CheckOrCreateFile<T>(string fileName, T defaultData)
        {
            string fullPath = Path.Combine(DataFolder, fileName);
            if (!File.Exists(fullPath))
                QueueSaveData(defaultData, fullPath);
        }

        public static void SaveData<T>(T data, string fileName)
        {
            string filePath = Path.Combine(DataFolder, fileName);
            QueueSaveData(data, filePath);
        }

        private static void QueueSaveData(object data, string filePath)
        {
            _saveFilePathQueue.Enqueue(filePath);
            _saveDataQueue.Enqueue(JsonConvert.SerializeObject(data));
            ProcessQueue();
        }

        private static async void ProcessQueue()
        {
            if (_isSaving || !_saveDataQueue.Any()) return;

            _isSaving = true;

            while (_saveDataQueue.Any())
            {
                string data = _saveDataQueue.Dequeue();
                string filePath = _saveFilePathQueue.Dequeue();
                await SaveDataAsync(data, filePath);
            }

            _isSaving = false;
        }

        private static async Task SaveDataAsync(string data, string filePath)
        {
            // ������|���ؿ������A���ҥؿ��O�_�s�b�A���s�b�h�Ы�
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // �ϥ� StreamWriter �g�J�ɮ�
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(data);
                Debug.Log($"Saved data to {filePath}");
            }
        }
    }

}
