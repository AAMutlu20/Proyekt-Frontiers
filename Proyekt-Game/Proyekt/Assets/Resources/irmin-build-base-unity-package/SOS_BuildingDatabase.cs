// Original Author: Irmin Verhoeff
// Editors: -
// Description: This is a scriptable object script designed to hold (be a database for) the building scriptable objects. A script you can reference anywhere and get building with their respective indexes.


using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_TowerDefenceTowersDatabase", menuName = "Scriptable Objects/SOS_TowerDefenceTowersDatabase")]
public class SOS_BuildingDatabase : ScriptableObject
{
    // List of all building scriptable objects.
    [SerializeField] List<SOS_Building> _buildings = new();

    /// <summary>
    /// Get a building from the database.
    /// </summary>
    /// <param name="pIndex">Index of the building you want to get.</param>
    /// <returns>Building from the database by index.</returns>
    public SOS_Building GetBuilding(int pIndex)
    {
        return _buildings[pIndex];
    }

    /// <summary>
    /// Method to get count of buildings in the database.
    /// </summary>
    /// <returns>Count of buildings in the database</returns>
    public int GetBuildingCount()
    {
        return _buildings.Count;
    }

    // Returns if the input index is valid for the database.
    public bool IsValidBuildingIndex(int pBuildingIndex)
    {
        return pBuildingIndex > 0 && pBuildingIndex < _buildings.Count;
    }
}
