using MarkupAttributes;
using NeutronNetwork.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Current Settings", fileName = "Current Settings")]
    public class CurrentSettings : MarkupScriptable
    {
        [TabScope("Tab Scope", "Editor|Standalone|Android|Server", box: true)]
        [SerializeField]
        [Tab("./Editor")]
        [InlineEditor] private StateSettings _editor;
        [SerializeField]
        [Tab("../Standalone")]
        [InlineEditor] private StateSettings _standalone;
        [SerializeField]
        [Tab("../Android")]
        [InlineEditor] private StateSettings _android;
        [SerializeField]
        [Tab("../Server")]
        [InlineEditor] private StateSettings _server;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _editor = Resources.Load<StateSettings>("Editor - Neutron Settings");
            _server = Resources.Load<StateSettings>("Server - Neutron Settings");
            _android = Resources.Load<StateSettings>("Android - Neutron Settings");
            _standalone = Resources.Load<StateSettings>("Standalone - Neutron Settings");
        }

        private void Reset()
        {
            OnValidate();

            _editor.Reset();
            _standalone.Reset();
            _android.Reset();
            _server.Reset();
        }
#endif
    }
}