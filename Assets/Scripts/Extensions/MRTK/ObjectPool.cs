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

        // 從池中取出物件，如果池中沒有，則創建一個新的
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

        // 回收物件，將物件隱藏並放回池中
        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        // 清空物件池
        public void Clear()
        {
            pool.Clear();
        }
    }
}
