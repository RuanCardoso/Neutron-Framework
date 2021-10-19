using System;
using System.Threading.Tasks;

namespace NeutronNetwork.Examples
{
    public class ServerController : ServerSide
    {
        private Random _random = new Random();
#if UNITY_SERVER || UNITY_EDITOR
        protected async override Task<bool> OnAuthentication(NeutronPlayer player, Authentication authentication)
        {
            string user = authentication.User;
            string pass = authentication.Pass;
            bool authStatus = user == "Neutron" && pass == "Neutron";
            player.Nickname = "Teste de nome";
            player.Properties = "{\"Team\":\"Red\"}";
            return OnAuth(player, authStatus);
        }

        protected override void OnPlayerJoinedRoom(NeutronPlayer player, NeutronRoom room)
        {
            LogHelper.Error(room.PhysicsManager.name);
        }

        private string GenerateName(int len)
        {
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[_random.Next(consonants.Length)].ToUpper();
            Name += vowels[_random.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[_random.Next(consonants.Length)];
                b++;
                Name += vowels[_random.Next(vowels.Length)];
                b++;
            }
            return Name;
        }
    }
#endif
}