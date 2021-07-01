using System;

public class OnDisposed : IDisposable
{
    private Action m_OnDisposed;

    public OnDisposed(Action onDisposed)
    {
        m_OnDisposed = onDisposed;
    }

    public void Dispose()
    {
        m_OnDisposed?.Invoke();
    }
}