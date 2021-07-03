using NeutronNetwork.Attributes;
using NeutronNetwork.Naughty.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    ///* Todos os jogadores terão este script, ao lado do servidor.<br/>
    ///* Você basicamente pode fazer oque quiser aqui, ex: enviar os canais pro jogador a cada X Segundos.
    ///* Melhor você herdar para não perder suas implementações.
    /// </summary>
    public class View : MonoBehaviour
    {
        [SerializeField] [ReadOnly] private Player m_Owner;
        /// <summary>
        ///* O Jogador dono deste objeto, é pra ele que você vai enviar as paradas.
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