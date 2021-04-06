using System;
using System.Collections.Generic;
using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

#region Channel
[Serializable]
public class ChannelDictionary : NeutronSafeDictionary<int, Channel>, ISerializationCallbackReceiver
{
    [SerializeField] private List<ChannelValue> m_list = new List<ChannelValue>();

    public void OnAfterDeserialize() => AddToDict();
    public void OnBeforeSerialize() { }

    private void AddToDict()
    {
        base.Clear();
        for (int i = 0; i < m_list.Count; i++)
        {
            m_list[i].m_key = m_list[i].m_value.ID;
            if (!base.ContainsKey(m_list[i].m_key))
                base.TryAdd(m_list[i].m_key, m_list[i].m_value);
            else return;
        }
    }

    public new bool TryAdd(int key, Channel value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryAdd(key, value)))
#if UNITY_EDITOR
            m_list.Add(new ChannelValue(key, value));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new bool TryRemove(int key, out Channel value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryRemove(key, out value)))
#if UNITY_EDITOR
            m_list.Remove(new ChannelValue(key));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new void Clear()
    {
        base.Clear();
#if UNITY_EDITOR
        m_list.Clear();
#endif
    }

    [Serializable]
    class ChannelValue : IEquatable<ChannelValue>
    {
        [ReadOnly] public int m_key;
        public Channel m_value;

        public ChannelValue(int key)
        {
            m_key = key;
        }

        public ChannelValue(int key, Channel value)
        {
            m_key = key;
            m_value = value;
        }

        public bool Equals(ChannelValue other)
        {
            return this.m_key == other.m_key;
        }
    }
}
#endregion

#region Room
[Serializable]
public class RoomDictionary : NeutronSafeDictionary<int, Room>, ISerializationCallbackReceiver
{
    [SerializeField] private List<RoomValue> m_list = new List<RoomValue>();

    public void OnAfterDeserialize() => AddToDict();
    public void OnBeforeSerialize() { }

    private void AddToDict()
    {
        base.Clear();
        for (int i = 0; i < m_list.Count; i++)
        {
            m_list[i].m_key = m_list[i].m_value.ID;
            if (!base.ContainsKey(m_list[i].m_key))
                base.TryAdd(m_list[i].m_key, m_list[i].m_value);
            else return;
        }
    }

    public new bool TryAdd(int key, Room value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryAdd(key, value)))
#if UNITY_EDITOR
            m_list.Add(new RoomValue(key, value));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new bool TryRemove(int key, out Room value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryRemove(key, out value)))
#if UNITY_EDITOR
            m_list.Remove(new RoomValue(key));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new void Clear()
    {
        base.Clear();
#if UNITY_EDITOR
        m_list.Clear();
#endif
    }

    [Serializable]
    class RoomValue : IEquatable<RoomValue>
    {
        [ReadOnly] public int m_key;
        public Room m_value;

        public RoomValue(int key)
        {
            m_key = key;
        }

        public RoomValue(int key, Room value)
        {
            m_key = key;
            m_value = value;
        }

        public bool Equals(RoomValue other)
        {
            return this.m_key == other.m_key;
        }
    }
}
#endregion

#region Player
[Serializable]
public class PlayerDictionary : NeutronSafeDictionary<int, Player>, ISerializationCallbackReceiver
{
    [SerializeField] private List<PlayerValue> m_list = new List<PlayerValue>();

    public void OnAfterDeserialize() => AddToDict();
    public void OnBeforeSerialize() { }

    private void AddToDict()
    {
        base.Clear();
        for (int i = 0; i < m_list.Count; i++)
        {
            m_list[i].m_key = m_list[i].m_value.ID;
            if (!base.ContainsKey(m_list[i].m_key))
                base.TryAdd(m_list[i].m_key, m_list[i].m_value);
            else return;
        }
    }

    public new bool TryAdd(int key, Player value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryAdd(key, value)))
#if UNITY_EDITOR
            m_list.Add(new PlayerValue(key, value));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new bool TryRemove(int key, out Player value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryRemove(key, out value)))
#if UNITY_EDITOR
            m_list.Remove(new PlayerValue(key));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new void Clear()
    {
        base.Clear();
#if UNITY_EDITOR
        m_list.Clear();
#endif
    }

    [Serializable]
    class PlayerValue : IEquatable<PlayerValue>
    {
        [ReadOnly] public int m_key;
        public Player m_value;

        public PlayerValue(int key)
        {
            m_key = key;
        }

        public PlayerValue(int key, Player value)
        {
            m_key = key;
            m_value = value;
        }

        public bool Equals(PlayerValue other)
        {
            return this.m_key == other.m_key;
        }
    }
}
#endregion