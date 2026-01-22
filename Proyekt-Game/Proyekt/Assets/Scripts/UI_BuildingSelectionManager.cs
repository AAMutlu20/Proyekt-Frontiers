// Original Author: Irmin Verhoeff
// Editors: -
// Description: UI script to update visuals for selected building in the UI. Only handles the visual aspect of selecting buildings.


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_BuildingSelectionManager : MonoBehaviour
{
    // Selected and deselected color to set for the images.
    [SerializeField] Color _deselectedColor;
    [SerializeField] Color _selectedColor;

    /// <summary>
    /// The images to change color when selected and deselected. The index of the image should be the same as the index of the building, the image is for. This to keep using the same index to keep things tidy.
    /// </summary>
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
