using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MRTK.Extensions
{
    public class CustomMRTKTMPInputField : MRTKTMPInputField
    {
        public new void ActivateMRTKTMPInputField()
        {
            MRTKInputFieldManager.SetCurrentInputField(this);

            // 根據 Content Type 顯示不同的鍵盤類型
            OpenKeyboardBasedOnContentType();

            ActivateInputField();
        }

        // 根據內容類型選擇鍵盤類型
        private void OpenKeyboardBasedOnContentType()
        {
            // 根據輸入框的 contentType 選擇鍵盤類型
            TouchScreenKeyboardType keyboardType = contentType switch
            {
                ContentType.IntegerNumber => TouchScreenKeyboardType.NumberPad,
                ContentType.DecimalNumber => TouchScreenKeyboardType.DecimalPad,
                ContentType.EmailAddress => TouchScreenKeyboardType.EmailAddress,
                ContentType.Password or ContentType.Pin => TouchScreenKeyboardType.Default,// 可以根據需求設置為其他類型
                _ => TouchScreenKeyboardType.Default,
            };

            // 打開對應的鍵盤
            TouchScreenKeyboard.Open("", keyboardType);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            if (eventData == null || XRSubsystemHelpers.DisplaySubsystem == null)
            {
                base.OnDeselect(eventData);
                MRTKInputFieldManager.RemoveCurrentInputField(this);
            }
        }

    }
}
