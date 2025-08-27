using System.Text;
using TMPro; // �ޤJ TextMeshPro �R�W�Ŷ�
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace MRTK.Extensions
{
    public class SpeechHandler : MonoBehaviour
    {
        private DictationRecognizer dictationRecognizer;
        private StringBuilder textSoFar; // �O�s�̲ת��奻
        private string currentDictation; // �ʺA�奻

        [SerializeField]
        private TMP_Text dynamicTextUI; // �Ω���ܰʺA���Ѫ� TMP_Text
        [SerializeField]
        private TMP_Text finalTextUI;   // �Ω���̲ܳ׵��G�� TMP_Text

        void Start()
        {
            textSoFar = new StringBuilder();

            // ��l�� DictationRecognizer
            dictationRecognizer = new DictationRecognizer();

            // �]�m DictationRecognizer ���ƥ�
            dictationRecognizer.DictationResult += OnDictationResult;
            dictationRecognizer.DictationHypothesis += OnDictationHypothesis;
            dictationRecognizer.DictationComplete += OnDictationComplete;
            dictationRecognizer.DictationError += OnDictationError;
        }

        void OnDestroy()
        {
            if (dictationRecognizer != null)
            {
                dictationRecognizer.Stop();
                dictationRecognizer.Dispose();
            }
        }

        /// <summary>
        /// �ҥλy������
        /// </summary>
        public void EnableDictation()
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Stopped)
            {
                textSoFar.Clear();
                currentDictation = string.Empty;
                dictationRecognizer.Start();

                // ���m UI
                UpdateDynamicText("");
                UpdateFinalText("");

                Debug.Log("Dictation �w�ҥ�");
            }
            else
            {
                Debug.LogWarning("Dictation �w�g�b�B�椤");
            }
        }

        /// <summary>
        /// ���λy������
        /// </summary>
        public void DisableDictation()
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
                Debug.Log("Dictation �w����");
            }
            else
            {
                Debug.LogWarning("Dictation �S���b�B��");
            }
        }

        /// <summary>
        /// �����e���Ѥ����ʺA�奻
        /// </summary>
        /// <returns></returns>
        public string GetCurrentDictation()
        {
            return currentDictation;
        }

        /// <summary>
        /// ����̲׵��G�奻
        /// </summary>
        /// <returns></returns>
        public string GetFinalDictation()
        {
            return textSoFar.ToString();
        }

        /// <summary>
        /// ��s�ʺA���Ѥ奻�� UI
        /// </summary>
        /// <param name="text"></param>
        public void UpdateDynamicText(string text)
        {
            if (dynamicTextUI != null)
            {
                dynamicTextUI.text = text;
            }
        }

        /// <summary>
        /// ��s�̲׵��G�奻�� UI
        /// </summary>
        /// <param name="text"></param>
        public void UpdateFinalText(string text)
        {
            if (finalTextUI != null)
            {
                finalTextUI.text = text;
            }
        }

        // �����㪺�y�����ѵ��G��Ĳ�o
        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log($"�̲׵��G: {text}");
            textSoFar.Append(text + " ");
            UpdateFinalText(textSoFar.ToString()); // ��s�̲׵��G�� UI
        }

        // ��y�����Ѷi�椤�A��^�{�ɵ��G��Ĳ�o
        private void OnDictationHypothesis(string text)
        {
            Debug.Log($"�ʺA�奻: {text}");
            currentDictation = text;
            UpdateDynamicText(currentDictation); // ��s�ʺA�奻�� UI
        }

        // ��y�����ѧ�����Ĳ�o
        private void OnDictationComplete(DictationCompletionCause completionCause)
        {
            if (completionCause == DictationCompletionCause.Complete)
            {
                Debug.Log("Dictation ����");
            }
            else
            {
                Debug.LogWarning($"Dictation �������A��]: {completionCause}");
            }
        }

        // ��y�����ѵo�Ϳ��~��Ĳ�o
        private void OnDictationError(string error, int hresult)
        {
            Debug.LogError($"Dictation �o�Ϳ��~: {error}, HResult: {hresult}");
        }

        /// <summary>
        /// �]�m TMP_Text ����
        /// </summary>
        /// <param name="dynamicText"></param>
        /// <param name="finalText"></param>
        public void SetTextComponents(TMP_Text dynamicText, TMP_Text finalText)
        {
            dynamicTextUI = dynamicText;
            finalTextUI = finalText;
        }
    }
}
