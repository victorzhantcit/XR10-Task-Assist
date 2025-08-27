using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace MRTK.Extensions
{
    public class ScreenShotHandler : MonoBehaviour
    {
        public delegate void OnScreenShotTaken(bool success, Texture2D capturedTexture);
        public delegate void OnSaveFileCompleted(bool success);
        public delegate void OnImageLoaded(string fileName, Texture2D imageTexture);
        public event OnScreenShotTaken ScreenShotTaken;
        public event OnSaveFileCompleted SaveFileCompleted;

        private static readonly string IMAGE_EXT = ".jpg";
        private static readonly string SCREENSHOT_FOLDER = "Screenshots";
        private static readonly string FILENAME_FORMAT = "CapturedImage_{0:yyyy-MM-dd_HH-mm-ss}" + IMAGE_EXT;

        private PhotoCapture _photoCaptureObject = null;

        private string _fileFullPath = string.Empty;

        private Texture2D _capturedTexture;
        public Texture2D CapturedTexture => _capturedTexture;

        public void TakeScreenShot()
        {
            Debug.Log("TakeScreenShot");
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        }

        private void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            Debug.Log("OnPhotoCaptureCreated");
            _photoCaptureObject = captureObject;

            // 抓取最高解析度
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            CameraParameters cameraParameters = new CameraParameters()
            {
                hologramOpacity = 0.0f, // 設為 0 不顯示全像
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            captureObject.StartPhotoModeAsync(cameraParameters, OnPhotoModeStarted);
        }

        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            Debug.Log("OnPhotoModeStarted");
            if (result.success) // 如果啟用拍照成功，執行拍照功能
                _photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            else
                Debug.LogError("Unable to start photo mode!");
        }

        private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Debug.Log("OnCapturedPhotoToMemory");
            if (result.success)
            {
                // 創建一個新的Texture2D並設置正確的解析度
                Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                _capturedTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

                // 將原始圖像數據加載到Texture2D中
                photoCaptureFrame.UploadImageDataToTexture(_capturedTexture);

                PrepareFileFullPath();
            }
            else
            {
                Debug.LogError("Failed to capture photo to memory.");
            }

            ScreenShotTaken?.Invoke(result.success, _capturedTexture);

            //SaveTextureAsPNG();

            // 停止拍照模式
            _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        private void PrepareFileFullPath()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, SCREENSHOT_FOLDER);

            // 檢查資料夾是否存在，如果不存在則創建
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filename = string.Format(FILENAME_FORMAT, System.DateTime.Now);
            _fileFullPath = Path.Combine(folderPath, filename);
        }

        private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            Debug.Log("OnStoppedPhotoMode");
            _photoCaptureObject.Dispose();
            _photoCaptureObject = null;
        }

        public void SaveTextureAsPNG()
        {
            StartCoroutine(SavePNGAsync());
        }

        private IEnumerator SavePNGAsync()
        {
            byte[] pngImage = _capturedTexture.EncodeToPNG();

            // 使用工作線程進行寫檔
            Task writeTask = Task.Run(() => File.WriteAllBytes(_fileFullPath, pngImage));

            // 等待寫檔完成
            yield return new WaitUntil(() => writeTask.IsCompleted);

            if (HandleReadWriteException(writeTask.Exception, "Error writing file"))
            {
                SaveFileCompleted?.Invoke(false);
                yield break;
            }

            Debug.Log("Saved image to: " + _fileFullPath);
            SaveFileCompleted?.Invoke(true); // 保存完成後調用回調

            yield return null;
        }

        // 調用這個方法來加載圖片
        public void LoadImageAsync(string screenshotName, OnImageLoaded callback)
        {
            string filePath = Path.Combine(Application.persistentDataPath, SCREENSHOT_FOLDER, screenshotName + IMAGE_EXT);

            StartCoroutine(LoadImageCoroutine(filePath, callback));
        }

        private IEnumerator LoadImageCoroutine(string filePath, OnImageLoaded callback)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 開啟一個工作線程來讀取圖片檔案
            Task<byte[]> readTask = Task.Run(() => File.ReadAllBytes(filePath));

            // 等待讀取完成
            yield return new WaitUntil(() => readTask.IsCompleted);

            if (HandleReadWriteException(readTask.Exception, "Error reading file"))
            {
                callback?.Invoke("載入圖片失敗！！", null);
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(readTask.Result)) // 成功載入 byte 數據
                callback?.Invoke(fileName, texture);
            else
                callback?.Invoke("載入圖片失敗！！", null);
        }

        private bool HandleReadWriteException(Exception exception, string errorMessage)
        {
            if (exception != null)
            {
                Debug.LogWarning($"{errorMessage}: {exception}");
                return true;
            }
            return false;
        }
    }
}
