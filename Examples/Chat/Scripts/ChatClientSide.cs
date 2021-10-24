using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.UI;
using TMPro;
using UnityEngine;

namespace NeutronNetwork.Examples.Chat
{
    public class ChatClientSide : ClientSide
    {
        //* Get the main neutron instance.
        Neutron Main => Neutron.Client;
        //* Start the client connection automatically.
        protected override bool AutoStartConnection => true;
        //* UI Componets
        [SerializeField] [ReadOnly] private TMP_InputField _rcvField;
        [SerializeField] [ReadOnly] private TMP_InputField _inputField;

        protected override void Start()
        {
            base.Start();
            {
                //* Get UI Components
                _rcvField = NeutronUI.GetUIComponent<TMP_InputField>("Canvas", "Background", "rcvField");
                _inputField = NeutronUI.GetUIComponent<TMP_InputField>("Canvas", "Background", "inputField");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Main.SendMessage(_inputField.text, Packets.TunnelingTo.Auto);
                //* Reset inputfields.
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        protected override void OnMessageReceived(string message, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnMessageReceived(message, player, isMine, neutron);
            {
                NeutronSchedule.ScheduleTask(() =>
                {
                    _rcvField.text += $"{player.Nickname}: {message}\n";
                    _rcvField.MoveTextEnd(true);
                });
            }
        }
    }
}