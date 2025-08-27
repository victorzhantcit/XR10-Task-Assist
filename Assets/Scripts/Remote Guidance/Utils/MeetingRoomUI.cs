using System;
using TMPro;
using UnityEngine;
using Guidance.Dtos;

namespace Guidance.Utils
{
    public class MeetingRoomUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _description;

        private Action _onClickedAction;
        private string _title = string.Empty;
        private string _host = string.Empty;
        private int _guestCount;
        private string _startTime;

        public void SetUp(RoomInfo data, Action action)
        {
            _title = data.RoomName;
            _host = data.RoomMasterWSID;
            _guestCount = data.Clients;
            _startTime = data.StartTime;
            _onClickedAction = action;
            UpdateUI();
        }

        public void CreateRoom(string roomName, string host)
        {
            _title = roomName;
            _host = host;
            _guestCount = 0;
            _startTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        }

        public void HandleRoomClicked()
        {
            _onClickedAction?.Invoke();
        }

        public void UpdateTitle(string title)
        {
            _title = title;
            UpdateUI();
        }

        public void ShowRoom()
        {
            UpdateUI();
        }

        public void GuestJoin()
        {
            _guestCount--;
            UpdateUI();
        }

        public void GuestLeave()
        {
            _guestCount++;
            UpdateUI();
        }

        private void UpdateUI()
        {
            _description.text = $"{_title}\n" +
                $"主持人 {_host}\n" +
                $"與會人員: {_guestCount}\n" +
                $"開始時間: {_startTime}";
        }
    }
}
