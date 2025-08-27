using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorkOrder.Dtos;

namespace WorkOrder.Utils
{
    public class WorkOrderListItem : VirtualListItem<WorkOrderDto>
    {
        [SerializeField] private Image _backPlateImage;
        [SerializeField] private TMP_Text _idLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private TMP_Text _placeLabel;
        [SerializeField] private TMP_Text _reserveDateLabel;
        [SerializeField] private PressableButton _editButton;
        [SerializeField] private UnityEngine.UI.Slider _priorityBar;

        private Action _editAction = null;
        private Color[] _priorityColor = new Color[3] 
        { 
            new Color(0.1921569f, 0.764706f, 0.7960785f),
            Color.yellow,
            Color.red
        };

        private void OnDisable()
        {
            _backPlateImage.color = Color.white;
            _editAction = null;
        }

        public override void SetContent(WorkOrderDto recordData, int _ = -1, bool __ = false)
        {
            _idLabel.text = recordData.recordSn;
            _nameLabel.text = recordData.description;
            _statusLabel.text = recordData.translatedStatus;
            _placeLabel.text = recordData.buildingName;
            _reserveDateLabel.text = recordData.scheduledDate;
            _priorityBar.value = (recordData.priority + 1) * 0.333f;
            _priorityBar.fillRect.GetComponent<Image>().color = _priorityColor[recordData.priority];
        }

        public void SetColor(Color color) => _backPlateImage.color = color;
        public void SetEditAction(Action editAction) => _editAction = editAction;
        public void OnEnterEditorClick() => _editAction?.Invoke();
    }
}
