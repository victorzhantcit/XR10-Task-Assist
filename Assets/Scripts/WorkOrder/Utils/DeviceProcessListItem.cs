using MRTK.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorkOrder.Dtos;

namespace WorkOrder.Utils
{
    public class DeviceProcessListItem : VirtualListItem<RecordDto>
    {
        [SerializeField] private TMP_Text _respondTimeLabel;
        [SerializeField] private TMP_Text _staffNameLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private Image _statusBackPlate;
        [SerializeField] private TMP_Text _respondLabel;

        public override void SetContent(RecordDto record, int _ = -1, bool __ = true)
        {
            _respondTimeLabel.text = record.respondTime;
            _staffNameLabel.text = record.staffName;
            _statusLabel.text = record.translatedStatus;
            _respondLabel.text = record.respond;
        }

        public void SetColor(Color color) => _statusBackPlate.color = color;
    }
}

