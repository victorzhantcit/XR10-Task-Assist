using Inspection.Dtos;
using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Utils
{
    public class CheckListItem : VirtualListItem<CheckItemDto>
    {
        [SerializeField] private TMP_Text _itemNameLabel;
        [SerializeField] private TMP_Text _methodRunningLabel;
        [SerializeField] private TMP_Text _positiveButtonLabel;
        [SerializeField] private Image _positiveBackPlate;
        [SerializeField] private PressableButton _positiveButton;
        [SerializeField] private TMP_Text _negativeButtonLabel;
        [SerializeField] private Image _negativeBackPlate;
        [SerializeField] private PressableButton _negativeButton;
        [SerializeField] private CustomMRTKTMPInputField _noteInputField;
        [SerializeField] private PressableButton _noteInputButton;

        private Action _onPositiveClicked = null;
        private Action _onNegativeClicked = null;
        private Action<string> _onNoteTextChanged = null;
        private Color _positiveColor;
        private Color _negativeColor;
        private Color _defaultColor;

        private void OnDisable()
        {
            _onPositiveClicked = null;
            _onNegativeClicked = null;
            _onNoteTextChanged = null;
        }

        public override void SetContent(CheckItemDto item, int displayIndex, bool interactable)
        {
            _itemNameLabel.text = $"{displayIndex:D2}. {item.itemName}";
            _methodRunningLabel.text = $"�ˬd�覡�G{item.method}  �Ұ����A�G{item.translatedRunning}";
            _positiveButtonLabel.text = item.TranslatedPositiveStatus;
            _positiveBackPlate.color = (item.IsPositiveSelected) ? _positiveColor : _defaultColor;
            _positiveButton.enabled = interactable;
            _negativeButtonLabel.text = item.TranslatedNegativeStatus;
            _negativeBackPlate.color = (item.IsNegativeSelected) ? _negativeColor : _defaultColor;
            _negativeButton.enabled = interactable;
            _noteInputField.text = item.note;
            _noteInputField.interactable = interactable;
            _noteInputButton.enabled = interactable;

            // ���s�e�{ RadioButton �ĪG
            _onPositiveClicked = () =>
            {
                item.SetStatusPositive();
                _positiveBackPlate.color = _positiveColor;
                _negativeBackPlate.color = _defaultColor;
            };
            _onNegativeClicked = () =>
            {
                item.SetStatusNegative();
                _negativeBackPlate.color = _negativeColor;
                _positiveBackPlate.color = _defaultColor;
            };

            _onNoteTextChanged = (newNote) =>
            {
                bool invalidValue = newNote == null || item.note == newNote;

                if (invalidValue)
                    return;

                item.note = newNote;
            };
        }
        public void OnPositiveButtonClicked() => _onPositiveClicked?.Invoke();
        public void OnNegativeButtonClicked() => _onNegativeClicked?.Invoke();
        public void OnNoteInputChanged(string note) => _onNoteTextChanged?.Invoke(note);


        public void SetColorBoard(Color positiveColor, Color negativeColor, Color defaultColor)
        {
            _positiveColor = positiveColor;
            _negativeColor = negativeColor;
            _defaultColor = defaultColor;
        }
    }
}
