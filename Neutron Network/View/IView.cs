using NeutronNetwork;
using UnityEngine;

public class View : MonoBehaviour
{
    [SerializeField] private Player m_Owner;
    public Player Owner { get => m_Owner; set => m_Owner = value; }
}