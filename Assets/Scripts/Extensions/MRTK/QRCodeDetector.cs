using Microsoft.MixedReality.OpenXR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARSubsystems;

namespace MRTK.Extensions
{
    /// <summary>
    /// 需要 Microsoft.MixedReality.OpenXR ARMarkerManager
    /// 並在 AR Marker Content Prefab 加入 ARMarker, ARMarkerScale
    /// </summary>
    public class QRCodeDetector : MonoBehaviour
    {
        public delegate void OnQRCodeDetected(string qrContent);
        public event OnQRCodeDetected OnQrCodeDetected;

        //[SerializeField] private GameObject mainText;
        [SerializeField] private ARMarkerManager markerManager;

        [Header("Developement Only (Press Backspace to simulate)")]
        public string SimulatedQrContent = string.Empty;

        public Dictionary<string, float> _registeredMarkerId = new Dictionary<string, float>();
        GameObject _scannedTargetHint;
        private bool _qrDetected = true;

        //private TextMeshProUGUI _textMeshPro;

        private void Start()
        {
            if (markerManager == null)
            {
                Debug.LogError("ARMarkerManager is not assigned.");
                return;
            }

            // Subscribe to the markersChanged event
            markerManager.markersChanged += OnMarkersChanged;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_qrDetected || markerManager.enabled == false)
                return;

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Vector3 position = Camera.main.transform.position + Camera.main.transform.TransformDirection(new Vector3(0, 0, 2.0f));
                Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                Pose simulatedPose = new Pose(position, rotation);

                Debug.Log($"模擬 QRCode：{SimulatedQrContent} at {simulatedPose.position}");

                string decodedText = UnityWebRequest.UnEscapeURL(SimulatedQrContent);

                Debug.Log($"原始: {SimulatedQrContent}");
                Debug.Log($"解碼後: {decodedText}");

                // 直接呼叫原有邏輯
                OnQrCodeDetected?.Invoke(decodedText);
            }
        }
#endif

        private void OnDestroy()
        {
            if (markerManager != null)
                markerManager.markersChanged -= OnMarkersChanged;
        }

        public void EnableArMarker(bool enable)
        {
            if (enable)
            {
                markerManager.enabled = true;
                StartCoroutine(DelayUnlockScanner());
            }
            else
            {
                if (_scannedTargetHint != null)
                    _scannedTargetHint.SetActive(false);

                _registeredMarkerId.Clear();
                markerManager.enabled = false;
            }
        }

        private IEnumerator DelayUnlockScanner()
        {
            // 延遲2秒保證不會處理到上一階段的 QR Code
            yield return new WaitForSeconds(2f);
            _qrDetected = false;
        }

        /// <summary>
        /// Handles the markersChanged event and processes added, updated, and removed markers.
        /// </summary>
        /// <param name="args">Event arguments containing information about added, updated, and removed markers.</param>
        private void OnMarkersChanged(ARMarkersChangedEventArgs args)
        {
            if (_qrDetected)
                return;

            foreach (var updatedMarker in args.updated)
            {
                HandleUpdatedMarker(updatedMarker);
            }
        }

        /// <summary>
        /// Handles logic for updated markers.
        /// </summary>
        /// <param name="updatedMarker">The updated ARMarker.</param>
        private void HandleUpdatedMarker(ARMarker updatedMarker)
        {
            // Get the decoded string from the added marker
            string qrCodeString = updatedMarker.GetDecodedString();
            bool isTrackingQrCode = IsTracking(updatedMarker.trackableId, updatedMarker.lastSeenTime);

            if (isTrackingQrCode)
            {
                // 確認偵測到 QR Code
                OnQrCodeDetected?.Invoke(qrCodeString);
                _qrDetected = true;
                Debug.Log($"Tracking QR Code: {updatedMarker.trackableId}");
                _scannedTargetHint = updatedMarker.transform.GetChild(0).gameObject;
                _scannedTargetHint.SetActive(false);
            }

            UpdateRegisterTrackable(updatedMarker.trackableId, updatedMarker.lastSeenTime);
        }

        private void UpdateRegisterTrackable(TrackableId trackableId, float lastSeenTime)
        {
            _registeredMarkerId[trackableId.ToString()] = lastSeenTime;
        }

        private bool IsTracking(TrackableId trackableId, float lastSeenTime)
        {
            string trackableKey = trackableId.ToString();

            if (!_registeredMarkerId.ContainsKey(trackableKey))
                return false;

            float timeBetweenLastSeen = lastSeenTime - _registeredMarkerId[trackableKey];

            return timeBetweenLastSeen <= 2f && timeBetweenLastSeen > 0;
        }

    }
}
