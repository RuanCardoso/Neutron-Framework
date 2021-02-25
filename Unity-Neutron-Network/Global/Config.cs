using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config : MonoBehaviour
{
    public static JsonData GetConfig { get; private set; }
    private void Awake()
    {
        LoadSettings();
    }

    public static void LoadSettings()
    {
        if (GetConfig == null)
            GetConfig = Data.LoadSettings();
    }
}