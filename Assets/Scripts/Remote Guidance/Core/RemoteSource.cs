using FMSolution.FMETP;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Guidance.Core
{
    public class RemoteSource : MonoBehaviour
    {
        public UnityAction<Byte[]> VideoProcessData => _video.Action_ProcessImageData;
        public UnityAction<Byte[]> AudioProcessData => _audio.Action_ProcessFMPCM16Data;

        [SerializeField] private GameViewDecoder _video;
        [SerializeField] private AudioDecoder _audio;
        [SerializeField] private TMP_Text _userName;

        private string _remoteWsId = string.Empty;
        private ushort _pairLabel = ushort.MinValue;
        private Action<string, AudioDecoder> _remoteVideoOnClicked;

        // 創建視訊解碼器
        public void Init(string remoteWsId, ushort pairLabel)
        {
            this.name = remoteWsId;
            _userName.text = remoteWsId;
            _remoteWsId = remoteWsId;
            _pairLabel = pairLabel;

            _video.label = pairLabel;

            _audio.name = remoteWsId;
            _audio.label = GetMicLabel(pairLabel);
            _audio.transform.parent = this.transform;
        }

        public void UpdateUserName(string username)
        {
            _userName.text = username;
        }

        public void SetAudioParent(Transform audioParent)
        {
            _audio.transform.SetParent(audioParent, false);
        }

        public void SetRemoteViewClickEvent(Action<string, AudioDecoder> onVideoExpandClicked)
        {
            _remoteVideoOnClicked = onVideoExpandClicked;
        }

        public void OnVideoClicked()
        {
            _remoteVideoOnClicked?.Invoke(_remoteWsId, _audio);
        }

        private ushort GetMicLabel(ushort pairLabel)
        {
            ushort nextLabel = (ushort)((pairLabel + 1) % ushort.MaxValue);
            return nextLabel < 1000 ? (ushort)1000 : nextLabel;
        }

        public void SafeDestroy()
        {
            if (_audio != null && _audio.gameObject != null)
                Destroy(_audio.gameObject);

            if (this.gameObject != null)
                Destroy(this.gameObject);
        }
    }
}
