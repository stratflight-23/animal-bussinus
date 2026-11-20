using TMPro;
using UnityEngine;

// A sign that shows how many Goop Coins you have. Updates by itself when coins change.
public class CoinScoreboard : MonoBehaviour
{
    public TextMeshPro text;
    public string title = "GOOP COINS";

    void OnEnable()
    {
        CurrencyWallet.Changed += OnChanged;
        OnChanged(CurrencyWallet.Coins);
    }

    void OnDisable() => CurrencyWallet.Changed -= OnChanged;

    void OnChanged(int coins)
    {
        if (text != null) text.text = title + "\n<size=150%><b>" + coins + "</b></size>";
    }
}
