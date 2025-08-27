using System.Collections.Generic;
using UnityEngine;

namespace MRTK.Extensions
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool = new Queue<T>();

        public ObjectPool(T prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        // �q�������X����A�p�G�����S���A�h�Ыؤ@�ӷs��
        public T Get()
        {
            if (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                obj.gameObject.SetActive(true);
                return obj;
            }
            else
            {
                return Object.Instantiate(prefab, parent);
            }
        }

        // �^������A�N�������èé�^����
        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        // �M�Ū����
        public void Clear()
        {
            pool.Clear();
        }
    }
}
