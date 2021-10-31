using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using System.Collections.Generic;
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
        public List<Component> Components = new List<Component>();
        //* Armazena o Id de rede deste objeto.
        private (int, int, RegisterMode) _viewId;

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            {
                Owner.OnDestroy += OnNeutronUnregister;
            }
        }

        /// <summary>
        ///* Destroí o objeto de rede do Matchmaking.
        /// </summary>
        public void Destroy()
        {

        }

        public void OnNeutronRegister(NeutronPlayer player, bool isServer, Neutron neutron)
        {
            if (!OnNeutronRegister(player, isServer, CompareTag("Player") ? RegisterMode.Player : RegisterMode.Dynamic, neutron))
                MonoBehaviour.Destroy(gameObject);
        }

        public override bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterMode registerMode, Neutron neutron)
        {
            base.OnNeutronRegister(player, isServer, registerMode, neutron);
            {
                //* O jogador dono da instância de neutron.
                NeutronPlayer instanceOwner = neutron.LocalPlayer;
                //* Define o jogador dono deste objeto.
                Owner = player;
                //* Define se este objeto é o do servidor.
                IsServer = isServer;
                //* Define o tipo de registro.
                RegisterMode = registerMode;
                //* Define um ID para identificar o objeto de algum jogador.
                int keyId = Owner.Id;
                // Obtém o matchmaking atual.
                INeutronMatchmaking matchmaking = IsServer ? player.Matchmaking : instanceOwner.Matchmaking;

                if (!IsServer)
                    SceneHelper.MoveToContainer(gameObject, neutron._sceneName);
                else if (player.IsInMatchmaking())
                {
                    if (!Owner.IsInRoom())
                        SceneHelper.MoveToContainer(gameObject, $"Channel({Owner.Channel.Id}) - [Container] - {SceneHelper.GetSideTag(IsServer)}");
                    else if (Owner.IsInChannel())
                        SceneHelper.MoveToContainer(gameObject, $"Room({Owner.Room.Id}) - [Container] - {SceneHelper.GetSideTag(IsServer)}");
                }
                else
                    return LogHelper.Error("Matchmaking not found!");

                if (IsServer)
                    Helper.SetColor(this, Color.red); // define uma cor para o objeto do servidor.

                if (registerMode == RegisterMode.Player)
                {
                    if (Id == 0)
                    {
                        //* Define um nome de identificação para este objeto.
                        gameObject.name = $"Player -> {Owner.Nickname}";
                        //* Define o ID deste objeto.
                        Id = Owner.Id;
                        //* Define este objeto como o objeto de rede do jogador.
                        Owner.NeutronView = this;
                    }
                    else
                        return LogHelper.Error("Dynamically instantiated objects must have their ID at 0.");
                }
                else if (registerMode == RegisterMode.Dynamic)
                {
                    if (Id == 0)
                    {
                        //* Define o ID deste objeto.
                        Id = player.SceneObjectId++;
                        // if (!isServer) //! test Mode
                        //     LogHelper.Error($"{IsServer} : {Id} -> {matchmaking.Name}");
                        //* Define um nome de identificação para este objeto.
                        gameObject.name = $"Dynamic Object -> [{name}] #-{Id}";
                    }
                    else
                        return LogHelper.Error("Dynamically instantiated objects must have their ID at 0.");
                }
                else if (registerMode == RegisterMode.Scene)
                {
                    //* Define um nome de identificação para este objeto.
                    gameObject.name = $"Scene Object -> [{name.Replace("(Clone)", "")}] [{(IsServer ? "Server" : "Client")}] #-{Id}";
                    //* Objetos de cena tem ID 0;
                    keyId = 0;
                }
                //* Invoca o awake.
                OnNeutronAwake();
                // define a instância que invocou este metódo.
                This = !IsServer ? neutron : Neutron.Server.Instance;
                //* Vamos adicionar este objeto de rede no atual Matchmaking.
                _viewId = (keyId, Id, registerMode);
                if (!(matchmaking.Views.Count <= short.MaxValue))
                    return LogHelper.Error($"You have reached the object limit for this matchmaking.");
                if (!matchmaking.Views.TryAdd(_viewId, this))
                    return LogHelper.Error($"An object with the same id already exists: [{name}] :Id -> {player.Id} - [{matchmaking.Name} - {_viewId}] -> Server: {IsServer} -> Count {matchmaking.Views.Count}");
                // Invoca os metódos virtual e define como pronto para uso.
                Invoke();
            }

            void Invoke()
            {
                MakeAttributes();
                //------------------------------------------------------------------
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

        private async void OnNeutronUnregister()
        {
            //* Desaloca para evitar vazamento de memória.
            Owner.OnDestroy -= OnNeutronUnregister;
            //* Agora vamos destruir e dar "unregister".
            if (AutoDestroy)
            {
                INeutronMatchmaking matchmaking = IsServer ? Owner.Matchmaking : This.LocalPlayer.Matchmaking;
                if (matchmaking.Views.TryRemove(_viewId, out NeutronView _))
                {
                    await NeutronSchedule.ScheduleTaskAsync(() =>
                    {
                        Destroy(gameObject);
                    });
                }
                else
                    LogHelper.Error("Failed to destroy object!");
            }
        }
    }
}