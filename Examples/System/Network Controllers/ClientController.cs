using Newtonsoft.Json.Linq;

namespace NeutronNetwork.Examples
{
    public class ClientController : ClientSide
    {
        protected override bool AutoStartConnection => false;

        protected override void Start()
        {
            base.Start();
            {
                UILogic.OnAuthentication += Connect;
            }
        }

        protected override void OnNeutronConnected(bool isSuccess, Neutron neutron)
        {
            base.OnNeutronConnected(isSuccess, neutron);
            {
                if (isSuccess)
                    LogHelper.Info("Neutron connected with successful.");
                else
                    LogHelper.Error("Neutron connection failed!");
            }
        }

        protected override void OnNeutronAuthenticated(bool isSuccess, JObject properties, Neutron neutron)
        {
            base.OnNeutronAuthenticated(isSuccess, properties, neutron);
            {
                if (isSuccess)
                    LogHelper.Info("Authenticated with successful.");
            }
        }

        protected override void OnPlayerConnected(NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnPlayerConnected(player, isMine, neutron);
            {
                if (isMine)
                    LogHelper.Info("The player is ready to use!");
            }
        }

        public void Connect(string user, string pass) => Connect(authentication: new Authentication(user, pass));
    }
}