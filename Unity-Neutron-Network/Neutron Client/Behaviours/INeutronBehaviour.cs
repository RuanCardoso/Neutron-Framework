using System;
using UnityEngine;

public class NeutronBehaviour : MonoBehaviour
{
    [NonSerialized] public ClientView ClientView;
    [NonSerialized] public ServerView ServerView;
    public bool IsMine { get; set; }
    public bool IsBot { get; set; }
    public bool IsServer { get => ServerView != null; }
    public bool IsClient { get => ClientView != null; }
}