using System.Threading.Tasks;

namespace NeutronNetwork.Examples
{
    public class ServerController : ServerSide
    {
#if UNITY_SERVER || UNITY_EDITOR
        protected async override Task<bool> OnAuthentication(NeutronPlayer player, Authentication authentication)
        {
            string user = authentication.User;
            string pass = authentication.Pass;
            return OnAuth(player, null, user == "Neutron" && pass == "Neutron");
        }
#endif
    }
}