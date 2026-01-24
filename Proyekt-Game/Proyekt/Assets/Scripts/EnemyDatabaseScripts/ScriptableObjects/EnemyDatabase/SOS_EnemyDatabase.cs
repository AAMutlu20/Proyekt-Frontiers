using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_EnemyDatabase", menuName = "Scriptable Objects/SOS_EnemyDatabase")]
public class SOS_EnemyDatabase : ScriptableObject
{
    [SerializeField] List<SOS_Enemy> _enemies = new();

    /// <summary>
    /// Get a, enemy from the database.
    /// </summary>
    /// <param name="pIndex">Index of the enemy you want to get.</param>
    /// <returns>enemy from the database by index.</returns>
    public SOS_Enemy GetEnemy(int pIndex)
    {
        return _enemies[pIndex];
    }

    /// <summary>
    /// Method to get count of enemies in the database.
    /// </summary>
    /// <returns>Count of enemies in the database</returns>
    public int GetEnemyCount()
    {
        return _enemies.Count;
    }

    // Returns if the input index is valid for the database.
    public bool IsValidEnemyIndex(int pEnemyIndex)
    {
        return pEnemyIndex > 0 && pEnemyIndex < _enemies.Count;
    }

}
