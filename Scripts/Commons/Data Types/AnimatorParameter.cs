using NeutronNetwork.Internal.Packets;
using System;
using UnityEngine;

namespace NeutronNetwork.Editor
{
    [Serializable]
    public class AnimatorParameter : IEquatable<AnimatorParameter>
    {
        #region Fields
        [SerializeField] private string _parameterName;
        [SerializeField] private AnimatorControllerParameterType _parameterType;
        [SerializeField] private SyncOnOff _syncMode;
        #endregion

        #region Properties
        public string ParameterName { get => _parameterName; set => _parameterName = value; }
        public AnimatorControllerParameterType ParameterType { get => _parameterType; set => _parameterType = value; }
        public SyncOnOff SyncMode { get => _syncMode; set => _syncMode = value; }
        #endregion

        public AnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType, SyncOnOff syncMode)
        {
            _parameterName = parameterName;
            _syncMode = syncMode;
            _parameterType = parameterType;
        }

        public bool Equals(AnimatorParameter other)
        {
            return _parameterName == other.ParameterName && _parameterType == other._parameterType;
        }
    }
}