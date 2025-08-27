using FMSolution.FMETP;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using MRTK.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaskAssist.Core;
using TMPro;
using Unity.Extensions;
using UnityEngine;
using UnityEngine.UI;
using WorkOrder.Dtos;
using WorkOrder.Utils;


namespace WorkOrder.Core
{
    public class WorkOrderDeviceView : MonoBehaviour
    {
        [SerializeField] private ServiceManager _service;
        [SerializeField] private WorkOrderRootUI _workOrderRootUI;
        [SerializeField] private PhotoCaptureFMETP _photoCapture;

        [Header("UI")]
        [SerializeField] private RectTransform _infomationLayout;
        [SerializeField] private TMP_Text _descriptionLabel, _statusLabel, _executorLabel, _estManMinuteLabel, _uploadDateLabel, _datumDateLabel, _noDataHintLabel;
        [SerializeField] private CustomMRTKTMPInputField _respondInputField;
        [SerializeField] private PressableButton _respondButton;
        [SerializeField] private Image _statusBackImage;
        [SerializeField] private VirtualizedScrollRectList _dataVirtualList, _processVirtualList, _prePhotoVirtualList, _afPhotoVirtualList;
        [SerializeField] private TabView _deviceInfoTabView; 
        [SerializeField] private RectTransform[] _interactableButtons;

        private OrderDeviceDto _currentDevice;
        private List<string> _devicePrePhotoSns = new List<string>();
        private List<string> _deviceAfPhotoSns = new List<string>();
        public List<string> DevicePrePhotoSns => _devicePrePhotoSns;
        public List<string> DeviceAfPhotoSns => _deviceAfPhotoSns;
        private bool _editable = true;
        public bool Editable => _editable;

        private float _saveDataScroll = 0f, _saveProcessScroll = 0f, _savePrePhotoScroll = 0f, _saveAfPhotoScroll = 0f;
        private bool _isPrePhotoCaptured = false;

        // Start is called before the first frame update
        void Start()
        {
            _dataVirtualList.OnVisible += OnDeviceDataItemVisible;
            _processVirtualList.OnVisible += OnDeviceProcessItemVisible;
            _prePhotoVirtualList.OnVisible += OnPrePhotoItemVisible;
            _afPhotoVirtualList.OnVisible += OnAfPhotoItemVisible;
        }

        public void SetVisible(bool visible)
        {
            if (visible == gameObject.activeSelf) return;
            this.gameObject.SetActive(visible);
            if (visible) StartCoroutine(RebuildLayout());
            else SetScrollValues();
        }

        private IEnumerator RebuildLayout()
        {
            yield return null; // ���ݤ@�V
            LayoutRebuilder.ForceRebuildLayoutImmediate(_infomationLayout);
            ResetVirtualListLayout();
            SetDeviceInfoTabView(0);
        }

        private void ResetVirtualListLayout()
        {
            if (_dataVirtualList.isActiveAndEnabled) _dataVirtualList.ResetLayout();
            if (_processVirtualList.isActiveAndEnabled) _processVirtualList.ResetLayout();
            _prePhotoVirtualList.ResetLayout();
            _afPhotoVirtualList.ResetLayout();
            RestoreScrollValues();
        }

        private void SetScrollValues(bool reset = false)
        {
            SetScrollValue(_dataVirtualList, ref _saveDataScroll, reset);
            SetScrollValue(_processVirtualList, ref  _saveProcessScroll, reset);
            SetScrollValue(_prePhotoVirtualList, ref _savePrePhotoScroll, reset);
            SetScrollValue(_afPhotoVirtualList, ref _saveAfPhotoScroll, reset);
        }

        private void SetScrollValue(VirtualizedScrollRectList list, ref float savedScroll, bool reset)
            => savedScroll = (!reset) ? list.Scroll : 0f;

        private void RestoreScrollValues()
        {
            _dataVirtualList.Scroll = _saveDataScroll;
            _processVirtualList.Scroll = _saveProcessScroll;
            _prePhotoVirtualList.Scroll = _savePrePhotoScroll;
            _afPhotoVirtualList.Scroll = _saveAfPhotoScroll;
        }

        public void InitVirtualLists()
        {
            SetCurrentDevicePhotoSns();
            InitVirtualList(_dataVirtualList, _currentDevice.numericalData.Count);
            InitVirtualList(_processVirtualList, _currentDevice.records.Count);
            InitVirtualList(_prePhotoVirtualList, _devicePrePhotoSns.Count);
            InitVirtualList(_afPhotoVirtualList, _deviceAfPhotoSns.Count);
            SetScrollValues(true);
        }

