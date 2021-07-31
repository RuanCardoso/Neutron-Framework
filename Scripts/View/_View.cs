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
        /// <summary>
        ///* O Jogador dono deste objeto, é pra ele que você vai enviar as paradas.
        /// </summary>
        public NeutronPlayer Player { get; set; }
        /// <summary>
        ///* Define se está pronto para o uso.
        /// </summary>
        public bool IsReady => Player != null;

        public virtual void Awake()
        {

        }

        public virtual void Start()
        {

        }

        public virtual void Update()
        {
            if (IsReady)
                name = $"{Player.Nickname} [{Player.ID}]";
        }
    }
}