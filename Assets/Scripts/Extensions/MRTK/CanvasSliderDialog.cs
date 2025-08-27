using MixedReality.Toolkit.UX;
using System;
using TMPro;
using UnityEngine;

namespace MRTK.Extensions
{
    public class CanvasSliderDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text _headerLabel;
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _valueVisualizeLabel;

        private Action<SliderEventData> _onSliderValueChanged = null;

        public void Setup(string userName, float defaultValue, Action<SliderEventData> onSliderValueChanged)
        {
            _onSliderValueChanged = null;
            _headerLabel.text = userName;
            _slider.Value = defaultValue;
            _onSliderValueChanged = onSliderValueChanged;
        }

        public void UpdateValueLabel(float value, bool percentage = false)
        {
            _valueVisualizeLabel.text = (percentage) ? $"{value * 100:F1}%" : $"{value:F2}";
        }

        public void OnSliderValueChanged(SliderEventData e)
        {
            _onSliderValueChanged?.Invoke(e);
        }

        public void Show()
        {
            this.gameObject.SetActive(true);
        }

        public void UpdateHeaderLabel(string newText)
        {
            _headerLabel.text = newText;
        }
    }
}
