using MixedReality.Toolkit.UX;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRTK.Extensions
{
    public class TransparentPromptDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text _overlayHint;
        [SerializeField] private Image _overlayQrHint;
        [SerializeField] private PressableButton _cancelScanButton;
        private Action _cancelAction = null;
        private bool _isHandDetectedBefore = false;
        private void Start() => Setup(true, "�ݦV��x�H�}�ҥ\����");
        public void OnHandMenuFirstPalmUpDetected()
        {
            if (_isHandDetectedBefore) return;
            Setup(false);
            _isHandDetectedBefore = true;
        }

        public void ShowLoginSuccessHint()
        {
            _isHandDetectedBefore = false;
            Setup(true, "�n�J���\�I�ݦV��x�H�~��ާ@");
        }

        public void ShowHandHint()
        {
            _isHandDetectedBefore = false;
            Setup(true, "�ݦV��x�H�~��ާ@");
        }

        public void Setup(bool activate, string message = "", bool isQrHint = false, Action cancelAction = null)
        {
            this.gameObject.SetActive(activate);
            _overlayHint.text = message;
            _overlayQrHint.enabled = isQrHint;
            _cancelScanButton.gameObject.SetActive(cancelAction != null);
            _cancelAction = _cancelAction = cancelAction ?? (() => Debug.Log("Cancel button clicked, but no action defined."));
        }

        public void Activate(bool activate)
        {
            Setup(activate, "...");
        }

        public void SetText(string text)
        {
            _overlayHint.text = text;
        }

        public void SetColorDefault()
        {
            _overlayHint.color = Color.white;
        }

        public void SetColorWarning()
        {
            _overlayHint.color = Color.yellow;
        }

        public void SetColorCompleted()
        {
            _overlayHint.color = Color.green;
        }

        public void DelayDeactivate()
        {
            if (!gameObject.activeSelf) return;

            StartCoroutine(DeactivateInSeconds(3f));
        }

        private IEnumerator DeactivateInSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Setup(false);
        }

        public void OnCancelButtonClicked() => _cancelAction?.Invoke();
    }
}
