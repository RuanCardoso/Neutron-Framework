﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeutronConfig : MonoBehaviour
{
    public static JsonData GetConfig { get; private set; }
    private void Awake()
    {
        LoadSettings();
    }

    public static void LoadSettings()
    {
        if (GetConfig == null)
            GetConfig = NeutronData.LoadSettings();
    }
}