using irminNavmeshEnemyAiUnityPackage;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectionSystem : MonoBehaviour
{

    [SerializeField] private List<GameObject> _gameObjectsInRange = new();

    [SerializeField] private LayerMask _layerMask;

    public UnityEvent<GameObject> OnDetectedNewGameObjectObject;
    public UnityEvent<GameObject> OnNoLongerDetectedGameObject;
    [SerializeField] Collider _collider;

    public List<GameObject> GameObjectsInRange {  get { List<GameObject> gameObjectsInRangeCopy = new(_gameObjectsInRange); return gameObjectsInRangeCopy; } }

    private void OnTriggerEnter(Collider other)
    {
        if (IrminStaticUtilities.Tools.LayerUtility.IsInLayerMask(_layerMask, other.gameObject))
        {
            _gameObjectsInRange.Add(other.gameObject);
            OnDetectedNewGameObjectObject?.Invoke(other.gameObject);
        }
        else
        {
            Debug.Log($"{other.gameObject.name} was not in layer");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (IrminStaticUtilities.Tools.LayerUtility.IsInLayerMask(_layerMask, other.gameObject))
        {
            _gameObjectsInRange.Remove(other.gameObject);
            OnNoLongerDetectedGameObject?.Invoke(other.gameObject);
        }
    }

    public float GetRadius()
    {
        SphereCollider foundSphereCollider = _collider.GetComponent<SphereCollider>();
        if(foundSphereCollider != null)
        {
            return foundSphereCollider.radius;
        }
        else
        {
            Debug.LogWarning("Tried to get radius of collider that isn't a sphere collider.");
            return 0;
        }
        
    }
}
