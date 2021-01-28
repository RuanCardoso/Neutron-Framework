using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeutronCDatabase : NeutronCFunc {
    protected byte[] DBLogin (string user, string pass) {
        using (NeutronWriter writer = new NeutronWriter ()) {
            writer.WritePacket (Packet.Database);
            writer.WritePacket (Packet.Login);
            writer.Write (user);
            writer.Write (pass);
            return writer.ToArray();
        }
    }
}