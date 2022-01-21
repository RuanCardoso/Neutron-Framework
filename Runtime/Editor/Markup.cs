using MarkupAttributes.Editor;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal;
using UnityEditor;

namespace NeutronNetwork.Editor
{
    [CustomEditor(typeof(MarkupBehaviour), true), CanEditMultipleObjects]
    internal class Markup : MarkedUpEditor
    { }

    [CustomEditor(typeof(MarkupScriptable), true), CanEditMultipleObjects]
    internal class Markup2 : MarkedUpEditor
    { }

    //[CustomEditor(typeof(MatchmakingBehaviour), true, isFallback = true), CanEditMultipleObjects]
    //internal class Markup3 : MarkedUpEditor
    //{ }
}