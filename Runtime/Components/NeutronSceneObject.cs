using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(NeutronView))]
    [RequireComponent(typeof(AllowOnServer))]
    [AddComponentMenu("Neutron/Neutron Scene Object")]
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CLIENT)]
    public class NeutronSceneObject : MonoBehaviour
    {
        public static NeutronEventNoReturn<NeutronPlayer, bool, Scene, MatchmakingMode, INeutronMatchmaking, Neutron> OnSceneObjectRegister = delegate { };

        [SerializeField] [ReadOnly] private NeutronView _neutronView;
        [SerializeField] [HorizontalLine] private bool _hasMap = false;
        [SerializeField] [ShowIf("_hasMap")] private MatchmakingMode _matchmakingMode = MatchmakingMode.Room;
        [SerializeField] [ShowIf("_hasMap")] private string _mapKey = "Map";
        [SerializeField] [ShowIf("_hasMap")] private string _mapName = "testMap";
        [SerializeField] private bool _hideInHierarchy = true;
        [SerializeField] [HorizontalLine] [ReadOnly] private bool _isOriginalObject = true;

#pragma warning disable IDE0051
        private void Awake()
#pragma warning restore IDE0051 
        {
            if (_isOriginalObject)
            {
                gameObject.SetActive(false);
                OnSceneObjectRegister += OnNeutronRegister;
                gameObject.hideFlags = _hideInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;
            }
        }

#pragma warning disable IDE0051
        private void Start()
#pragma warning restore IDE0051 
        {
            if (!_isOriginalObject)
                OnSceneObjectRegister -= OnNeutronRegister;
        }

#pragma warning disable IDE0051
        private void Reset()
#pragma warning restore IDE0051
        {
#if UNITY_EDITOR
            _neutronView = transform.root.GetComponent<NeutronView>();
#endif
        }

        public void OnNeutronRegister(NeutronPlayer player, bool isServer, Scene scene, MatchmakingMode mode, INeutronMatchmaking matchmaking, Neutron neutron)
        {
            if (!_isOriginalObject)
                return;

            #region Prevent Being Instantiated
            if (_hasMap)
            {
                if (!(mode == _matchmakingMode))
                    return;

                if (matchmaking.Get.ContainsKey(_mapKey))
                {
                    string mapName = matchmaking.Get[_mapKey].ToObject<string>();
                    if (!(mapName == _mapName))
                        return;
                    else { /*continue;*/ }
                }
                else
                    LogHelper.Info($"This scene object({_mapName} - {matchmaking.Name}) is not linked to a map, it will be instantiated in all scenes of the specified matchmaking.");
            }

            switch (_neutronView.Side)
            {
                case Side.Both:
                    break;
                case Side.Server:
                    {
                        if (!isServer)
                            return;
                        else
                            break;
                    }
                case Side.Client:
                    {
                        if (isServer)
                            return;
                        else
                            break;
                    }
                default:
                    break;
            }
            #endregion

            #region Make Owner
            NeutronPlayer owner = player;
            if (Neutron.Server.SceneObjectsOwner == OwnerMode.Server)
                owner = PlayerHelper.MakeTheServerPlayer(player.Channel, player.Room, player.Matchmaking);
            #endregion

            #region Duplicate and Register
            NeutronSceneObject sceneObject = Instantiate(gameObject).GetComponent<NeutronSceneObject>();
            sceneObject._isOriginalObject = false;
            sceneObject.gameObject.hideFlags = _hideInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;
            sceneObject._neutronView = sceneObject.transform.root.GetComponent<NeutronView>();
            if (sceneObject._neutronView != null)
            {
                if (sceneObject._neutronView.This == null) // if null, not registered.
                    sceneObject._neutronView.OnNeutronRegister(owner, isServer, RegisterMode.Scene, neutron);
                else
                    LogHelper.Error("NeutronView has been registered!");
            }
            else
                LogHelper.Error("NeutronView not found in scene object!");
            sceneObject.gameObject.SetActive(true);
            #endregion
        }
    }
}