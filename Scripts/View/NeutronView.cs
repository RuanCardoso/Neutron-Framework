using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Server;
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
        public override bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterMode registerType, Neutron neutron, int dynamicId = 0)
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
                    SceneHelper.MoveToContainer(gameObject, OthersHelper.GetConstants().ContainerName);
                else if (player.IsInMatchmaking())
                {
                    if (!Player.IsInRoom())
                        SceneHelper.MoveToContainer(gameObject, $"[Container] -> Channel[{Player.Channel.ID}]");
                    else if (Player.IsInChannel())
                        SceneHelper.MoveToContainer(gameObject, $"[Container] -> Room[{Player.Room.ID}]");
                }
                else
                    return LogHelper.Error("Matchmaking not found!");

                if (IsServer)
                    OthersHelper.SetColor(this, Color.red); // define uma cor para o objeto do servidor.

                if (registerType == RegisterMode.Player)
                {
                    if (Id == 0)
                    {
                        // Define um nome de identificação para este objeto.
                        gameObject.name = $"Player -> {Player.Nickname} [{(IsServer ? "Server" : "Client")}]";
                        // Define o ID deste objeto.
                        Id = Player.ID;
                        // Define este objeto como o objeto de rede do jogador.
                        Player.NeutronView = this;

                        // Invoca o awake.
                        OnNeutronAwake();
                    }
                    else
                        return LogHelper.Error("Dynamically instantiated objects must have their ID at 0.");
                }
                else if (registerType == RegisterMode.Dynamic)
                {
                    if (Id == 0)
                    {
                        // Define o ID deste objeto.
                        Id = dynamicId;
                        // Define um nome de identificação para este objeto.
                        gameObject.name = $"Dynamic Object -> [{name}] [{(IsServer ? "Server" : "Client")}] #-{Id}";

                        // Invoca o awake.
                        OnNeutronAwake();
                    }
                    else
                        return LogHelper.Error("Dynamically instantiated objects must have their ID at 0.");
                }
                else if (registerType == RegisterMode.Scene)
                {
                    // Define um nome de identificação para este objeto.
                    gameObject.name = $"Scene Object -> [{name.Replace("(Clone)", "")}] [{(IsServer ? "Server" : "Client")}] #-{Id}";
                    // Objetos de cena tem ID 0;
                    keyId = 0;

                    // Invoca o awake.
                    OnNeutronAwake();
                }
                // define a instância que invocou este metódo.
                This = !IsServer ? neutron : NeutronServer.Neutron;
                // Adiciona o objeto na lista de objetos de redes.
                if (IsServer)
                {
                    if (player.Matchmaking.SceneView.Views.Count <= short.MaxValue)
                    {
                        if (!player.Matchmaking.SceneView.Views.TryAdd((keyId, Id, registerType), this))
                            return LogHelper.Error($"{IsServer} Duplicated ID [{keyId} - {Id}]");
                    }
                    else
                        return LogHelper.Error($"You have reached the object limit for this matchmaking.");
                }
                else
                {
                    if (!neutron.Player.Matchmaking.SceneView.Views.TryAdd((keyId, Id, registerType), this))
                        return LogHelper.Error($"{IsServer} Duplicated ID [{keyId} - {Id}]");
                }
                Invoke(); // Invoca os metódos virtual e define como pronto para uso.
            }

            void Invoke()
            {
                var neutronBehaviours = GetComponentsInChildren<NeutronBehaviour>();
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
    }
}