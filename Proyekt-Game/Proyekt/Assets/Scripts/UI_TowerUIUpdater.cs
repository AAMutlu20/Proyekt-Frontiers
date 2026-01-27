using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_TowerUIUpdater : MonoBehaviour
{
    [SerializeField] private bool _isSingleton;
    public static UI_TowerUIUpdater Singleton;

    [SerializeField] private bool _reinitializeAllBuildingCostsOnStart = true;
    [SerializeField] private bool _updateAllPriceUIElementsOnStart = true;

    [SerializeField] private SOS_BuildingDatabase _buildingDatabase;
    [SerializeField] private List<TextMeshProUGUI> _priceTextMeshProElementsByBuildingIndex;

    [SerializeField] private string _currencyName = "Gold";

    private void Awake()
    {
        if (_isSingleton) { TrySetSingleton(); }
    }

    private void Start()
    {
        // TODO: Reinitialization call should probably not be done in the UI script.
        if(_reinitializeAllBuildingCostsOnStart)
        {
            _buildingDatabase.ReinitializeAllSOSBuildings();
        }
        if (_updateAllPriceUIElementsOnStart)
        {
            UpdatePriceUIOfAllBuildings();
        }
        for (int i = 0; i < _buildingDatabase.GetBuildingCount(); i++)
        {
            _buildingDatabase.GetBuilding(i).OnPriceChanged.AddListener(BuildingPriceChanged);
        }
    }

    private void BuildingPriceChanged(SOS_Building pBuildingRef, int pBuildingDatabaseIndex)
    {
        UpdatePriceUIOfBuilding(pBuildingDatabaseIndex);
    }

    private void UpdatePriceUIOfBuilding(int pBuildingIndex)
    {
        if (pBuildingIndex < 0 || pBuildingIndex >= _buildingDatabase.GetBuildingCount() || pBuildingIndex >= _priceTextMeshProElementsByBuildingIndex.Count) { Debug.LogWarning("Invalid Index to update price"); return; }
        _priceTextMeshProElementsByBuildingIndex[pBuildingIndex].text = $"{_buildingDatabase.GetBuilding(pBuildingIndex).BuildingCost} {_currencyName}";
    }

    private void UpdatePriceUIOfAllBuildings()
    {
        for (int i = 0; i < _buildingDatabase.GetBuildingCount(); i++)
        {
            UpdatePriceUIOfBuilding(i);
        }
    }

    private bool TrySetSingleton()
    {
        if (Singleton != null && Singleton != this)
        {
            Debug.Log($"{name} tried to become Singleton but {name} had already claimed the title.");
            _isSingleton = false;
            return false;
        }
        Singleton = this;
        return true;
    }

    // Better to do this inside the SOS itself
    //public void IncreasePriceOfBuilding(int pBuildingIndex, int pPriceIncrease = 5)
    //{
    //    pPriceIncrease = Mathf.Abs(pPriceIncrease);
    //    _buildingDatabase.GetBuilding(pBuildingIndex).BuildingCost += pPriceIncrease;
    //}
}
