using Inspection.Core;
using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Utils
{
    public class PhotoListItem : VirtualListItem<string>
    {
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private PressableButton _removeImageButton;

        private string _photoSns;
        Action _removeImageAction = null;
        private Func<string, Texture2D> _getTextureFunc;

        private void OnDisable()
        {
            _removeImageAction = null;
        }

        public override void SetContent(string photoSns, int index, bool interactable)
        {
            _photoSns = photoSns;
            _removeImageButton.gameObject.SetActive(interactable);

            ActivateLoadingIcon(true);
            LoadAndDisplayPhotoAsync();
        }

        public void SetRemoveAction(Action editAction)
        {
            _removeImageAction = editAction;
        }

        public void SetTextureTranslate(Func<string, Texture2D> getTexture)
        {
            _getTextureFunc = getTexture;
        }

        // 異步加載並顯示圖片的方法
        private void LoadAndDisplayPhotoAsync()
        {
            ActivateLoadingIcon(false);

            Texture2D texture = _getTextureFunc(_photoSns);

            if (texture == null)
            {
                Debug.LogWarning("圖片加載失敗或找不到圖片");
                return;
            }

            AspectRatioFitter fitter = _rawImage.GetComponent<AspectRatioFitter>();

            fitter.aspectRatio = (float)texture.width / texture.height;
            _rawImage.texture = texture;
        }

        private void ActivateLoadingIcon(bool enable) => _rawImage.transform.GetChild(0).gameObject.SetActive(enable);
        public void OnRemoveButtonClicked() => _removeImageAction?.Invoke();
    }
}
