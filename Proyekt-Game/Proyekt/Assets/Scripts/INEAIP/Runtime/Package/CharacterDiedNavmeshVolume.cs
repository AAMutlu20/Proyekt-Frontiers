using NUnit.Framework;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class CharacterDiedNavmeshVolume : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(AfterOneFrameRebuildNavMeshes());
        }

        IEnumerator AfterOneFrameRebuildNavMeshes()
        {
            yield return new WaitForEndOfFrame();
            if (NavMeshSurfaceSystem.Singleton != null)
            {
                foreach (NavMeshSurface navMeshSurface in NavMeshSurfaceSystem.Singleton.NavMeshSurfaces)
                {
                    navMeshSurface.BuildNavMesh();

                }
            }
        }
    }
}