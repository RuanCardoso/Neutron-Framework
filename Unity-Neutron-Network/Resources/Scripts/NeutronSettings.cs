using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "Neutron/Settings")]
public class NeutronSettings : ScriptableObject
{
    [Header("[Server & Client]")]
    public NeutronGlobalSettings GlobalSettings;

    [Header("[Client]")]
    public NeutronClientSettings ClientSettings;

    [Header("[Server]")]
    public NeutronServerSettings ServerSettings;

    [Header("[Permissions]")]
    public NeutronPermissionsSettings PermissionsSettings;

    [Header("[Handles]")]
    public NeutronPermissionsSettings HandleSettings;

    [Header("Constants")]
    public int MAX_REC_MSG;
    public int MAX_SEND_MSG;
    public int LIMIT_OF_CONN_BY_IP;
}