using System;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronMessageInfo
    {
        private double sentClientTime;
        public double SentClientTime { get => sentClientTime; }

        public NeutronMessageInfo(double sentClientTime)
        {
            this.sentClientTime = sentClientTime;
        }
    }
}