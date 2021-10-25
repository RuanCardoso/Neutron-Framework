using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
using NeutronNetwork.UI;
using TMPro;
using UnityEngine;

namespace NeutronNetwork.Examples.Chat
{
    public class ChatClientSide : ClientSide
    {
        const int CHAT_ID = 1;
        //* Get the main neutron instance.
        Neutron Main => Neutron.Client;
        //* Start the client connection automatically.
        protected override bool AutoStartConnection => true;
        //* UI Componets
        [SerializeField] [ReadOnly] private TMP_InputField _rcvField;
        [SerializeField] [ReadOnly] private TMP_InputField _inputField;
        [SerializeField] private bool _autoChat = true;

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
                if (_inputField.text.Length > 0)
                {
                    if (_autoChat)
                        Main.SendMessage(_inputField.text, TunnelingTo.Auto); //* Send the message to all clients.
                    else
                    {
                        //* Send the message to all clients.
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                        {
                            var writer = Begin_gRPC(stream, Main);
                            writer.Write(_inputField.text);
                            writer.Write();
                            End_gRPC(CHAT_ID, stream, Protocol.Tcp, Main);
                        }
                    }
                    //* Reset inputfields.
                    _inputField.text = "";
                    _inputField.ActivateInputField();
                }
                else
                    LogHelper.Error("Input field is empty!");
            }
        }

        //* Auto chat
        protected override void OnMessageReceived(string message, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            ProcessMessage(player, message);
        }

        //* Manual chat
        [gRPC(ID = CHAT_ID, TargetTo = TargetTo.All, TunnelingTo = TunnelingTo.Auto, Cache = CacheMode.None)]
        private void OnMessageReceived(NeutronStream.IReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron instance)
        {
            if (!isServer)
                ProcessMessage(player, reader.ReadString());
        }

        private void ProcessMessage(NeutronPlayer player, string message)
        {
            NeutronSchedule.ScheduleTask(() =>
            {
                _rcvField.text += $"{player.Nickname}: {message}\n";
                _rcvField.verticalScrollbar.value = 1;
                //* Move to receive field to last message.
                _rcvField.MoveTextEnd(false);
            });
        }
    }
}