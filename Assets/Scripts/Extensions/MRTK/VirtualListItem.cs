using UnityEngine;

namespace MRTK.Extensions
{
    public abstract class VirtualListItem<T> : MonoBehaviour
    {
        /// <summary>
        /// 由繼承的物件實作內容，設置清單項目的顯示、按鈕互動等等
        /// </summary>
        /// <param name="data"></param>
        public abstract void SetContent(T data, int index, bool interactable);

        public virtual void ShowItem(bool enabled)
        {
            gameObject.SetActive(enabled);
        }
    }
}
