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
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CLIENT)]
    public class SceneObject : MonoBehaviour
    {
        public static NeutronEventNoReturn<NeutronPlayer, bool, Scene, MatchmakingMode, INeutronMatchmaking, Neutron> OnSceneObjectRegister = delegate { };

        [SerializeField] [ReadOnly] private NeutronView _neutronView;
        [SerializeField] private MatchmakingMode _matchmakingMode = MatchmakingMode.Room;
        [SerializeField] private string _mapkey = "Map";
        [SerializeField] private string _mapName = "Neutron";
        [SerializeField] private bool _hideInHierarchy = true;
        [SerializeField] [ReadOnly] private bool _isOriginalObject = true;

        private void Awake()
        {
            if (_isOriginalObject)
            {
                OnSceneObjectRegister += OnNeutronRegister;
                gameObject.SetActive(false);
                gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        private void Start()
        {
            if (!_isOriginalObject)
                OnSceneObjectRegister -= OnNeutronRegister;
        }

        private void Reset()
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
            if (!(mode == _matchmakingMode))
                return;
            if (matchmaking.Get.ContainsKey(_mapkey))
            {
                string mapName = matchmaking.Get[_mapkey].ToObject<string>();
                if (!(mapName == _mapName))
                    return;
                else { /*continue;*/ }
            }
            else
                LogHelper.Info($"This scene object({_mapName}) is not linked to a map, it will be instantiated in all scenes of the specified matchmaking.");
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
            SceneObject sceneObject = Instantiate(gameObject).GetComponent<SceneObject>();
            sceneObject._isOriginalObject = false;
            sceneObject._neutronView = sceneObject.transform.root.GetComponent<NeutronView>();
            sceneObject.gameObject.SetActive(true);
            sceneObject.gameObject.hideFlags = _hideInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;
            if (sceneObject._neutronView != null)
            {
                if (sceneObject._neutronView.This == null) // if null, not registered.
                    sceneObject._neutronView.OnNeutronRegister(owner, isServer, RegisterMode.Scene, neutron);
                else
                    LogHelper.Error("Object has been registered!");
            }
            else
                LogHelper.Error("NeutronView not found in scene object!");
            #endregion
        }
    }
}