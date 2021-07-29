using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    public class View : MonoBehaviour
    {
        [SerializeField] private NeutronPlayer _owner;
        /// <summary>
        ///* O Jogador dono deste objeto, é pra ele que você vai enviar as paradas.
        /// </summary>
        public NeutronPlayer Owner { get => _owner; set => _owner = value; }

        public virtual void Awake()
        {

        }

        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }
    }
}