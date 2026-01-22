using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_TowerDefenceTowersDatabase", menuName = "Scriptable Objects/SOS_TowerDefenceTowersDatabase")]
public class SOS_BuildingDatabase : ScriptableObject
{
    [SerializeField] List<SOS_Building> _buildings = new();

    public SOS_Building GetTower(int pIndex)
    {
        return _buildings[pIndex];
    }

    public int GetBuildingCount()
    {
        return _buildings.Count;
    }
}
