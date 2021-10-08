/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    /// <summary>
    ///* Fornece um controlador para cada jogador individual do servidor.<br/>
    ///* Disponível somente ao lado do servidor.<br/>
    ///* Note o uso de pré-processadores #if UNITY_SERVER || UNITY_EDITOR<br/>
    /// </summary>
    public abstract class PlayerGlobalController : GlobalBehaviour
    {
        #region Properties
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
        #endregion

        #region Mono Behaviour
        protected virtual void Update()
        {
            if (IsReady)
                OnNeutronUpdate();
        }

        protected virtual void OnNeutronUpdate() { }
        #endregion
    }
}