        private void InitVirtualList(VirtualizedScrollRectList list, int count)
        {
            list.SetItemCount(0);
            list.SetItemCount(count);
        }

        private void SetCurrentDevicePhotoSns()
        {
            _devicePrePhotoSns.Clear();
            _deviceAfPhotoSns.Clear();
            if (!string.IsNullOrEmpty(_currentDevice.prePhotoSns)) _devicePrePhotoSns = _currentDevice.prePhotoSns.Split(',').ToList();
            if (!string.IsNullOrEmpty(_currentDevice.afPhotoSns)) _deviceAfPhotoSns = _currentDevice.afPhotoSns.Split(',').ToList();
        }

        private void OnDeviceDataItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _currentDevice.numericalData.Count) return;

            DeviceDataListItem item = gameObject.GetComponent<DeviceDataListItem>();
            DeviceNumericalDto recordData = _currentDevice.numericalData[index];

            item.SetContent(recordData);
        }

        private void OnDeviceProcessItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _currentDevice.records.Count) return;

            DeviceProcessListItem item = gameObject.GetComponent<DeviceProcessListItem>();
            RecordDto recordData = _currentDevice.records[index];

            item.SetContent(recordData);
            item.SetColor(_workOrderRootUI.GetColorByStatus(recordData.status));
        }

        private void OnPrePhotoItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _devicePrePhotoSns.Count) return;

            PhotoListItem item = gameObject.GetComponent<PhotoListItem>();
            string targetData = _devicePrePhotoSns[index];

            item.SetTextureConvertor(_service.GetWorkPhotoTexture);
            item.SetContent(targetData, index, _currentDevice.IsProcessing);
            item.SetRemoveAction(() =>
            {
                Debug.Log(".RemovePhoto(_devicePrePhotoSns[index]);");
                _service.RemoveWorkPhoto(_devicePrePhotoSns[index]);
                _devicePrePhotoSns.RemoveAt(index);
                _currentDevice.prePhotoSns = string.Join(',', _devicePrePhotoSns);
                _prePhotoVirtualList.SetItemCount(_devicePrePhotoSns.Count);
                _prePhotoVirtualList.ResetLayout();
            });
        }

        private void OnAfPhotoItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _deviceAfPhotoSns.Count) return;

            PhotoListItem item = gameObject.GetComponent<PhotoListItem>();
            string targetData = _deviceAfPhotoSns[index];

            item.SetTextureConvertor(_service.GetWorkPhotoTexture);
            item.SetContent(targetData, index, _currentDevice.IsProcessing);
            item.SetRemoveAction(() =>
            {
                Debug.Log(".RemovePhoto(_deviceAfPhotoSns[index]);");
                _service.RemoveWorkPhoto(_deviceAfPhotoSns[index]);
                _deviceAfPhotoSns.RemoveAt(index);
                _currentDevice.afPhotoSns = string.Join(',', _deviceAfPhotoSns);
                _afPhotoVirtualList.SetItemCount(_deviceAfPhotoSns.Count);
                _afPhotoVirtualList.ResetLayout();
            });
        }

        public void SetDetails(OrderDeviceDto orderDeviceDto)
        {
            _currentDevice = orderDeviceDto;
            _editable = orderDeviceDto.IsProcessing;
            _descriptionLabel.text = orderDeviceDto.code;
            _statusLabel.text = orderDeviceDto.translatedStatus;
            _statusBackImage.color = _workOrderRootUI.GetColorByStatus(orderDeviceDto.status);
            _executorLabel.text = orderDeviceDto.executor;
            _datumDateLabel.text = GetFormatDate(orderDeviceDto.dataTime);
            _uploadDateLabel.text = GetFormatDate(orderDeviceDto.submitTime);
            _respondInputField.text = ValidDeviceRespond(orderDeviceDto.respond) ? orderDeviceDto.respond : "";
            _respondInputField.interactable = false;
            _respondButton.enabled = false;

            for (int i = 0; i < _interactableButtons.Length; i++)
                _interactableButtons[i].gameObject.SetActive(_editable);

            InitVirtualLists();
            SetScrollValues(true);
        }

        private string GetFormatDate(string dateString) => string.IsNullOrEmpty(dateString) ? "----/--/-- --:--" : dateString;
        private bool ValidDeviceRespond(string respond) => respond != "Start" && !string.IsNullOrEmpty(respond);

        private void SetDeviceInfoTabView(int index)
        {
            if (index < 0 || index > _deviceInfoTabView.TabSections.Length) return;
            _deviceInfoTabView.ToggleCollection.SetSelection(index);
            UpdateDeviceDetail(index);
        }

        public void UpdateDeviceDetail(int tabIndex)
        {
            int listDataCount = 0;
            if (tabIndex == 0) listDataCount = _currentDevice.numericalData.Count;
            else if (tabIndex == 1) listDataCount = _currentDevice.records.Count;
            _noDataHintLabel.gameObject.SetActive(listDataCount <= 0);
        }

        public void OnDeviceRespondTextChanged(string newRespond)
        {
            bool invalidValue = newRespond == null || _currentDevice.respond == newRespond || newRespond == "Start";

            if (invalidValue) return;
            _currentDevice.respond = newRespond;
        }

        public void TakePrePhoto() => TakePhotoByProcess(true);
        public void TakeAfPhoto() => TakePhotoByProcess(false);

        // �}�l�I��
        private void TakePhotoByProcess(bool isPrePhoto)
        {
            _isPrePhotoCaptured = isPrePhoto;
            _workOrderRootUI.ShowSlateContent(false);
            _photoCapture.CapturePhoto(texture => HandleScreenshotTaken(texture != null, texture));
        }

