using MRTK.Extensions;
using System;
using System.Collections;
using UnityEngine;

namespace FMSolution.FMETP
{
    public class PhotoCaptureFMETP : MonoBehaviour
    {
        [SerializeField] private TransparentPromptDialog _dialog; // 提示框
        [SerializeField] private GameViewEncoder _gameViewEncoder; // FMETP 的 GameViewEncoder
                                                                   //[SerializeField] private RawImage _rawImage; // 用於顯示截圖的 UI

        public void CapturePhoto(Action<Texture2D> captureResponse)
        {
            StartCoroutine(ApplyWebCamTexture(captureResponse));
        }

        private IEnumerator ApplyWebCamTexture(Action<Texture2D> captureResponse)
        {
            // 確保 GameViewEncoder 啟用
            _gameViewEncoder.enabled = true;

            // 截圖倒計時
            int countdown = 3;
            while (countdown > 0)
            {
                _dialog.Setup(true, $"截圖倒計時...{countdown--}");
                yield return new WaitForSeconds(1f);
            }

            _dialog.Setup(false);

            // 等待 WebCamTexture 就緒
            yield return new WaitUntil(() =>
                _gameViewEncoder.WebcamTexture != null && _gameViewEncoder.WebcamTexture.isPlaying);

            // 保存為 Texture2D 並將結果傳回
            Texture2D capturedTexture = SavePhotoToTexture2D();
            if (capturedTexture != null)
            {
                //_rawImage.texture = capturedTexture; // 在 UI 顯示
                captureResponse?.Invoke(capturedTexture);
            }
            else
            {
                Debug.LogError("截圖失敗！");
                captureResponse?.Invoke(null);
            }

            // 停用 GameViewEncoder
            _gameViewEncoder.enabled = false;
        }

        private Texture2D SavePhotoToTexture2D()
        {
            WebCamTexture webcamTexture = _gameViewEncoder.WebcamTexture;
            if (webcamTexture == null)
            {
                Debug.LogError("WebCamTexture 尚未初始化！");
                return null;
            }

            // 創建 Texture2D 並從 WebCamTexture 中獲取像素數據
            Texture2D photoTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
            photoTexture.SetPixels(webcamTexture.GetPixels());
            photoTexture.Apply();

            Debug.Log("Photo captured and saved as Texture2D.");
            return photoTexture;
        }
    }
}
