using NeutronNetwork.Internal.Components;
using System;
using System.Linq;

namespace NeutronNetwork
{
    #region With Type And Parameters
    public sealed class NeutronEventWithReturn<T, P1>
    {
        #region Delegate
        public delegate T m_Delegate(P1 p1);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke(P1 p1)
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke(p1);
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke(p1));
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventWithReturn<T, P1, P2>
    {
        #region Delegate
        public delegate T m_Delegate(P1 p1, P2 p2);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke(P1 p1, P2 p2)
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke(p1, p2);
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke(p1, p2));
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventWithReturn<T, P1, P2, P3>
    {
        #region Delegate
        public delegate T m_Delegate(P1 p1, P2 p2, P3 p3);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke(P1 p1, P2 p2, P3 p3)
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke(p1, p2, p3);
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke(p1, p2, p3));
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventWithReturn<T, P1, P2, P3, P4>
    {
        #region Delegate
        public delegate T m_Delegate(P1 p1, P2 p2, P3 p3, P4 p4);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke(P1 p1, P2 p2, P3 p3, P4 p4)
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke(p1, p2, p3, p4);
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke(p1, p2, p3, p4));
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventWithReturn<T, P1, P2, P3, P4, P5>
    {
        #region Delegate
        public delegate T m_Delegate(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke(p1, p2, p3, p4, p5);
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke(p1, p2, p3, p4, p5));
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }
    #endregion

    #region With Type And No Parameters
    public sealed class NeutronEventWithReturn<T>
    {
        #region Delegate
        public delegate T m_Delegate();
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public T Invoke()
        {
            T def = default(T);
            if (m_Event != null)
            {
                if (!DispatchOnMainThread)
                    return m_Event.Invoke();
                else NeutronDispatcher.Dispatch(() => m_Event.Invoke());
            }
            return def;
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }
    #endregion

    #region No Type And No Parameters
    public sealed class NeutronEventNoReturn
    {
        #region Delegate
        public delegate void m_Delegate();
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke()
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke();
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke());
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }
    #endregion

    #region No Type And Parameters
    public sealed class NeutronEventNoReturn<P1>
    {
        #region Delegate
        public delegate void m_Delegate(P1 p1);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke(P1 p1)
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke(p1);
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke(p1));
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventNoReturn<P1, P2>
    {
        #region Delegate
        public delegate void m_Delegate(P1 p1, P2 p2);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke(P1 p1, P2 p2)
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke(p1, p2);
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke(p1, p2));
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventNoReturn<P1, P2, P3>
    {
        #region Delegate
        public delegate void m_Delegate(P1 p1, P2 p2, P3 p3);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke(P1 p1, P2 p2, P3 p3)
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke(p1, p2, p3);
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke(p1, p2, p3));
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventNoReturn<P1, P2, P3, P4>
    {
        #region Delegate
        public delegate void m_Delegate(P1 p1, P2 p2, P3 p3, P4 p4);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke(P1 p1, P2 p2, P3 p3, P4 p4)
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke(p1, p2, p3, p4);
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke(p1, p2, p3, p4));
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }

    public sealed class NeutronEventNoReturn<P1, P2, P3, P4, P5>
    {
        #region Delegate
        public delegate void m_Delegate(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);
        event m_Delegate m_Event;
        #endregion

        #region Fields
        public bool DispatchOnMainThread;
        #endregion

        #region Default
        public void Invoke(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            if (!DispatchOnMainThread)
                m_Event?.Invoke(p1, p2, p3, p4, p5);
            else NeutronDispatcher.Dispatch(() => m_Event?.Invoke(p1, p2, p3, p4, p5));
        }

        public void Register(m_Delegate action) => m_Event += action;
        public void UnRegister(m_Delegate action) => m_Event -= action;
        #endregion

        #region Linq
        public m_Delegate[] GetMethods() => m_Event.GetInvocationList()
            .Select(x => (m_Delegate)x)
            .ToArray();
        #endregion
    }
    #endregion
}