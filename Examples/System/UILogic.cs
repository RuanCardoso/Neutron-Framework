using TMPro;
using UnityEngine;

namespace NeutronNetwork.Examples
{
    public class UILogic : MonoBehaviour
    {
        public static event NeutronEventNoReturn<string, string> OnAuthentication;
        public static event NeutronEventNoReturn<int> OnSelectChannel;

        [Header("Authentication")]
        [SerializeField] private TMP_InputField _userField;
        [SerializeField] private TMP_InputField _passField;

        public void Connect() => OnAuthentication(_userField.text, _passField.text);
        public void SelectChannel(int channel) => OnSelectChannel(channel);
    }
}