using WorkOrder.Dtos;
using WorkOrder.Utils;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;
using TaskAssist.Core;
using System.Collections.Generic;
using Unity.Extensions;

namespace WorkOrder.Core
{
    public class WorkOrderInfoView : MonoBehaviour
    {
        public delegate void DeviceClicked(int index);
        public event DeviceClicked OnDeviceItemClicked;

        [SerializeField] private ServiceManager _service;
        [SerializeField] private WorkOrderRootUI _workOrderRootUI;

        [Header("UI")]
        [SerializeField] private RectTransform _infomationLayout;
        [SerializeField] private TMP_Text _IdLabel, _nameLabel, _placeLabel, _executorLabel, _statusLabel;
        [SerializeField] private Image _statusBackImage;
        [SerializeField] private TMP_Text _scheduledDateLabel, _startTimeLabel, _submittedTimeLabel, _completedTimeLabel;
        [SerializeField] private RectTransform _scheduledDateDisplay, _startTimeDisplay, _submittedTimeDisplay, _completedTimeDisplay;
        [SerializeField] private PressableButton _uploadSummaryButton;
        [SerializeField] private VirtualizedScrollRectList _deviceList;

        private WorkOrderDto _currentWorkOrder;
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
            if (index < 0 || index >= _currentWorkOrder.orderDevices.Count) return;

            DeviceListItem item = gameObject.GetComponent<DeviceListItem>();
            OrderDeviceDto targetData = _currentWorkOrder.orderDevices[index];

            item.SetContent(targetData);
            item.SetColor(_workOrderRootUI.GetColorByStatus(targetData.status));
            item.SetEditAction(() => OnDeviceItemClicked?.Invoke(index));
        }

        public void SetInfo(WorkOrderDto workOrder)
        {
            _currentWorkOrder = workOrder;
            _IdLabel.text = $"{workOrder.recordSn}";
            _nameLabel.text = workOrder.description;
            _placeLabel.text = workOrder.buildingName;
            _executorLabel.text = (string.IsNullOrEmpty(workOrder.staffName)) ? workOrder.executorName : workOrder.staffName;
            UpdateInfoViewStatusAndDate();
            _deviceList.SetItemCount(workOrder.orderDevices.Count);
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
            _statusLabel.text = _currentWorkOrder.translatedStatus;
            _statusBackImage.color = _workOrderRootUI.GetColorByStatus(_currentWorkOrder.status);
        }

        private void UpdateLabels()
        {
            _scheduledDateLabel.text = _currentWorkOrder.scheduledDate;
            _startTimeLabel.text = _currentWorkOrder.startTime;
            _submittedTimeLabel.text = _currentWorkOrder.submitTime;
            _completedTimeLabel.text = _currentWorkOrder.completeTime;
        }

        private bool IsWorkOrderEditable
         => _workOrderRootUI.GetColorByStatus("c-processing") == _statusBackImage.color;

        private void UpdateDisplayElements()
        {
            _uploadSummaryButton.gameObject.SetActive(IsWorkOrderEditable);
            SetActiveState(_scheduledDateDisplay, _currentWorkOrder.scheduledDate);
            SetActiveState(_startTimeDisplay, _currentWorkOrder.startTime);
            SetActiveState(_submittedTimeDisplay, _currentWorkOrder.submitTime);
            SetActiveState(_completedTimeDisplay, _currentWorkOrder.completeTime);
        }

        private void SetActiveState(RectTransform displayObject, string value)
            => displayObject.gameObject.SetActive(!string.IsNullOrEmpty(value));

        public void OnSubmitWorkOrderClicked() => SubmitWorkOrder();

        private IEnumerator RebuildLayout()
        {
            yield return null; // 等待一幀
            LayoutRebuilder.ForceRebuildLayoutImmediate(_infomationLayout);
            _deviceList.ResetLayout();
            _deviceList.Scroll = savedScrollPosition;
        }

        private void SubmitWorkOrder()
        {
            if (!IsDeviceAllSubmitted())
            {
                _workOrderRootUI.ShowDialog("尚有設備未完成！");
                return;
            }

            _workOrderRootUI.ShowSubmitDialog("請輸入工單完工總結（不可為空）", string.Empty, (response) =>
            {
                WorkOrderSubmitDto workOrderSubmitDto = new WorkOrderSubmitDto();
                bool isValid = workOrderSubmitDto.SetupAndValidate(_currentWorkOrder, response);

                if (!isValid) return;

                List<KeyValue> requestData = KeyValueConverter.ToKeyValues(workOrderSubmitDto);
                _service.PostWorkOrderSubmit(requestData, HandleWorkOrderSubmitResponse);
            });
        }

        private bool IsDeviceAllSubmitted()
            => _currentWorkOrder.orderDevices.Find(device => !device.IsSubmitted) == null;

        private void HandleWorkOrderSubmitResponse(bool success)
        {
            // 離線與在線的處理方式無差異
            _currentWorkOrder.SetStatusToSubmitted(DateTime.Now.ToString(_workOrderRootUI.DATE_FORMAT_LOCAL));
            _uploadSummaryButton.gameObject.SetActive(false);
            SetInfo(_currentWorkOrder);
        }

        public void UpdateWorkOrderStateByDevice()
        {
            // 如果工單只有一個設備，不須呼叫 SubmitWorkOrder API，直接顯示工單完工上傳
            // 否則，保持現在狀態並顯示工單完工上傳按鈕
            if (_currentWorkOrder.orderDevices.Count == 1)
                HandleWorkOrderSubmitResponse(true);
            else
                _uploadSummaryButton.gameObject.SetActive(true);
        }
    }
}
