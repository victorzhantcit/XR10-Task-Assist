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

            // �ھ� Content Type ��ܤ��P����L����
            OpenKeyboardBasedOnContentType();

            ActivateInputField();
        }

        // �ھڤ��e���������L����
        private void OpenKeyboardBasedOnContentType()
        {
            // �ھڿ�J�ت� contentType �����L����
            TouchScreenKeyboardType keyboardType = contentType switch
            {
                ContentType.IntegerNumber => TouchScreenKeyboardType.NumberPad,
                ContentType.DecimalNumber => TouchScreenKeyboardType.DecimalPad,
                ContentType.EmailAddress => TouchScreenKeyboardType.EmailAddress,
                ContentType.Password or ContentType.Pin => TouchScreenKeyboardType.Default,// �i�H�ھڻݨD�]�m����L����
                _ => TouchScreenKeyboardType.Default,
            };

            // ���}��������L
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
