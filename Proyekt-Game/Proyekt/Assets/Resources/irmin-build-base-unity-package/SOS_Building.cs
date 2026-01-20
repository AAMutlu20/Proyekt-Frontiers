using UnityEngine;

[CreateAssetMenu(fileName = "SO_Building", menuName = "Scriptable Objects/SOS_Building")]
public class SOS_Building : ScriptableObject
{
    [SerializeField] private GameObject _building;
    [SerializeField] private int _buildingCost;


    public GameObject Building { get { return _building; } }
    public int BuildingCost { get { return _buildingCost; } }
}
