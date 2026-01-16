using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace irminNavmeshEnemyAiUnityPackage
{
    [Serializable]
    public class NavMeshBuildSourceList
    {
        [SerializeField] private List<NavMeshBuildSource> _navMeshBuildSources = new();

        public List<NavMeshBuildSource> NavMeshBuildSources { get { return _navMeshBuildSources; } }
    }
}