using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EnumStateVisualizer<T> : MonoBehaviour where T : Enum
{
    [Serializable]
    public class EnumVisualMapping
    {
        public T EnumValue; // �T�|��
        public GameObject[] VisualObjects; // ����������
    }

    [SerializeField]
    private List<EnumVisualMapping> _visualMappings = new List<EnumVisualMapping>();

    private T _currentEnumValue;

    /// <summary>
    /// ��l�ƨ�w�]��
    /// </summary>
    [SerializeField]
    private T _defaultEnumValue;

    /// <summary>
    /// ����ثe�����A
    /// </summary>
    public T CurrentEnumValue => _currentEnumValue;

    protected virtual void Start()
    {
        // ��l�ƨ�w�]��
        SetEnumValue(_defaultEnumValue);
    }

    /// <summary>
    /// ��������w���T�|��
    /// </summary>
    public virtual void SetEnumValue(T enumValue)
    {
        // �T�ΩҦ�����
        foreach (var mapping in _visualMappings)
        {
            ActivateVisualObjects(mapping.VisualObjects, false);
        }

        // �d��P��e�T�|�Ȭ���������ñҥ�
        var selectedMapping = _visualMappings.FirstOrDefault(mapping => Equals(mapping.EnumValue, enumValue));
        if (selectedMapping != null)
        {
            ActivateVisualObjects(selectedMapping.VisualObjects, true);
        }

        // ��s��e�T�|��
        _currentEnumValue = enumValue;
    }

    /// <summary>
    /// �ҥΩθT�ι���������
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
