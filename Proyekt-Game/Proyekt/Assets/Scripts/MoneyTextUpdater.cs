using System;
using TMPro;
using UnityEngine;

public class MoneyTextUpdater : MonoBehaviour
{

    [SerializeField] private Economy _economyRef;
    [SerializeField] private TextMeshProUGUI _coinAmountTextField;

    void Start()
    {
        _economyRef.OnCoinAmountChanged.AddListener(CoinAmountChanged);
        _coinAmountTextField.text = _economyRef.Coins.ToString();
    }

    private void CoinAmountChanged(int pCoins)
    {
        _coinAmountTextField.text = pCoins.ToString();
    }
}
