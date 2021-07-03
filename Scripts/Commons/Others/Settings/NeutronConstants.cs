namespace NeutronNetwork.Constants
{
    public static class NeutronConstants
    {
        #region Global
        public const string CONTAINER_PLAYER_NAME = "[Container] -> Player[Main]";
        public const int GENERATE_PLAYER_ID = 27716848;
        public const int NEUTRON_EVENT_WITH_RETURN_DELAY = 10;
        #endregion

        #region Comms
        public const int NEUTRON_SYNCHRONIZE_BEHAVIOUR = 1003;
        public const int CREATE_PLAYER = 1001;
        public const int CREATE_OBJECT = 1002;
        public const int NEUTRON_ANIMATOR = 1004;
        public const int NEUTRON_RIGIDBODY = 1005;
        #endregion

        #region Others
        public const float ONE_PER_SECOND = 1f;
        public const int MIN_SEND_RATE = 1;
        public const int MAX_SEND_RATE = 64;
        #endregion
    }
}