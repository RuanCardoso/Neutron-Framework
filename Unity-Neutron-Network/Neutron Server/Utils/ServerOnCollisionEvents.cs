using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class ServerOnCollisionEvents : MonoBehaviour {
    private ServerView StatePlayer;
    public static event SEvents.OnPlayerCollision onPlayerCollision;
    public static event SEvents.OnPlayerTrigger onPlayerTrigger;
    [SerializeField] private string objectIdentifier;

    private void Start () {
        if (TryGetComponent (out ServerView state)) StatePlayer = state;
        else StatePlayer = GetComponentInParent<ServerView> ();
        //-------------------------------------------------------------------
        if (StatePlayer == null) Destroy (this);
    }

    private void OnCollisionEnter (Collision collision) {
        if (StatePlayer == null) return;
        onPlayerCollision (StatePlayer.player, collision, objectIdentifier);
    }

    private void OnTriggerEnter (Collider other) {
        if (StatePlayer == null) return;
        onPlayerTrigger (StatePlayer.player, other, objectIdentifier);
    }
}