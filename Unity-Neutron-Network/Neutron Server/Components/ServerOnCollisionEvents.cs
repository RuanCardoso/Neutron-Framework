using NeutronNetwork;
using NeutronNetwork.Internal.Server.InternalEvents;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class ServerOnCollisionEvents : MonoBehaviour {
    private NeutronView StatePlayer;
    public static event ServerEvents.OnPlayerCollision onPlayerCollision;
    public static event ServerEvents.OnPlayerTrigger onPlayerTrigger;
    [SerializeField] private string objectIdentifier;

    private void Start () {
        if (TryGetComponent (out NeutronView state)) StatePlayer = state;
        else StatePlayer = GetComponentInParent<NeutronView> ();
        //-------------------------------------------------------------------
        if (StatePlayer == null) Destroy (this);
    }

    private void OnCollisionEnter (Collision collision) {
        if (StatePlayer == null) return;
        onPlayerCollision (StatePlayer.owner, collision, objectIdentifier);
    }

    private void OnTriggerEnter (Collider other) {
        if (StatePlayer == null) return;
        onPlayerTrigger (StatePlayer.owner, other, objectIdentifier);
    }
}