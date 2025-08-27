using UnityEngine;

namespace MRTK.Extensions
{
    public abstract class VirtualListItem<T> : MonoBehaviour
    {
        /// <summary>
        /// ���~�Ӫ������@���e�A�]�m�M�涵�ت���ܡB���s���ʵ���
        /// </summary>
        /// <param name="data"></param>
        public abstract void SetContent(T data, int index, bool interactable);

        public virtual void ShowItem(bool enabled)
        {
            gameObject.SetActive(enabled);
        }
    }
}