//        private IEnumerator ScreenShotCountdown(int seconds)
//        {
//#if UNITY_EDITOR
//            _workOrderRootUI.UpdatePromptDialog(true, "���b�B�z�I�ϡA�ЫO������...");
//            yield return new WaitForSeconds(1);
//            Texture2D texture = SimulateScreenshotResult(177, 100, 30);
//            HandleScreenshotTaken(true, texture);
//#else
//            while (seconds > 0)
//            {
//                _workOrderRootUI.UpdatePromptDialog(true, "�I�ϭ˭p��\n" + seconds--);
//                yield return new WaitForSeconds(1);
//            }

//            _workOrderRootUI.UpdatePromptDialog(true, "���b�B�z�I�ϡA�ЫO������...");
//            _screenshotHandler.TakeScreenShot();
//#endif

//            yield return null;
//        }

        private void HandleScreenshotTaken(bool success, Texture2D capturedTexture)
        {
            Debug.Log("ReceiveScreenShotTaken");
            if (!success)
            {
                _workOrderRootUI.ShowDialog("�ù��I�ϥ���", "�Э��s�I�ϩ��p���}�o�H��");
                Debug.LogWarning("Capture failed!");
                return;
            }

            _workOrderRootUI.UpdatePromptDialog(false);
            _workOrderRootUI.ShowSlateContent(true);

            string capturedPhotoBase64 = System.Convert.ToBase64String(capturedTexture.EncodeToJPG());
            string tempSns = _service.AddWorkPhoto(capturedPhotoBase64);

            UpdatePhotos(tempSns);
        }

        private void UpdatePhotos(string addPhotoSns)
        {
            if (_isPrePhotoCaptured)
            {
                _devicePrePhotoSns.Add(addPhotoSns);
                _currentDevice.prePhotoSns = string.Join(',', _devicePrePhotoSns);

                _prePhotoVirtualList.SetItemCount(_devicePrePhotoSns.Count);
                _prePhotoVirtualList.ResetLayout();
            }
            else
            {
                _deviceAfPhotoSns.Add(addPhotoSns);
                _currentDevice.afPhotoSns = string.Join(',', _deviceAfPhotoSns);

                _afPhotoVirtualList.SetItemCount(_deviceAfPhotoSns.Count);
                _afPhotoVirtualList.ResetLayout();
            }
        }

        //public void OnPrePhotoSubmitClicked() => _workOrderRootUI.SubmitPrePhotos();

        public void OnPrePhotoSubmitClicked()
        {
            PrePhotoSubmitDto prePhotoSubmitDto = new PrePhotoSubmitDto();
            bool isValid = prePhotoSubmitDto.SetupAndValidate(
                _workOrderRootUI.CurrentRecordSn, 
                _currentDevice,
                _service.GetWorkPhotoBase64
            );

            if (!isValid)
            {
                _workOrderRootUI.ShowDialog("�B�z�e�Ϥ����o����");
                return;
            }

            List<KeyValue> requestData = KeyValueConverter.ToKeyValues(prePhotoSubmitDto);
            _service.PostPrePhotoSubmit(requestData, HandlePrePhotoSubmitted);
        }

        private void HandlePrePhotoSubmitted(PrePhotoSubmitResponseDto response)
        {
            bool isOfflinePost = response == null;

            if (isOfflinePost)
            {
                _workOrderRootUI.ShowDialog("��ƤW�ǥ���", "�е��ݫ�_�������ҫ�A�A�z�L�D������s���");
                return;
            }

            string[] responsePhotoSns = response.PhotoSns.Split(',');

            for (int i = 0; i < responsePhotoSns.Length; i++)
            {
                string remotePhotoSns = responsePhotoSns[i];
                string localPhotoSns = _devicePrePhotoSns[i];

                _service.UpdateWorkPhotoSns(localPhotoSns, remotePhotoSns);
                _devicePrePhotoSns[i] = remotePhotoSns;
            }
            _currentDevice.prePhotoSns = response.PhotoSns;
            _workOrderRootUI.ShowDialog($"�w��s {_currentDevice.code} �B�z�e�Ϥ�");
        }

        private void HandleSubmitDataChanged(DeviceWorkSubmitResponseDto response)
        {
            bool isOfflinePost = response == null;

            if (isOfflinePost)
            {
                _workOrderRootUI.ShowDialog("��ƤW�ǥ���", "�е��ݫ�_�������ҫ�A�A�z�L�D������s���");
                return;
            }

            if (!string.IsNullOrEmpty(response.AfPhotoSns))
            {
                string[] responsePhotoSns = response.AfPhotoSns.Split(',');

                for (int i = 0; i < responsePhotoSns.Length; i++)
                {
                    string remotePhotoSns = responsePhotoSns[i];
                    string localPhotoSns = _deviceAfPhotoSns[i];

                    _service.UpdateWorkPhotoSns(localPhotoSns, remotePhotoSns);
                    _deviceAfPhotoSns[i] = remotePhotoSns;
                }
                _currentDevice.afPhotoSns = response.AfPhotoSns;
            }

            _workOrderRootUI.ShowDialog($"�w��s {_currentDevice.code} �ثe�i��");
        }

        private DeviceWorkSubmitDto PrepareWorkSubmit(DeviceWorkSubmitType submitType)
        {
            DeviceWorkSubmitDto deviceWorkSubmitDto = new DeviceWorkSubmitDto();
            bool isValid = deviceWorkSubmitDto.SetupAndValidate(
                submitType, 
                _workOrderRootUI.CurrentWorkOrder, 
                _currentDevice,
                _service.GetWorkPhotoBase64
            );

            if (isValid) return deviceWorkSubmitDto;

            DeviceWorkSubmitDto.ValidateStatus validateStatus = deviceWorkSubmitDto.GetValidateStatus();

            if (validateStatus == DeviceWorkSubmitDto.ValidateStatus.InvalidPhoto)
                _workOrderRootUI.ShowDialog("�B�z��Ϥ����o���šI");
            else if (validateStatus == DeviceWorkSubmitDto.ValidateStatus.InvalidRespond)
                _workOrderRootUI.ShowDialog("�п�J�`���I");
            return null;
        }

        public void OnRecordClicked() => RecordCurrentDeviceWork();
        public void OnPauseClicked() => PauseCurrentDeviceWork();
        public void OnSubmitClicked() => SubmitCurrentDeviceWork();

        private void RecordCurrentDeviceWork()
        {
            _workOrderRootUI.ShowSubmitDialog("�п�J�����Ƶ��]���o���š^", string.Empty, (enteredRespond) =>
            {
                if (string.IsNullOrEmpty(enteredRespond)) return;

                _currentDevice.respond = enteredRespond;

                DeviceWorkSubmitDto deviceWorkSubmitDto = PrepareWorkSubmit(DeviceWorkSubmitType.Record);
                if (deviceWorkSubmitDto == null) return;

                List<KeyValue> requestData = KeyValueConverter.ToKeyValues(deviceWorkSubmitDto);
                _service.PostDeviceWorkSubmit(requestData, HandleRecordDeviceWorkResponse);
            });
        }

        private void HandleRecordDeviceWorkResponse(DeviceWorkSubmitResponseDto response)
        {
            HandleSubmitDataChanged(response);
            _currentDevice.records.Add(new RecordDto()
            {
                respondTime = DateTime.Now.ToString(_workOrderRootUI.DATE_FORMAT_LOCAL),
                status = "Processing",
                staffName = "��",
                respond = _currentDevice.respond
            });
            _currentDevice.respond = string.Empty;
            SetDetails(_currentDevice);
        }

        private void PauseCurrentDeviceWork()
        {
            _workOrderRootUI.ShowSubmitDialog("��J�ȵ��Ƶ��]���o���š^", string.Empty, (enteredRespond) =>
            {
                if (string.IsNullOrEmpty(enteredRespond)) return;

                _currentDevice.respond = enteredRespond;

                DeviceWorkSubmitDto deviceWorkSubmitDto = PrepareWorkSubmit(DeviceWorkSubmitType.Pause);
                if (deviceWorkSubmitDto == null) return;

                List<KeyValue> requestData = KeyValueConverter.ToKeyValues(deviceWorkSubmitDto);
                _service.PostDeviceWorkSubmit(requestData, HandlePauseDeviceWorkResponse);
            });
        }

        private void HandlePauseDeviceWorkResponse(DeviceWorkSubmitResponseDto response)
        {
            HandleSubmitDataChanged(response);
            _currentDevice.records.Add(new RecordDto()
            {
                respondTime = DateTime.Now.ToString(_workOrderRootUI.DATE_FORMAT_LOCAL),
                status = "Pause",
                staffName = "��",
                respond = _currentDevice.respond
            });
            _currentDevice.respond = string.Empty;
            _currentDevice.SetStatusToPause();
            _workOrderRootUI.SwitchToPreviousView();
        }

        private void SubmitCurrentDeviceWork()
        {
            _workOrderRootUI.ShowSubmitDialog("��J���u�`���]���o���š^", string.Empty, (enteredRespond) =>
            {
                if (string.IsNullOrEmpty(enteredRespond)) return;

                _currentDevice.respond = enteredRespond;

                DeviceWorkSubmitDto deviceWorkSubmitDto = PrepareWorkSubmit(DeviceWorkSubmitType.Submit);
                if (deviceWorkSubmitDto == null) return;

                List<KeyValue> requestData = KeyValueConverter.ToKeyValues(deviceWorkSubmitDto);
                _service.PostDeviceWorkSubmit(requestData, HandleSubmitDeviceWorkResponse);
            });
        }

        private void HandleSubmitDeviceWorkResponse(DeviceWorkSubmitResponseDto response)
        {
            HandleSubmitDataChanged(response);
            _currentDevice.SetStatusToSubmitted();
            _currentDevice.records.Add(new RecordDto()
            {
                respondTime = DateTime.Now.ToString(_workOrderRootUI.DATE_FORMAT_LOCAL),
                status = "Submitted",
                staffName = "��",
                respond = _currentDevice.respond
            });
            _workOrderRootUI.UpdateSubmittedDeviceData();
            _workOrderRootUI.SwitchToPreviousView();
        }

