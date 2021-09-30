using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    public class PlayerActions : MonoBehaviour
    {
        /// <summary>
        ///* O Jogador a qual este View pertence.
        /// </summary>
        public NeutronPlayer Player {
            get;
            set;
        }

        /// <summary>
        ///* Define se está pronto para o uso.
        /// </summary>
        protected bool IsReady => Player != null;

        protected virtual void OnEnable()
        {
            NeutronModule.OnUpdate += OnNeutronUpdate;
        }

        protected virtual void OnDestroy()
        {
            NeutronModule.OnUpdate -= OnNeutronUpdate;
        }

        protected virtual void OnNeutronUpdate()
        {

        }
    }
}