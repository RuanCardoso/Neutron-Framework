using NeutronNetwork.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    /// <para>Think of it as an "exclusive player space" all players when connecting create one on the server side, this "space" is global.</para>
    /// Here, for example, you can perform functions on the player who owns this "space", for example, send a function in certain seconds.
    /// </summary>
    public class View : MonoBehaviour
    {
        [SerializeField] [ReadOnly] private Player m_Owner;
        /// <summary>
        /// The owner of this instance.
        /// </summary>
        public Player Owner { get => m_Owner; set => m_Owner = value; }

        #region MonoBehaviour
        private void Awake()
        {

        }

        private void Start()
        {

        }

        private void Update()
        {

        }
        #endregion
    }
}