using MixedReality.Toolkit.UX;
using System;
using TMPro;
using UnityEngine;

namespace MRTK.Extensions
{
    public class CanvasInputFieldDialog : MonoBehaviour
    {
        [SerializeField] private Dialog _dialog;
        [SerializeField] private TMP_InputField _submitInputField;
        [SerializeField] private TMP_Text _errorMessage;

        private string _definiteText = string.Empty;

        public void Setup(string message, string defaultValue, Action<string> onSubmitted)
        {
            this.gameObject.SetActive(true);
            _dialog.Reset();
            _submitInputField.text = defaultValue;
            _dialog.SetHeader(message)
                .SetPositive("½T»{", (args) => onSubmitted(_submitInputField.text))
                .SetNegative("X", (args) => { })
                .ShowAsync();
            InitDefiniteText();
        }

        public void InitDefiniteText()
        {
            _definiteText = _submitInputField.text;
            _errorMessage.text = string.Empty;
        }

        public void OnSpeechRecognizing(string text) => _submitInputField.text = _definiteText + text;

        public void OnSpeechRecognized(string text)
        {
            _definiteText += text;
            _submitInputField.text = _definiteText;
        }

        public void OnRecognitionFinished(string text) => _definiteText = string.Empty;

        public void OnRecognitionFaulted(string text) => _errorMessage.text = text;
    }
}
