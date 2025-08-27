using MRTK.Extensions;
using System;
using System.Collections;
using UnityEngine;

namespace FMSolution.FMETP
{
    public class PhotoCaptureFMETP : MonoBehaviour
    {
        [SerializeField] private TransparentPromptDialog _dialog; // ���ܮ�
        [SerializeField] private GameViewEncoder _gameViewEncoder; // FMETP �� GameViewEncoder
                                                                   //[SerializeField] private RawImage _rawImage; // �Ω���ܺI�Ϫ� UI

        public void CapturePhoto(Action<Texture2D> captureResponse)
        {
            StartCoroutine(ApplyWebCamTexture(captureResponse));
        }

        private IEnumerator ApplyWebCamTexture(Action<Texture2D> captureResponse)
        {
            // �T�O GameViewEncoder �ҥ�
            _gameViewEncoder.enabled = true;

            // �I�ϭ˭p��
            int countdown = 3;
            while (countdown > 0)
            {
                _dialog.Setup(true, $"�I�ϭ˭p��...{countdown--}");
                yield return new WaitForSeconds(1f);
            }

            _dialog.Setup(false);

            // ���� WebCamTexture �N��
            yield return new WaitUntil(() =>
                _gameViewEncoder.WebcamTexture != null && _gameViewEncoder.WebcamTexture.isPlaying);

            // �O�s�� Texture2D �ñN���G�Ǧ^
            Texture2D capturedTexture = SavePhotoToTexture2D();
            if (capturedTexture != null)
            {
                //_rawImage.texture = capturedTexture; // �b UI ���
                captureResponse?.Invoke(capturedTexture);
            }
            else
            {
                Debug.LogError("�I�ϥ��ѡI");
                captureResponse?.Invoke(null);
            }

            // ���� GameViewEncoder
            _gameViewEncoder.enabled = false;
        }

        private Texture2D SavePhotoToTexture2D()
        {
            WebCamTexture webcamTexture = _gameViewEncoder.WebcamTexture;
            if (webcamTexture == null)
            {
                Debug.LogError("WebCamTexture �|����l�ơI");
                return null;
            }

            // �Ы� Texture2D �ñq WebCamTexture ����������ƾ�
            Texture2D photoTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
            photoTexture.SetPixels(webcamTexture.GetPixels());
            photoTexture.Apply();

            Debug.Log("Photo captured and saved as Texture2D.");
            return photoTexture;
        }
    }
}
