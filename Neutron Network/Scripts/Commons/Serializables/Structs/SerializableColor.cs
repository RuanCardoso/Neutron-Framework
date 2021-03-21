using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public struct SerializableColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SerializableColor(Single r, Single g, Single b) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public SerializableColor(Single r, Single g, Single b, Single a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator SerializableColor(Color color)
        {
            return new SerializableColor(color.r, color.g, color.b, color.a);
        }

        public static implicit operator Color(SerializableColor color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }
    }
}