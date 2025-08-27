using Inspection.Dtos;
using Inspection.Utils;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Core
{
    public class InspectionInfoView : MonoBehaviour
    {
        public delegate void DeviceClicked(int index);
        public event DeviceClicked OnDeviceItemClicked;

        [SerializeField] private InspectionRootUI _inspectionRootUI;

        [Header("UI")]
        [SerializeField] private RectTransform _inspectionResponsiveLayout;
        [SerializeField] private TMP_Text _IdLabel, _nameLabel, _placeLabel, _typeLabel, _statusLabel;
        [SerializeField] private TMP_Text _scheduledDateLabel, _startTimeLabel, _submittedTimeLabel, _completedTimeLabel, _rejectTimeLabel, _commentLabel;
        [SerializeField] private RectTransform _scheduledDateDisplay, _startTimeDisplay, _submittedTimeDisplay, _completedTimeDisplay, _rejectTimeDisplay, _commentDisplay;
        [SerializeField] private Image _statusBackImage;
        [SerializeField] private PressableButton _resubmitButton;
        [SerializeField] private VirtualizedScrollRectList _deviceList;

        private InspectionDto _inspection;
        private OrderDeviceDto _orderDevice;
        private float savedScrollPosition = 0f;

        void Start()
        {
            _deviceList.OnVisible += OnDeviceItemVisible;
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
            if (visible) StartCoroutine(RebuildLayout());
            else savedScrollPosition = _deviceList.Scroll;
        }

        private void OnDeviceItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _inspection.orderDevices.Count) return;

            DeviceListItem item = gameObject.GetComponent<DeviceListItem>();
            OrderDeviceDto targetData = _inspection.orderDevices[index];

            item.SetContent(targetData);
            item.SetColor(_inspectionRootUI.GetColorByStatus(targetData.status));
            item.SetEditAction(() => OnDeviceItemClicked?.Invoke(index));
        }

        public void SetInfo(InspectionDto inspection)
        {
            _inspection = inspection;
            _IdLabel.text = $"{inspection.recordSn}";
            _nameLabel.text = inspection.description;
            _placeLabel.text = inspection.buildingName;
            _typeLabel.text = inspection.translatedOrderType;
            UpdateInfoViewStatusAndDate();
            _deviceList.SetItemCount(inspection.orderDevices.Count);
            savedScrollPosition = 0f;
        }

        public void UpdateInfoViewStatusAndDate()
        {
            UpdateStatusAndBackground();
            UpdateLabels();
            UpdateDisplayElements();
        }

        private void UpdateStatusAndBackground()
        {
            _statusLabel.text = _inspection.translatedStatus;
            _statusBackImage.color = _inspectionRootUI.GetColorByStatus(_inspection.status);
        }

        private void UpdateLabels()
        {
            _scheduledDateLabel.text = _inspection.scheduledDate;
            _startTimeLabel.text = _inspection.startTime;
            _submittedTimeLabel.text = _inspection.submitTime;
            _completedTimeLabel.text = _inspection.completeTime;
            _rejectTimeLabel.text = _inspection.rejectTime;
            _commentLabel.text = _inspection.comment;
        }

        private void UpdateDisplayElements()
        {
            _resubmitButton.gameObject.SetActive(_inspection.IsRejected);
            SetActiveState(_scheduledDateDisplay, _inspection.scheduledDate);
            SetActiveState(_startTimeDisplay, _inspection.startTime);
            SetActiveState(_submittedTimeDisplay, _inspection.submitTime);
            SetActiveState(_completedTimeDisplay, _inspection.completeTime);
            SetActiveState(_rejectTimeDisplay, _inspection.rejectTime);
            SetActiveState(_commentDisplay, _inspection.comment);
        }

        private void SetActiveState(RectTransform displayObject, string value)
            => displayObject.gameObject.SetActive(!string.IsNullOrEmpty(value));

        public void OnReuploadRejectedButtonClicked() => _inspectionRootUI.ReuploadRejectedButtonClicked();

        private IEnumerator RebuildLayout()
        {
            yield return null; // µ¥«Ý¤@´V
            LayoutRebuilder.ForceRebuildLayoutImmediate(_inspectionResponsiveLayout);
            _deviceList.ResetLayout();
            _deviceList.Scroll = savedScrollPosition;
        }
    }
}
