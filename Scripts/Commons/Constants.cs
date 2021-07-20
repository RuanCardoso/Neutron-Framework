namespace NeutronNetwork.Constants
{
    public static class NeutronConstants
    {
        #region String's
        public const string CONTAINER_PLAYER_NAME = "[Container] -> Player[Main]";
        #endregion

        #region Integers
        public const int NEUTRON_EVENT_WITH_RETURN_DELAY = 10;
        public const int GENERATE_PLAYER_ID = 27716848;
        public const int BOUNDED_CAPACITY = int.MaxValue;
        public const int MIN_SEND_RATE = 1;
        public const int MAX_SEND_RATE = 128;
        public const int MAX_LATENCY = 150;
        #endregion

        #region Single's
        public const float ONE_PER_SECOND = 1F;
        #endregion

        #region Double's
        public const double NETWORK_TIME_DESYNCHRONIZATION_TOLERANCE = 1D;
        public const double RESYNCHRONIZATION_TOLERANCE = 0.001D;
        #endregion

        #region Byte's
        public const byte CREATE_PLAYER = 255;
        public const byte CREATE_OBJECT = 254;
        #endregion
    }

    public class ExecutionOrder
    {
        public const int NEUTRON_CONFIG = -1100;
        public const int NEUTRON_DISPATCHER = -1000;
        public const int NEUTRON_EVENTS = -900;
        public const int NEUTRON_SERVER = -800;
        public const int NEUTRON_CLIENT = -700;
        public const int NEUTRON_VIEW = -600;
        public const int NEUTRON_BEHAVIOUR = -500;
        public const int NEUTRON_CONNECTION = -400;
    }
}