#if UNITY_EDITOR
        private Texture2D SimulateScreenshotResult(int width, int height, int blockSize)
        {
            // �Ыث��w�ؤo�� Texture2D
            Texture2D texture = new Texture2D(width, height);

            // �N��� texture ��R���զ�Ψ�L�I����
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.white);  // �I���]���զ�
                }
            }

            // �H���ͦ�5���I�A�ì��C���I�ͦ��H���C����
            for (int i = 0; i < 5; i++)
            {
                // �H���ͦ��@�Ӱ_�l�I
                int randomX = UnityEngine.Random.Range(0, width - blockSize);
                int randomY = UnityEngine.Random.Range(0, height - blockSize);

                float r = UnityEngine.Random.value; // �H������q�D
                float g = UnityEngine.Random.value; // �H�����q�D
                float b = UnityEngine.Random.value; // �H���Ŧ�q�D
                Color randomColor = new Color(r, g, b);

                // �b�o���H���I�ͦ��@���H���j�p�����
                for (int y = 0; y < blockSize; y++)
                {
                    for (int x = 0; x < blockSize; x++)
                    {
                        texture.SetPixel(randomX + x, randomY + y, randomColor);
                    }
                }
            }

            // ���ΩҦ��������ܤ�
            texture.Apply();

            return texture;
        }
#endif
    }
}
