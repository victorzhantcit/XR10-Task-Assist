using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using MRTK.Extensions;
using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaskAssist.Core;
using TMPro;
using UnityEngine;
using User.Dtos;

namespace User.Core
{
    public class LoginSlate : MonoBehaviour
    {
        [SerializeField] private ServiceManager _service;
        [SerializeField] private PressableButton _backButton;
        [SerializeField] private HandMenuStatus _handMenuVisualizer;
        [SerializeField] private TransparentPromptDialog _promptDialog;
        [SerializeField] private DialogPoolHandler _dialogPoolHandler;

        [Header("Login")]
        [SerializeField] private RectTransform _loginView;
        [SerializeField] private VirtualizedScrollRectList _userList;

        [Header("NewUser")]
        [SerializeField] private RectTransform _accountView;
        [SerializeField] private RectTransform _firstLoginArea;
        [SerializeField] private TMP_Text _pinTitle;
        [SerializeField] private LineRenderer _pinLine;
        [SerializeField] private CustomMRTKTMPInputField _accountInputField;
        [SerializeField] private CustomMRTKTMPInputField _passwordInputField;
        [SerializeField] private PressableButton _clearPinButton;
        [SerializeField] private PressableButton _confirmPinButton;

        private List<Transform> _pinSphereAnchors = new List<Transform>();
        private readonly Stack<ViewType> _viewHistory = new Stack<ViewType>();
        private List<int> _currentPin = new List<int>();
        private List<int> _secondCheckPin = new List<int>();
        private List<int> _correctPin = new List<int>();
        private ViewType _currentView = ViewType.Login;
        private Dictionary<string, string> _userFiles = new Dictionary<string, string>();
        private string _selectUserId = string.Empty;
        private bool _pinValidated = false;
        private bool _loginServerResponded = false;
        private bool _loginServerLocker = false; // 禁止多次向server發送login訊息

        private enum ViewType
        {
            Login,
            NewUser,
            Pin
        }

        private void Awake() => _viewHistory.Push(ViewType.Login);

        private void Start()
        {
            _userList.OnVisible += HandleUserItemVisible;
            SwitchToLogin();
        }

        private void LateUpdate()
        {
            if (_currentView == ViewType.Login) return;

            _pinLine.positionCount = _pinSphereAnchors.Count;
            _pinLine.SetPositions(_pinSphereAnchors.Select(t => t.position).ToArray());
        }

        private void HandleUserItemVisible(GameObject gameObject, int index)
        {
            UserLoginListItem userLoginListItem = gameObject.GetComponent<UserLoginListItem>();

            var usernameFilePair = _userFiles.ElementAt(index);
            userLoginListItem.SetContent(usernameFilePair.Value);
            userLoginListItem.SetOnClicked(() =>
            {
                _selectUserId = usernameFilePair.Key;
                SwitchToPin();
            });
        }

        public void SwitchToLogin() => SetView(ViewType.Login);
        public void SwitchToNewUser() => SetView(ViewType.NewUser);
        public void SwitchToPin() => SetView(ViewType.Pin);

        public void SwitchToPreviousView()
        {
            // 確保 _viewHistory 不會彈出到空 (始終保留第一個 View)
            if (_viewHistory.Count > 1) _viewHistory.Pop();
            SetView(_viewHistory.Peek());
            _backButton.gameObject.SetActive(_viewHistory.Count > 1);
        }

        public void InitPinGrid(bool isNewUser)
        {
            _firstLoginArea.gameObject.SetActive(isNewUser);
            _accountInputField.text = string.Empty;
            _passwordInputField.text = string.Empty;
            _pinTitle.text = isNewUser ? "設置圖形驗證" : "圖形驗證";
            _secondCheckPin.Clear();
            _clearPinButton.gameObject.SetActive(true);
            _confirmPinButton.gameObject.SetActive(true);
            ClearPinResult();
        }

        public void ClearPinResult()
        {
            _currentPin.Clear();
            _pinLine.positionCount = 0;

            for (int i = 0; i < _pinSphereAnchors.Count; i++)
                _pinSphereAnchors[i].GetComponent<Renderer>().material.color = Color.white;

            _pinSphereAnchors.Clear();
        }

