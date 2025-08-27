using Inspection.Dtos;
using MRTK.Extensions;
using TMPro;
using UnityEngine;

namespace Inspection.Utils
{
    public class DeviceDataListItem : VirtualListItem<DeviceNumericalDto>
    {
        [SerializeField] private TMP_Text _tagLabel;
        [SerializeField] private TMP_Text _valueLabel;

        public override void SetContent(DeviceNumericalDto data, int _ = -1, bool __ = false)
        {
            // 如果沒有單位就不顯示 "(unit)" 字眼
            string unit = (!string.IsNullOrEmpty(data.tagUnit)) ? $"({data.tagUnit})" : string.Empty;

            _tagLabel.text = data.tagDescription + unit;

            if (!string.IsNullOrEmpty(data.value))
            {
                _valueLabel.text = data.value;
                _valueLabel.color = Color.white;
            }
            else
            {
                _valueLabel.text = "--";
                _valueLabel.color = Color.gray;
            }
        }
    }
}
