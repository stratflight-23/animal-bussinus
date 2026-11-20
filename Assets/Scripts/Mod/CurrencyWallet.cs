using System;
using UnityEngine;

// The player's coins. Saved on this device.
// When multiplayer is added, coins should live on the server so players can't cheat them.
public static class CurrencyWallet
{
    const string Key = "Coins";

    public static event Action<int> Changed;

    public static int Coins => PlayerPrefs.GetInt(Key, 0);

    public static void Add(int amount)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, Coins + amount));
        PlayerPrefs.Save();
        Changed?.Invoke(Coins);
    }

    public static bool Spend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        Add(-amount);
        return true;
    }
}
