using NeutronNetwork.Internal.Packets;

namespace NeutronNetwork.Constants
{
    public class ExecutionOrder
    {
        public const int NEUTRON_CONFIG = -1100;
        public const int NEUTRON_DISPATCHER = -1000;
        public const int NEUTRON_REGISTER = -950;
        public const int NEUTRON_EVENTS = -900;
        public const int NEUTRON_SERVER = -800;
        public const int NEUTRON_CLIENT = -700;
        public const int NEUTRON_VIEW = -600;
        public const int NEUTRON_BEHAVIOUR = -500;
        public const int NEUTRON_CONNECTION = -400;
    }

    public class PacketSize
    {
        public const int AutoSync = sizeof(Packet) + sizeof(RegisterMode) + sizeof(short) + sizeof(byte);
        public const int gRPC = sizeof(byte) + sizeof(byte);
        public const int iRPC = sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(short) + sizeof(byte) + sizeof(byte);
    }

    public class NeutronConstants
    {
        public const int MAX_CAPACITY_FOR_EVENT_ARGS_IN_ACCEPT_POOL = 3;
        public const int MAX_CAPACITY_FOR_EVENT_ARGS_IN_RECEIVE_POOL = 65535;
        public const int GENERATE_PLAYER_ID = 0;
        public const int TIME_DECIMAL_PLACES = 3;
        public const int MAX_FPS = 512;
        public const float MIN_SEND_RATE = 0f;
        public const float MAX_SEND_RATE = 1f;
        public const float ONE_PER_SECOND = 1F;
    }
}

namespace NeutronNetwork
{
    public class ErrorMessage
    {
        public const int PLAYER_NOT_FOUND = 0x0000001;
        public const int MATCHMAKING_NOT_FOUND = 0x0000002;
        public const int RPC_ID_NOT_FOUND = 0x0000003;
        public const int CHANNELS_NOT_FOUND = 0x0000004;
        public const int MATCHMAKING_INDISPONIBLE = 0x0000005;
        public const int ROOMS_NOT_FOUND = 0x0000006;
        public const int FAILED_TO_JOIN_MATCHMAKING = 0x0000007;
        public const int FAILED_TO_JOIN_MATCHMAKING_WRONG_PASSWORD = 0x0000009;
        public const int IS_NULL_OR_EMPTY = 0x0000010;
        public const int FAILED_CREATE_ROOM = 0x0000011;
        public const int FAILED_LEAVE_MATCHMAKING = 0x0000012;
    }
}