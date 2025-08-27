using System.Collections.Generic;

namespace TaskAssist.Utils
{
    public class IBMSPhotosStorage
    {
        public Dictionary<string, string> PhotoHashMap { get; set; } = new Dictionary<string, string>();
        public int LastPhotoSns { get; set; }

        public bool ContainsPhotoSns(string photoSns)
        {
            return PhotoHashMap.ContainsKey(photoSns);
        }

        public void AddPhoto(string photoSns, string photoBase64)
        {
            PhotoHashMap.Add(photoSns, photoBase64);
        }

        public string SavePhotoAndReturnSns(string photoBase64)
        {
            string newSns = GetNewPhotoSns();

            PhotoHashMap.Add(newSns, photoBase64);

            return newSns;
        }

        public void UpdatePhotoSns(string oldSns, string newSns)
        {
            // 舊的 Sns 不存在
            if (!PhotoHashMap.ContainsKey(oldSns) || oldSns == newSns)
                return;

            PhotoHashMap[newSns] = PhotoHashMap[oldSns];
            PhotoHashMap.Remove(oldSns);
        }

        public void RemovePhotoBySns(string photoSns)
        {
            if (!PhotoHashMap.ContainsKey(photoSns))
                return;

            PhotoHashMap.Remove(photoSns);
        }

        public string GetPhotoData(string photoSn)
        {
            if (!PhotoHashMap.ContainsKey(photoSn))
                return null;

            return PhotoHashMap[photoSn];
        }

        private string GetNewPhotoSns()
        {
            return (--LastPhotoSns).ToString();
        }
    }
}
