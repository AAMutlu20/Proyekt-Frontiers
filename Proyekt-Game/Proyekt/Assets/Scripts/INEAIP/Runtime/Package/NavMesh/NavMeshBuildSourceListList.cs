using irminNavmeshEnemyAiUnityPackage;
using System;
using UnityEngine;

[Serializable]
public class NavMeshBuildSourceListList
{
    [SerializeField] NavMeshBuildSourceList _navMeshBuildSourceList = new();

    public NavMeshBuildSourceList NavMeshBuildSourceList {  get { return _navMeshBuildSourceList; } }
}
