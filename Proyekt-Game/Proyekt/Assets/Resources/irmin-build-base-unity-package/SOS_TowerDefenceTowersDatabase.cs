using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_TowerDefenceTowersDatabase", menuName = "Scriptable Objects/SOS_TowerDefenceTowersDatabase")]
public class SOS_TowerDefenceTowersDatabase : ScriptableObject
{
    [SerializeField] List<SOS_Tower> _towers = new();

    public SOS_Tower GetTower(int pIndex)
    {
        return _towers[pIndex];
    }
}
