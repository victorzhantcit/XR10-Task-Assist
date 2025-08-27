using Inspection.Dtos;
using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Inspection.Utils
{
    public class InspectionListItem : VirtualListItem<InspectionDto>
    {
        [SerializeField] private Image _backPlateImage;
        [SerializeField] private TMP_Text _idLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private TMP_Text _placeLabel;
        [SerializeField] private TMP_Text _reserveDateLabel;
        [SerializeField] private PressableButton _editButton;
        private Action _editAction = null;

        private void OnDisable()
        {
            _backPlateImage.color = Color.white;
            _editAction = null;
        }

        public override void SetContent(InspectionDto recordData, int _ = -1, bool __ = false)
        {
            _idLabel.text = recordData.recordSn;
            _nameLabel.text = recordData.description;
            _statusLabel.text = recordData.translatedStatus;
            _placeLabel.text = recordData.buildingName;
            _reserveDateLabel.text = recordData.scheduledDate;
        }

        public void SetColor(Color color) => _backPlateImage.color = color;
        public void SetEditAction(Action editAction) => _editAction = editAction;
        public void OnEnterEditorClick() => _editAction?.Invoke();
    }
}
