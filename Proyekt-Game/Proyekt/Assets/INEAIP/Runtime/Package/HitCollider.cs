// Author: Irmin Verhoeff
// Description: This was designed with bullets in mind (hence penatration damage) It damages things it hits and then takes damage


using irminNavmeshEnemyAiUnityPackage;
using UnityEngine;

public class HitCollider : MonoBehaviour
{
    [SerializeField] FactionMemberComponent _factionMemberComponent;

    [SerializeField] bool _irminHitColliderActive = true;
    [SerializeField] bool _useFaction = true;
    [SerializeField] int _faction = -1;
    [SerializeField] private bool _interactingBusy = false;

    [SerializeField] LayerMask _targetLayermask = -1;

    [SerializeField] float _damage = 1;
    [SerializeField] float _selfDamagePerHit = 1;

    [SerializeField] IrminBaseHealthSystem _baseDamagable;
    /// <summary>
    /// Connect damagable game object. This is the object the thing getting damaged is percievign as the aggressor.
    /// </summary>
    [SerializeField] GameObject _connectedDamagableGameObject;
    private IDamagable _connectedIDamagable;

    //OLD Damage system.
    //[SerializeField] float _damage = 1;
    //[SerializeField] float _healthPoints = 1;
    [SerializeField] bool _takesPenetrationDamagePerHit = true;

    // OLD damage system.
    //public float HealthPoints { get { return _healthPoints; } protected set { _healthPoints = value; HealthPointCheck(); } }

    public int Faction { get { return _faction; } set { _faction = value; } }

    private void Awake()
    {
        _baseDamagable = GetComponent<IrminBaseHealthSystem>();
        //if (_baseDamagable != null) { _baseDamagable.OnMinHealthReached += DestoyThisGameObject; }
    }
    public void ReAwaken()
    {
        // According to ChatGPT and Unity documentation this should reset all properties overriden on a prefab component or gameobject.
        // Cannot Build
        //UnityEditor.PrefabUtility.RevertObjectOverride(this, UnityEditor.InteractionMode.AutomatedAction);

        _baseDamagable = GetComponent<IrminBaseHealthSystem>();
        //if (_baseDamagable != null) { _baseDamagable.OnMinHealthReached += DestoyThisGameObject; }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!_irminHitColliderActive) { return; }

        // This is how I understood it from chat gpt explanation and online foroms:
        // 1 << obj.layer creates a bitmask
        // 1 << collision.gameObject.layer creates another bitmask
        // those two can be compared wit bitwise and (&)
        // If the result is zero, the layer is not in the layermask
        if (((_targetLayermask & (1 << collision.gameObject.layer)) != 0))
        {
            Debug.Log($"Orinator: {name} collided with {collision.gameObject.name}");
            collision.gameObject.TryGetComponent<IDamagable>(out IDamagable foundDamagable);
            if (foundDamagable == null)
            {
                Debug.LogWarning($"Found an object called {collision.gameObject.name} on the enemy layer but couldn't find it's character controller.");
                return;

            }
            else
            {
                if (!_useFaction || _factionMemberComponent.FactionID >= 0 && (foundDamagable.GetFactionID()) == _faction) { return; }
                Debug.Log($"Going to damage {foundDamagable} for {_damage}");
                foundDamagable.Damage(_damage, _connectedIDamagable);
                if (_baseDamagable != null && _takesPenetrationDamagePerHit)
                {
                    _baseDamagable.Damage(_selfDamagePerHit, _connectedIDamagable);
                }
            }
        }
    }
    private void DestroyThisGameObject()
    {
        Destroy(gameObject);
    }
    public void SetFaction(int pFaction)
    {
        _faction = pFaction;
    }
    public void SetInteractionBusy(bool pInteractingBusy)
    {
        _interactingBusy = pInteractingBusy;
    }
}
