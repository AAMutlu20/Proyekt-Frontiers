using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSelectionManager : MonoBehaviour
{
    [SerializeField] Color _deselectedColor;
    [SerializeField] Color _selectedColor;

    [SerializeField] List<Image> _imagesByBuildingDatabaseIndex = new();

    private void Start()
    {
        DeselectAllBuildingsVisual();
    }

    public void SetBuildingSelectedVisual(int pDatabaseIndex)
    {
        DeselectAllBuildingsVisual();
        _imagesByBuildingDatabaseIndex[pDatabaseIndex].color = _selectedColor;
    }

    public void DeselectAllBuildingsVisual()
    {
        foreach (Image image in _imagesByBuildingDatabaseIndex)
        {
            image.color = _deselectedColor;
        }
    }
}
