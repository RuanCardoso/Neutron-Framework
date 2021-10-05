using NeutronNetwork.Naughty.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Components
{
    public class NeutronTransform : NeutronBehaviour
    {
        //public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        //{
        //    if (isMine)
        //    {
        //        var writer = stream.Writer;

        //        writer.Write();
        //    }
        //    else
        //    {
        //        var reader = stream.Reader;
        //        if (DoNotPerformTheOperationOnTheServer)
        //        {

        //        }
        //    }
        //    return OnValidateAutoSynchronization(isMine);
        //}
        //protected override bool OnValidateAutoSynchronization(bool isMine) => true;

        //private struct Data
        //{
        //    public Vector3 Position { get; }
        //    public Quaternion Rotation { get; }
        //    public Vector3 Scale { get; }

        //    public Data(Vector3 position, Quaternion rotation, Vector3 scale)
        //    {
        //        Position = position;
        //        Rotation = rotation;
        //        Scale = scale;
        //    }
        //}
    }
}