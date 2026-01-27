using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SO_Building", menuName = "Scriptable Objects/SOS_Building")]
public class SOS_Building : ScriptableObject
{
    [SerializeField] private GameObject _building;
    [SerializeField] private int _startingBuildingCost;
    [SerializeField] private int _buildingCost;
    [SerializeField] private int _idIndex = -1;

    public GameObject Building { get { return _building; } }
    public int BuildingCost { get { return _buildingCost; } private set { _buildingCost = value; OnPriceChanged?.Invoke(this, _idIndex); } }
    public int IDIndex { get { return _idIndex; } set { _idIndex = value; } }

    public UnityEvent<SOS_Building, int> OnPriceChanged = new();


    public void ResetBuildingCost()
    {
        _buildingCost = _startingBuildingCost;
    }

    public void IncreasePriceOfBuilding(int pPriceIncrease = 5)
    {
        pPriceIncrease = Mathf.Abs(pPriceIncrease);
        BuildingCost += pPriceIncrease;
    }

    // May need to call this since the SOS is persistent.
    public void ResetEvents()
    {
        OnPriceChanged = new();
    }

    public void Initialize()
    {
        ResetBuildingCost();
        ResetEvents();
    }
}
