public class NeutronMessageInfo
{
    private float sentClientTime;
    public float SentClientTime { get => sentClientTime; }

    public NeutronMessageInfo(float sentClientTime)
    {
        this.sentClientTime = sentClientTime;
    }
}