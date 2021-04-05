using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DispatcherExtesions
{
    public static void DispatchOnMainThread(this Action n_Action)
    {
        NeutronDispatcher.m_ActionsDispatcher.SafeEnqueue(n_Action);
    }
}