        private void AddPinPoint(Transform dotTransform, int pinCode)
        {
            _currentPin.Add(pinCode);
            _pinSphereAnchors.Add(dotTransform);
            _pinLine.positionCount = _currentPin.Count;
            _pinLine.SetPosition(_currentPin.Count - 1, dotTransform.position);
            //Debug.Log(string.Join('-', _currentPin));
        }

        public void AddToPinPattern(Collider dot)
        {
            int pinCode = dot.transform.GetSiblingIndex();
            if (_currentPin.Contains(dot.transform.GetSiblingIndex()))
                return;

            dot.GetComponent<Renderer>().material.color = Color.blue;
            AddPinPoint(dot.transform, pinCode);
        }

        public void ConfirmPinPattern()
        {
            //Debug.Log($"當前輸入的 PIN: {string.Join("", _currentPin)}");
            if (_currentPin.Count < 4)
            {
                _dialogPoolHandler.ShowDialog("長度不足！");
                return;
            }

            if (_currentView == ViewType.NewUser)
                HandleNewUserPinSetup();
            else if (_currentView == ViewType.Pin)
                HandleValidateUserPin();
        }

        private void HandleNewUserPinSetup()
        {
            if (_secondCheckPin.Count == 0)
            {
                // 第一次輸入 PIN
                _pinTitle.text = "再次確認";
                _secondCheckPin = new List<int>(_currentPin);
                ClearPinResult();
            }
            else
            {
                // 第二次驗證 PIN
                if (_currentPin.SequenceEqual(_secondCheckPin))
                {
                    _pinTitle.text = "設置成功";
                    _clearPinButton.gameObject.SetActive(false);
                    _confirmPinButton.gameObject.SetActive(false);
                    _pinValidated = true;
                }
                else
                {
                    _pinTitle.text = "兩次圖形不一致，請重新輸入";
                    ClearPinResult();
                }

                _secondCheckPin.Clear();
            }
        }

        private void HandleValidateUserPin()
        {
            if (_loginServerLocker) return;
            _loginServerLocker = true;

            bool pinValid = _currentPin.SequenceEqual(_correctPin);

            if (!pinValid)
            {
                _dialogPoolHandler.ShowDialog("圖形驗證失敗，請重新輸入！");
                ClearPinResult();
                _loginServerLocker = false;
                return;
            }

            UserData userData = SecureDataManager.LoadDataFromFile(_selectUserId);
            if (!_service.IsNetworkAvailable)
            {
                _dialogPoolHandler.ShowDialog("未連線至網路，進行離線作業");
                HandleOfflineLoginSuccess();
                _loginServerLocker = false;
                return;
            }

            UserLoginIBMSPlatformDto userLoginDto = new UserLoginIBMSPlatformDto(userData.Id, userData.Password);

            StartCoroutine(HandleLoginOverTime());
            _service.ApplicationLoginUser(userLoginDto, isSuccess =>
            {
                _loginServerLocker = false;

                if (_loginServerResponded) return;
                _loginServerResponded = true;

                if (!isSuccess)
                {
                    _dialogPoolHandler.ShowDialog("帳號密碼需要更新！");
                    SwitchToNewUser();
                    return;
                }

                HandleOnlineLoginSuccess(userData);
            });
        }

        private IEnumerator HandleLoginOverTime()
        {
            _loginServerResponded = false;
            yield return new WaitForSeconds(5f);

            if (_loginServerResponded) yield break;

            _loginServerResponded = true;
            _dialogPoolHandler.ShowDialog("伺服器無回應，進行離線作業");
            HandleOfflineLoginSuccess();
        }

