using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public struct NSColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public NSColor(Single r, Single g, Single b) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public NSColor(Single r, Single g, Single b, Single a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator NSColor(Color color)
        {
            return new NSColor(color.r, color.g, color.b, color.a);
        }

        public static implicit operator Color(NSColor color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }
    }
}