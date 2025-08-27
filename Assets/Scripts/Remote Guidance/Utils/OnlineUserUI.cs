using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guidance.Utils
{
    public class OnlineUserUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _userName;
        [SerializeField] private Image _onlineColor;

        public void SetUp(string userName)
        {
            _userName.text = userName;
            _onlineColor.color = Color.green;
        }
    }
}