        private void SetView(ViewType viewType)
        {
            _currentView = viewType;
            _loginView.gameObject.SetActive(viewType == ViewType.Login);
            _accountView.gameObject.SetActive(viewType == ViewType.NewUser || viewType == ViewType.Pin);
            // 如果新頁面比最後紀錄來的更深層，則紀錄新的View
            if (viewType > _viewHistory.Peek()) _viewHistory.Push(viewType);
            _backButton.gameObject.SetActive(_viewHistory.Count > 1);

            switch (viewType)
            {
                case ViewType.Login:
                    _userFiles = SecureDataManager.GetUserNames();
                    _userList.SetItemCount(_userFiles.Count);
                    if (_userList.isActiveAndEnabled)
                        _userList.ResetLayout();
                    _viewHistory.Clear();
                    _viewHistory.Push(ViewType.Login);
                    break;
                case ViewType.NewUser:
                    if (!_service.IsNetworkAvailable)
                    {
                        _dialogPoolHandler.ShowDialog("新增使用者須連線至網路！");
                        return;
                    }
                    InitPinGrid(true);
                    break;
                case ViewType.Pin:
                    InitPinGrid(false);
                    _correctPin = SecureDataManager.GetUserPinCodes(_selectUserId);
                    break;
                default:
                    break;
            }
        }

        public void Login()
        {
            if (!_service.IsNetworkAvailable)
            {
                _dialogPoolHandler.ShowDialog("無網際網路供連線至伺服器！");
                return;
            }

            if (string.IsNullOrEmpty(_accountInputField.text))
            {
                _dialogPoolHandler.ShowDialog("請輸入帳號！");
                return;
            }

            if (string.IsNullOrEmpty(_passwordInputField.text))
            {
                _dialogPoolHandler.ShowDialog("請輸入密碼！");
                return;
            }

            if (!_pinValidated)
            {
                _dialogPoolHandler.ShowDialog("請設置圖形驗證！");
                return;
            }

            UserLoginIBMSPlatformDto userLoginData = new UserLoginIBMSPlatformDto(_accountInputField.text, _passwordInputField.text);
            //Debug.Log(userData.Print());

            _service.ApplicationLoginUser(userLoginData, isSuccess =>
            {
                if (!isSuccess)
                {
                    _dialogPoolHandler.ShowDialog("帳號密碼錯誤");
                    _handMenuVisualizer.SetSignOutView();
                    Debug.Log("Invalid Account");
                    return;
                }

                UserData userData = new UserData();
                userData.Setup(
                    id: _accountInputField.text,
                    password: _passwordInputField.text,
                    pin: string.Join("", _currentPin),
                    role: UserRole.Undefined
                );

                HandleOnlineLoginSuccess(userData);
            });
        }

        private async void HandleOnlineLoginSuccess(UserData userData)
        {
            UserPermissionDto userPermission = await _service.GetUserPermissionOnServer();
            UserRole userRole = _service.GetUserRoleByPermission(userPermission);

            userData.Role = userRole;
            _handMenuVisualizer.SetEnumValue(userRole switch
            {
                UserRole.Staff => HandMenuView.Staff,
                UserRole.Insp => HandMenuView.Inspector,
                UserRole.Maint => HandMenuView.Worker,
                _ => HandMenuView.SignOut
            });

            SecureDataManager.SaveDataToFile(userPermission.Personal.DisplayName, userData);
            SecureDataManager.UserName = userPermission.Personal.DisplayName;
            SecureDataManager.UserRole = userRole;

            gameObject.SetActive(false);
            SwitchToLogin();
            _promptDialog.ShowLoginSuccessHint();
        }

        private void HandleOfflineLoginSuccess()
        {
            UserData userData = SecureDataManager.LoadLoggedInData();

            _handMenuVisualizer.SetEnumValue(userData.Role switch
            {
                UserRole.Staff => HandMenuView.Staff,
                UserRole.Insp => HandMenuView.Inspector,
                UserRole.Maint => HandMenuView.Worker,
                _ => HandMenuView.SignOut
            });
            SecureDataManager.UserName = SecureDataManager.GetLoggedInUserName();
            SecureDataManager.UserRole = userData.Role;

            gameObject.SetActive(false);
            SwitchToLogin();
            _promptDialog.ShowLoginSuccessHint();
        }
    }
}
