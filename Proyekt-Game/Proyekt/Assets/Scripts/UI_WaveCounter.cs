// Author: Irmin Verhoeff
// Editors: -
// Description: UI script to update wave counter.


using TMPro;
using UnityEngine;

public class UI_WaveCounter : MonoBehaviour
{
    [SerializeField] private bool _isSingleton;
    public static UI_WaveCounter Singleton;

    [SerializeField] TextMeshProUGUI _waveTextMeshPro;
    [SerializeField] string _divider;

    private void Awake()
    {
        if (_isSingleton) { TrySetSingleton(); }
    }

    public void SetWaveText(int _currentWaveNumber, int _wavesNumber)
    {
        _waveTextMeshPro.text = $"{_currentWaveNumber}{_divider}{_wavesNumber}";
    }

    private bool TrySetSingleton()
    {
        if(Singleton != null && Singleton != this)
        {
            Debug.Log($"{name} tried to become Singleton but {name} had already claimed the title.");
            _isSingleton = false;
            return false;
        }
        Singleton = this;
        return true;
    }
}
