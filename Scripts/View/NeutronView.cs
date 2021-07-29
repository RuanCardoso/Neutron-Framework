using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    /// <summary>
    ///* Este é o seu objeto na rede e também é o seu objeto de rede, o seu RG.
    /// </summary>
    [AddComponentMenu("Neutron/Neutron View")]
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_VIEW)]
    public class NeutronView : ViewBehaviour
    {
        #region MonoBehaviour
        private new void Awake()
        {
            base.Awake(); //* não remova esta linha. coloque seu código abaixo dele.
        }

        private void Start()
        {

        }

        private new void Update()
        {
            base.Update(); //* não remova esta linha. coloque seu código abaixo dele.
        }
        #endregion

        #region Overrides
        public override void OnNeutronStart()
        {
            base.OnNeutronStart(); //* não remova esta linha. coloque seu código abaixo dele.
        }

        public override void OnNeutronAwake()
        {
            base.OnNeutronAwake(); //* não remova esta linha. coloque seu código abaixo dele.
        }

        public override bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterType registerType, Neutron neutron, int dynamicId = 0)
        {
            base.OnNeutronRegister(player, isServer, registerType, neutron, dynamicId);
            {
                // Define o jogador dono deste objeto.
                Player = player;
                // Define se este objeto é o do servidor.
                IsServer = isServer;
                // Define o tipo de registro.
                RegisterType = registerType;
                // Define um ID para identificar o objeto de algum jogador.
                int keyId = Player.ID;

                if (!IsServer)
                    SceneHelper.MoveToContainer(gameObject, OthersHelper.GetSettings().CONTAINER_PLAYER_NAME);
                else
                {
                    if (!Player.IsInRoom())
                        SceneHelper.MoveToContainer(gameObject, $"[Container] -> Channel[{Player.Channel.ID}]");
                    else if (Player.IsInChannel())
                        SceneHelper.MoveToContainer(gameObject, $"[Container] -> Room[{Player.Room.ID}]");
                }

                if (IsServer)
                    OthersHelper.SetColor(this, Color.red); // define uma cor para o objeto do servidor.

                if (registerType == RegisterType.Player)
                {
                    if (ID == 0)
                    {
                        // Define um nome de identificação para este objeto.
                        gameObject.name = $"Player -> {Player.Nickname} [{(IsServer ? "Server" : "Client")}]";
                        // Define o ID deste objeto.
                        ID = Player.ID;
                        // Define este objeto como o objeto de rede do jogador.
                        Player.NeutronView = this;

                        // Invoca o awake.
                        OnNeutronAwake();
                    }
                    else if (!LogHelper.Error("Dynamically instantiated objects must have their ID at 0."))
                        MonoBehaviour.Destroy(gameObject);
                }
                else if (registerType == RegisterType.Dynamic)
                {
                    if (ID == 0)
                    {
                        // Define o ID deste objeto.
                        ID = dynamicId;
                        // Define um nome de identificação para este objeto.
                        gameObject.name = $"Dynamic Object -> [{name}] [{(IsServer ? "Server" : "Client")}] #-{ID}";

                        // Invoca o awake.
                        OnNeutronAwake();
                    }
                    else if (!LogHelper.Error("Dynamically instantiated objects must have their ID at 0."))
                        MonoBehaviour.Destroy(gameObject);
                }
                else if (registerType == RegisterType.Scene)
                {
                    // Define um nome de identificação para este objeto.
                    gameObject.name = $"Scene Object -> [{name.Replace("(Clone)", "")}] [{(IsServer ? "Server" : "Client")}] #-{ID}";
                    // Objetos de cena tem ID 0;
                    keyId = 0;

                    // Invoca o awake.
                    OnNeutronAwake();
                }
                // define a instância que invocou este metódo.
                if (!IsServer)
                    This = neutron;
                // Adiciona o objeto na lista de objetos de redes.
                if (IsServer)
                {
                    if (!player.Matchmaking.SceneView.Views.TryAdd((keyId, ID), this))
                        LogHelper.Error($"{IsServer} Duplicated ID [{keyId} - {ID}]");
                }
                else
                {
                    if (!neutron.Player.Matchmaking.SceneView.Views.TryAdd((keyId, ID), this))
                        LogHelper.Error($"{IsServer} Duplicated ID [{keyId} - {ID}]");
                }
                Invoke(); // Invoca os metódos virtual e define como pronto para uso.
            }

            void Invoke()
            {
                var neutronBehaviours = this.GetComponentsInChildren<NeutronBehaviour>();
                foreach (var neutronBehaviour in neutronBehaviours)
                {
                    neutronBehaviour.NeutronView = this;
                    if (neutronBehaviour.enabled)
                        neutronBehaviour.OnNeutronStart();
                }
                OnNeutronStart();
            }
            return true;
        }
        #endregion
    }
}