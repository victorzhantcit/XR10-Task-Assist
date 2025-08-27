using System.Text;
using TMPro; // 引入 TextMeshPro 命名空間
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace MRTK.Extensions
{
    public class SpeechHandler : MonoBehaviour
    {
        private DictationRecognizer dictationRecognizer;
        private StringBuilder textSoFar; // 保存最終的文本
        private string currentDictation; // 動態文本

        [SerializeField]
        private TMP_Text dynamicTextUI; // 用於顯示動態辨識的 TMP_Text
        [SerializeField]
        private TMP_Text finalTextUI;   // 用於顯示最終結果的 TMP_Text

        void Start()
        {
            textSoFar = new StringBuilder();

            // 初始化 DictationRecognizer
            dictationRecognizer = new DictationRecognizer();

            // 設置 DictationRecognizer 的事件
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
        /// 啟用語音辨識
        /// </summary>
        public void EnableDictation()
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Stopped)
            {
                textSoFar.Clear();
                currentDictation = string.Empty;
                dictationRecognizer.Start();

                // 重置 UI
                UpdateDynamicText("");
                UpdateFinalText("");

                Debug.Log("Dictation 已啟用");
            }
            else
            {
                Debug.LogWarning("Dictation 已經在運行中");
            }
        }

        /// <summary>
        /// 停用語音辨識
        /// </summary>
        public void DisableDictation()
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
                Debug.Log("Dictation 已停用");
            }
            else
            {
                Debug.LogWarning("Dictation 沒有在運行");
            }
        }

        /// <summary>
        /// 獲取當前辨識中的動態文本
        /// </summary>
        /// <returns></returns>
        public string GetCurrentDictation()
        {
            return currentDictation;
        }

        /// <summary>
        /// 獲取最終結果文本
        /// </summary>
        /// <returns></returns>
        public string GetFinalDictation()
        {
            return textSoFar.ToString();
        }

        /// <summary>
        /// 更新動態辨識文本到 UI
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
        /// 更新最終結果文本到 UI
        /// </summary>
        /// <param name="text"></param>
        public void UpdateFinalText(string text)
        {
            if (finalTextUI != null)
            {
                finalTextUI.text = text;
            }
        }

        // 當有完整的語音辨識結果時觸發
        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log($"最終結果: {text}");
            textSoFar.Append(text + " ");
            UpdateFinalText(textSoFar.ToString()); // 更新最終結果到 UI
        }

        // 當語音辨識進行中，返回臨時結果時觸發
        private void OnDictationHypothesis(string text)
        {
            Debug.Log($"動態文本: {text}");
            currentDictation = text;
            UpdateDynamicText(currentDictation); // 更新動態文本到 UI
        }

        // 當語音辨識完成時觸發
        private void OnDictationComplete(DictationCompletionCause completionCause)
        {
            if (completionCause == DictationCompletionCause.Complete)
            {
                Debug.Log("Dictation 完成");
            }
            else
            {
                Debug.LogWarning($"Dictation 未完成，原因: {completionCause}");
            }
        }

        // 當語音辨識發生錯誤時觸發
        private void OnDictationError(string error, int hresult)
        {
            Debug.LogError($"Dictation 發生錯誤: {error}, HResult: {hresult}");
        }

        /// <summary>
        /// 設置 TMP_Text 元件
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
