using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EnumStateVisualizer<T> : MonoBehaviour where T : Enum
{
    [Serializable]
    public class EnumVisualMapping
    {
        public T EnumValue; // 枚舉值
        public GameObject[] VisualObjects; // 對應的物件
    }

    [SerializeField]
    private List<EnumVisualMapping> _visualMappings = new List<EnumVisualMapping>();

    private T _currentEnumValue;

    /// <summary>
    /// 初始化到預設值
    /// </summary>
    [SerializeField]
    private T _defaultEnumValue;

    /// <summary>
    /// 獲取目前的狀態
    /// </summary>
    public T CurrentEnumValue => _currentEnumValue;

    protected virtual void Start()
    {
        // 初始化到預設值
        SetEnumValue(_defaultEnumValue);
    }

    /// <summary>
    /// 切換到指定的枚舉值
    /// </summary>
    public virtual void SetEnumValue(T enumValue)
    {
        // 禁用所有物件
        foreach (var mapping in _visualMappings)
        {
            ActivateVisualObjects(mapping.VisualObjects, false);
        }

        // 查找與當前枚舉值相關的物件並啟用
        var selectedMapping = _visualMappings.FirstOrDefault(mapping => Equals(mapping.EnumValue, enumValue));
        if (selectedMapping != null)
        {
            ActivateVisualObjects(selectedMapping.VisualObjects, true);
        }

        // 更新當前枚舉值
        _currentEnumValue = enumValue;
    }

    /// <summary>
    /// 啟用或禁用對應的物件
    /// </summary>
    private void ActivateVisualObjects(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}
