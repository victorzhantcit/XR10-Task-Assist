using MixedReality.Toolkit.UX;
using MRTK.Extensions;
using System;
using UnityEngine;
using UnityEngine.UI;
using WorkOrder.Core;

namespace WorkOrder.Utils
{
    public class PhotoListItem : VirtualListItem<string>
    {
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private PressableButton _removeImageButton;

        private string _photoSns;
        Action _removeImageAction = null;
        private static Func<string, Texture2D> _getPhotoTextureFunc;

        private void OnDisable()
        {
            //_removeImageAction = null;
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

        public void SetTextureConvertor(Func<string, Texture2D> getTextureFunc)
        {
            _getPhotoTextureFunc = getTextureFunc;
        }

        // 異步加載並顯示圖片的方法
        private void LoadAndDisplayPhotoAsync()
        {
            ActivateLoadingIcon(false);

            Texture2D texture = _getPhotoTextureFunc(_photoSns);

            if (texture == null)
            {
                Debug.LogWarning($"PhotoSn {_photoSns}");
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
