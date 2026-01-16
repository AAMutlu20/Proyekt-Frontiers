using UnityEngine;

[CreateAssetMenu(fileName = "SO_Tower", menuName = "Scriptable Objects/SOS_Tower")]
public class SOS_Tower : ScriptableObject
{
    [SerializeField] private GameObject _towerPrefab;

    public GameObject TowerPrefab { get { return _towerPrefab; } }
}
