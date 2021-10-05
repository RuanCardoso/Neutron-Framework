namespace NeutronNetwork.Internal.Interfaces
{
    public interface ISnapshot
    {
        // snapshots have two timestamps:
        // -> the remote timestamp (when it was sent by the remote)
        //    used to interpolate.
        // -> the local timestamp (when we received it)
        //    used to know if the first two snapshots are old enough to start.
        //
        // IMPORTANT: the timestamp does _NOT_ need to be sent over the
        //            network. simply get it from batching.
        double remoteTimestamp { get; set; }
        double localTimestamp { get; set; }
    }
}