// Original Author: Irmin Verhoeff
// Editors: -
// Description: UI script to update money text. I kept this seperate from economy.cs as I thought UI scripts should not have anything to do with "control" functionality. UI is purely a visual component, an output if you will. It relays information to the player.


using System;
using TMPro;
using UnityEngine;

public class UI_MoneyTextUpdater : MonoBehaviour
{

    [SerializeField] private Economy _economyRef;
    [SerializeField] private TextMeshProUGUI _coinAmountTextField;

    void Start()
    {
        // Subscribe to OnCoinAmountChanged and set text to coin amount at start.
        _economyRef.OnCoinAmountChanged.AddListener(CoinAmountChanged);
        _coinAmountTextField.text = _economyRef.Coins.ToString();
    }

    // Change text on coin amount changed.
    private void CoinAmountChanged(int pCoins)
    {
        _coinAmountTextField.text = pCoins.ToString();
    }
}
