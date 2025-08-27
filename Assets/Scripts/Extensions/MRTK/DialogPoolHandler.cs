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
            // 設置 Positive 按鈕（“確定”）
            dialog.SetPositive("確定", (args) =>
            {
                confirmAction?.Invoke(); // 如果 confirmAction 為 null，則不執行任何操作
                StartCoroutine(ReleaseDialogAfterDisabled(dialog));
            });

            // 設置 Negative 按鈕（“取消”），只有當 cancelAction 不為 null 時才顯示
            if (cancelAction != null)
            {
                dialog.SetNegative("取消", (args) =>
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
