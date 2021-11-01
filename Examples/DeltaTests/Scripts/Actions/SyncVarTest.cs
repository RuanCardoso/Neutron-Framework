namespace NeutronNetwork.Examples.DeltaEx
{
    public class SyncVarTest : SyncVarBehaviour
    {
        [SyncVar] public float health = 100;
        [SyncVar] public int level = 1;
    }
}