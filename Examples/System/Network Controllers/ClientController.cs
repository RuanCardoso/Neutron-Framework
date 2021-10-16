using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static NeutronNetwork.UI.NeutronUI;

namespace NeutronNetwork.Examples
{
    public partial class ClientController : ClientSide
    {
        //* Define se a conex�o do cliente deve ser iniciada automaticamente.
        protected override bool AutoStartConnection => false;
        //* Ref
        private Canvas _loginPanel;
        //* Ref
        private Canvas _channelsPanel;
        //* Ref
        private Button[] _channelsButton;
        //* Player
        [SerializeField] private GameObject _playerPrefab;

        //* Use esta chamada para iniciar o servidor manualmente.
        //* Necess�rio desativar "AutoStart" no servidor em inspector.
        //* Neutron.Server.StartServer();

        private void Ui()
        {
            _loginPanel = GetUIComponent<Canvas>("Login");
            _channelsPanel = GetUIComponent<Canvas>("Channels");
            _channelsButton = GetUIComponent<Button>("Channels", "Background", new string[] { "Channel 1", "Channel 2", "Channel 3" });

            for (int i = 0; i < _channelsButton.Length; i++)
            {
                int index = i;
                var channelButton = _channelsButton[index];
                channelButton.onClick.AddListener(() =>
                {
                    Neutron.Client.JoinChannel(index);
                });
            }
        }

        protected override void Start()
        {
            Ui();
            base.Start();
            {
                UILogic.OnAuthentication += Connect;
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnNeutronConnected(bool isSuccess, Neutron neutron)
        {
            base.OnNeutronConnected(isSuccess, neutron);
            {
                if (isSuccess)
                    LogHelper.Info("Neutron connected with successful.");
                else
                    LogHelper.Error("Neutron connection failed!");
            }
        }

        protected override void OnNeutronAuthenticated(bool isSuccess, JObject properties, Neutron neutron)
        {
            base.OnNeutronAuthenticated(isSuccess, properties, neutron);
            {
                if (isSuccess)
                {
                    LogHelper.Info($"Authenticated with successful");
                    NeutronSchedule.ScheduleTask(() =>
                    {
                        _loginPanel.enabled = false;
                        _channelsPanel.enabled = true;
                    });
                }
            }
        }

        protected override void OnPlayerConnected(NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnPlayerConnected(player, isMine, neutron);
            {
                if (isMine)
                    LogHelper.Info($"The player is ready to use! {neutron.LocalPlayer.Get["Team"]}");
            }
        }

        protected override void OnPlayerJoinedChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            LogHelper.Info($"The [{player.Nickname}] player has entered on channel.");
            neutron.JoinRoom(0);
        }

        protected override void OnPlayerJoinedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnPlayerJoinedRoom(room, player, isMine, neutron);
            {
                LogHelper.Info($"The [{player.Nickname}] player has entered on room.");
                NeutronSchedule.ScheduleTask(() =>
                {
                    _channelsPanel.enabled = false;
                    //* Carrega a cena de jogo de exemplo.
                    SceneManager.LoadScene(1, LoadSceneMode.Additive);
                });
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode load)
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                var writer = Neutron.Client.BeginPlayer(stream, Vector3.zero, Quaternion.identity);
                writer.Write();
                Neutron.Client.EndPlayer(stream, 10);
            }
        }

        public void Connect(string user, string pass) => Connect(authentication: new Authentication(user, pass));
    }

    public partial class ClientController
    {
        [gRPC(ID = 10, TargetTo = Packets.TargetTo.All, TunnelingTo = Packets.TunnelingTo.Room)]
        public void SpawnPlayer(NeutronStream.IReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron neutron)
        {
            NeutronSchedule.ScheduleTask(() =>
            {
                if (neutron.EndPlayer(reader, out Vector3 pos, out Quaternion rot))
                    Neutron.NetworkSpawn(reader, isServer, player, _playerPrefab, pos, rot, neutron);
                else
                    LogHelper.Error("Failed to instantiate player!");
            });
        }
    }
}