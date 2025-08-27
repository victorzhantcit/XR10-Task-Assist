using Inspection.Dtos;
using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Utils
{
    public class DeviceListItem : VirtualListItem<OrderDeviceDto>
    {
        [SerializeField] private Image _backPlateImage;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private RectTransform _pictureIcon;
        [SerializeField] private TMP_Text _pictureAmount;
        [SerializeField] private TMP_Text _timeSpentLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private PressableButton _editButton;
        private Action _editAction = null;

        private void OnDisable()
        {
            _editAction = null;
        }

        public override void SetContent(OrderDeviceDto deviceData, int _ = -1, bool __ = false)
        {
            string manMiniteString = deviceData.manMinute > 0 ? $"¥Î®É {deviceData.manMinute} ¤ÀÄÁ" : "--";
            int afPhotoSnsLength = deviceData.afPhotoSns.Split(',').Length;
            bool hasPicture = (!string.IsNullOrEmpty(deviceData.afPhotoSns) && afPhotoSnsLength > 0);
            bool hasValidRespond = !string.IsNullOrEmpty(deviceData.respond) && deviceData.respond != "Start";
            _pictureIcon.gameObject.SetActive(hasPicture);
            _pictureAmount.gameObject.SetActive(hasPicture);

            _nameLabel.text = deviceData.deviceDescription;
            _statusLabel.text = deviceData.translatedStatus;
            _timeSpentLabel.text = manMiniteString;
            _pictureAmount.text = (hasPicture) ? $"({afPhotoSnsLength})" : string.Empty;
            _descriptionLabel.text = (hasValidRespond) ? deviceData.respond : "--";
        }

        public void SetColor(Color color)
        {
            _backPlateImage.color = color;
        }

        public void SetEditAction(Action editAction)
        {
            _editAction = editAction;
        }

        public void OnEnterEditorClick()
        {
            _editAction?.Invoke();
        }
    }
}
