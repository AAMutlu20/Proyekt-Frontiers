// Author: Irmin Verhoeff
// Editors: -
// Description: A database accessor containing singleton references to all databases.


using UnityEngine;

public class DatabaseAccessor : MonoBehaviour
{
    public static DatabaseAccessor Singleton;

    [SerializeField] private bool _isSingleton = false;
    [SerializeField] private SOS_BuildingDatabase _generalBuildingDatabase;
    [SerializeField] private SOS_EnemyDatabase _generalEnemyDatabase;

    public SOS_BuildingDatabase GeneralBuildingDatabase { get { return _generalBuildingDatabase; } }
    public SOS_EnemyDatabase GeneralEnemyDatabase { get { return _generalEnemyDatabase; } }

    private void Awake()
    {
        if (_isSingleton) { TryMakeSingleton(); }
    }

    private bool TryMakeSingleton()
    {
        if (Singleton != null && Singleton != this) { _isSingleton = false; return false; }
        else
        {
            Singleton = this;
            return true;
        }
    }
}
