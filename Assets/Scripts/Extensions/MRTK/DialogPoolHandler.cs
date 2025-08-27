using MixedReality.Toolkit.UX;
using System;
using System.Collections;
using UnityEngine;

namespace MRTK.Extensions
{
    public class DialogPoolHandler : MonoBehaviour
    {
        [SerializeField] private Transform _dialogPoolParent;
        [SerializeField] private Dialog _dialog;
        private ObjectPool<Dialog> _dialogPool;

        // Start is called before the first frame update
        private void Start() => _dialogPool = new ObjectPool<Dialog>(_dialog, _dialogPoolParent);

        public void ShowDialog(string title, string message = null, Action confirmAction = null, Action cancelAction = null)
        {
            Dialog dialog = _dialogPool.Get();
            dialog.Reset();
            dialog.SetHeader(title);

            if (!string.IsNullOrEmpty(message))
                dialog.SetBody(message);

            SetDialogButtons(dialog, confirmAction, cancelAction);

            dialog.ShowAsync();
        }

        private void SetDialogButtons(Dialog dialog, Action confirmAction, Action cancelAction)
        {
            // �]�m Positive ���s�]���T�w���^
            dialog.SetPositive("�T�w", (args) =>
            {
                confirmAction?.Invoke(); // �p�G confirmAction �� null�A�h���������ާ@
                StartCoroutine(ReleaseDialogAfterDisabled(dialog));
            });

            // �]�m Negative ���s�]���������^�A�u���� cancelAction ���� null �ɤ~���
            if (cancelAction != null)
            {
                dialog.SetNegative("����", (args) =>
                {
                    cancelAction.Invoke();
                    StartCoroutine(ReleaseDialogAfterDisabled(dialog));
                });
            }
        }

        private IEnumerator ReleaseDialogAfterDisabled(Dialog dialog)
        {
            yield return new WaitUntil(() => !dialog.isActiveAndEnabled);
            _dialogPool.Release(dialog);
        }
    }
}
