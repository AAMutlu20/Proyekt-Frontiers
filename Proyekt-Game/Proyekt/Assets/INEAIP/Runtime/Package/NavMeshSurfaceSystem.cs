using NUnit.Framework;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class NavMeshSurfaceSystem : MonoBehaviour
    {
        public static NavMeshSurfaceSystem Singleton;
        [SerializeField] private bool _isSingleton;
        [SerializeField] private List<NavMeshSurface> _navMeshSurfaces = new();

        public List<NavMeshSurface> NavMeshSurfaces { get { List<NavMeshSurface> navMeshSurfacesCopy = new(_navMeshSurfaces); return navMeshSurfacesCopy; } }

        private void Awake()
        {
            if (_isSingleton) { SetSingleton(); }

        }

        private void SetSingleton()
        {
            if (Singleton != null && Singleton != this)
            {
                Debug.LogError($"Two singleton NavMeshSurfaceSystem detected. {name} cannot be singleton. Cancelling.");
                _isSingleton = false;
                return;
            }
            Singleton = this;
        }

        public void Rebake(Bounds pBoundsToRebake)
        {
            for (int i = 0; i < _navMeshSurfaces.Count; i++)
            {
                // Collect sources ONLY inside the bounds. Chat GPT told me this was needed
                //List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
                //UnityEngine.AI.NavMeshBuilder.CollectSources(
                //    null,
                //    _navMeshSurfaces[i].layerMask,
                //    _navMeshSurfaces[i].useGeometry,
                //    _navMeshSurfaces[i].defaultArea,
                //    new List<NavMeshBuildMarkup>(),
                //    sources
                //);
                //Debug.Log($"Sources: {sources.Count}");

                //AsyncOperation asyncRebake = UnityEngine.AI.NavMeshBuilder.UpdateNavMeshDataAsync(_navMeshSurfaces[i].navMeshData, _navMeshSurfaces[i].GetBuildSettings(), sources, pBoundsToRebake);
                //_navMeshSurfaces[i].UpdateNavMesh(_navMeshSurfaces[i].navMeshData, pBoundsToRebake);
                _navMeshSurfaces[i].BuildNavMesh();

            }
        }
    }
}