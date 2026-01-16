using System.Runtime.CompilerServices;
using UnityEngine;

public class BuildSpot : MonoBehaviour, IHoverable
{
    [SerializeField] private bool _isHovered = false;
    [SerializeField] private GameObject _hoveredVisual;
    [SerializeField] private GameObject _noTowerBuildVisual;

    [SerializeField] private SOS_TowerDefenceTowersDatabase _towerDefenceDatabase;
    [SerializeField] private SOS_Tower _selectedTower;

    private void Start()
    {
        _hoveredVisual.SetActive(false);
    }

    public void Hover(bool pStatus)
    {
        if(pStatus)
        {
            _isHovered = true;
            _hoveredVisual.SetActive(true);
        }
        else
        {
            _isHovered = false;
            _hoveredVisual.SetActive(false);
        }
    }

    public bool IsHovered()
    {
        return _isHovered;
    }

    public void BuildTower(int pDatabaseIndex)
    {
        _noTowerBuildVisual.SetActive(false);
        _selectedTower = _towerDefenceDatabase.GetTower(pDatabaseIndex);
        Instantiate(_selectedTower.TowerPrefab, transform);

    }
}
