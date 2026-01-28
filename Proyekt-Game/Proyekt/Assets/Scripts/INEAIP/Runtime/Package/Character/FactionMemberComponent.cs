using UnityEngine;

public class FactionMemberComponent : MonoBehaviour
{
    [SerializeField] private int _factionID;

    public int FactionID {  get { return _factionID; } set { _factionID = value; } }
}