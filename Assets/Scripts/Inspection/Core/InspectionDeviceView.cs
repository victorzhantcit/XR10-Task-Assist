using FMSolution.FMETP;
using Inspection.Dtos;
using Inspection.Utils;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using MRTK.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaskAssist.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Core
{
    public class InspectionDeviceView : MonoBehaviour
    {
        [SerializeField] private ServiceManager _service;
        [SerializeField] private InspectionRootUI _inspectionRootUI;
        [SerializeField] private PhotoCaptureFMETP _photoCapture;

        [Header("UI")]
        [SerializeField] private RectTransform _responsiveLayout;
        [SerializeField] private TMP_Text _descriptionLabel, _statusLabel, _inspectorLabel, _estManMinuteLabel, _uploadDateLabel, _datumDateLabel, _noDataHintLabel;
        [SerializeField] private CustomMRTKTMPInputField _respondInputField;
        [SerializeField] private PressableButton _respondButton;
        [SerializeField] private PressableButton _modifySubmittedButton;
        [SerializeField] private Image _statusBackImage;
        [SerializeField] private VirtualizedScrollRectList _dataVirtualList, _checkVirtualList, _photoVirtualList;
        [SerializeField] private RectTransform _operateButtonLayout;

        private OrderDeviceDto _orderDeviceDto;
        private List<string> _devicePhotoSns = new List<string>();
        public List<string> DevicePhotoSns => _devicePhotoSns;
        private bool _editableCheckItemData = true;
        public bool EditableCheckItemData => _editableCheckItemData;

        private float _saveDataScroll = 0f, _saveCheckScroll = 0f, _savePhotoScroll = 0f;


        // Start is called before the first frame update
        void Start()
        {
            _dataVirtualList.OnVisible += OnDeviceDataItemVisible;
            _checkVirtualList.OnVisible += OnCheckItemVisible;
            _photoVirtualList.OnVisible += OnPhotoItemVisible;
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
            if (visible) StartCoroutine(RebuildLayout());
            else SetScrollValues();
        }

        private IEnumerator RebuildLayout()
        {
            yield return null; // 等待一幀
            LayoutRebuilder.ForceRebuildLayoutImmediate(_responsiveLayout);
            _dataVirtualList.ResetLayout();
            _checkVirtualList.ResetLayout();
            _photoVirtualList.ResetLayout();
            RestoreScrollValues();
        }

        private void SetScrollValues(bool reset = false)
        {
            _saveDataScroll = (!reset) ? _dataVirtualList.Scroll : 0f;
            _saveCheckScroll = (!reset) ? _checkVirtualList.Scroll : 0f;
            _savePhotoScroll = (!reset) ? _photoVirtualList.Scroll : 0f;
        }

        private void RestoreScrollValues()
        {
            _dataVirtualList.Scroll = _saveDataScroll;
            _checkVirtualList.Scroll = _saveCheckScroll;
            _photoVirtualList.Scroll = _savePhotoScroll;
        }

        public void InitVirtualLists()
        {
            SetCurrentDevicePhotoSns();
            _dataVirtualList.SetItemCount(_orderDeviceDto.numericalData.Count);
            _checkVirtualList.SetItemCount(_orderDeviceDto.items.Count);
            _photoVirtualList.SetItemCount(_devicePhotoSns.Count);
        }

        private void SetCurrentDevicePhotoSns()
        {
            _devicePhotoSns.Clear();
            if (!string.IsNullOrEmpty(_orderDeviceDto.afPhotoSns)) _devicePhotoSns = _orderDeviceDto.afPhotoSns.Split(',').ToList();
        }

        private void OnDeviceDataItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _orderDeviceDto.numericalData.Count) return;

            DeviceDataListItem item = gameObject.GetComponent<DeviceDataListItem>();
            DeviceNumericalDto targetData = _orderDeviceDto.numericalData[index];

            item.SetContent(targetData);
        }

        private void OnCheckItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _orderDeviceDto.items.Count) return;

            CheckListItem item = gameObject.GetComponent<CheckListItem>();
            CheckItemDto targetData = _orderDeviceDto.items[index];

            item.SetColorBoard(_inspectionRootUI.PositiveColor, _inspectionRootUI.NegativeColor, _inspectionRootUI.DefaultColor);
            item.SetContent(targetData, index + 1, _orderDeviceDto.IsProcessing);
        }

        private void OnPhotoItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _devicePhotoSns.Count) return;

            PhotoListItem item = gameObject.GetComponent<PhotoListItem>();
            string targetData = _devicePhotoSns[index];

            item.SetTextureTranslate(_service.GetInspPhotoTexture);
            item.SetContent(targetData, index, _orderDeviceDto.IsProcessing);
            item.SetRemoveAction(() =>
            {
                _service.RemoveInspPhoto(_devicePhotoSns[index]);
                _devicePhotoSns.RemoveAt(index);
                _orderDeviceDto.afPhotoSns = string.Join(',', _devicePhotoSns);
                _photoVirtualList.SetItemCount(_devicePhotoSns.Count);
                _photoVirtualList.ResetLayout();
            });
        }

        public void SetDetails(OrderDeviceDto orderDeviceDto, bool inspcetionRejected = false)
        {
            _modifySubmittedButton.gameObject.SetActive(inspcetionRejected && !orderDeviceDto.IsProcessing);
            _orderDeviceDto = orderDeviceDto;
            _editableCheckItemData = orderDeviceDto.IsProcessing;
            _descriptionLabel.text = orderDeviceDto.deviceDescription;
            _statusLabel.text = orderDeviceDto.translatedStatus;
            _statusBackImage.color = _inspectionRootUI.GetColorByStatus(orderDeviceDto.status);
            _inspectorLabel.text = orderDeviceDto.executor;
            _estManMinuteLabel.text = (orderDeviceDto.estManMinute > 0) ? $"{orderDeviceDto.estManMinute} (分鐘)" : "--";
            _datumDateLabel.text = GetFormatDate(orderDeviceDto.dataTime);
            _uploadDateLabel.text = GetFormatDate(orderDeviceDto.submitTime);
            _noDataHintLabel.gameObject.SetActive(orderDeviceDto.numericalData.Count <= 0); // 無資料的顯示
            _respondInputField.text = (orderDeviceDto.respond != "Start") ? orderDeviceDto.respond : "";
            _respondInputField.interactable = _editableCheckItemData;
            _respondButton.enabled = _editableCheckItemData;
            _operateButtonLayout.gameObject.SetActive(_editableCheckItemData);

            InitVirtualLists();
            SetScrollValues(true);
        }

        public void OnRespondInputFieldClicked() => _inspectionRootUI.DeviceRespondInputClicked();

        public void OnDeviceRespondTextChanged(string newRespond)
        {
            bool invalidValue = newRespond == null || _orderDeviceDto.respond == newRespond || newRespond == "Start";

            if (invalidValue)
                return;

            _orderDeviceDto.respond = newRespond;
            _respondInputField.text = newRespond;
        }

        // 開始截圖
        public void TakeScreenShot()
        {
            _inspectionRootUI.ShowSlateContent(false);
            _photoCapture.CapturePhoto(texture => HandleScreenshotTaken(texture != null, texture));
        }

        private void HandleScreenshotTaken(bool success, Texture2D capturedTexture)
        {
            Debug.Log("ReceiveScreenShotTaken");
            if (!success)
            {
                _inspectionRootUI.ShowDialog("螢幕截圖失敗", "請重新截圖或聯絡開發人員");
                Debug.LogWarning("Capture failed!");
                return;
            }

            _inspectionRootUI.UpdatePromptDialog(false);
            _inspectionRootUI.ShowSlateContent(true);

            string capturedPhotoBase64 = Convert.ToBase64String(capturedTexture.EncodeToJPG());
            string tempSns = _service.AddInspPhoto(capturedPhotoBase64);

            _devicePhotoSns.Add(tempSns);
            _orderDeviceDto.afPhotoSns = string.Join(',', _devicePhotoSns);

            _photoVirtualList.SetItemCount(_devicePhotoSns.Count);
            _photoVirtualList.ResetLayout();
        }

        //public void OnDeviceRespondClicked() => 

        public void OnModifySubmittedButtonClicked()
        {
            SetCurrentDevicePhotoSns();
            _inspectionRootUI.PostStartInspection();
        }

        private string GetFormatDate(string dateString) => string.IsNullOrEmpty(dateString) ? "----/--/-- --:--" : dateString;
    }
}
