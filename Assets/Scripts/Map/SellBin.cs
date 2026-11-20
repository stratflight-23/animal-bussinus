using UnityEngine;

// The Sell Bin in the Treehouse Base. Put this on a trigger collider inside the crate.
// Drop loot in (let go of it over the glowing slot) and you get its coin value as Goop Coins.
//
// MULTIPLAYER NOTE: coins are saved on this device for now (CurrencyWallet). With multiplayer, the SERVER
// must check the loot is real and pay out the coins, so hacked games can't give themselves money.
[RequireComponent(typeof(Collider))]
public class SellBin : MonoBehaviour
{
    [Tooltip("Where the \"+50\" pops up. Leave empty to use just above the bin.")]
    public Transform popupPoint;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other) => TrySell(other);
    void OnTriggerStay(Collider other) => TrySell(other);

    void TrySell(Collider other)
    {
        var loot = other.GetComponentInParent<LootItem>();
        if (loot == null || loot.Sold || loot.IsHeld) return;   // you have to let go of it first

        loot.MarkSold();
        CurrencyWallet.Add(loot.coinValue);

        Vector3 p = popupPoint != null ? popupPoint.position : transform.position + Vector3.up;
        FloatingText.Spawn("+" + loot.coinValue + " " + loot.lootName, p, new Color(1f, 0.85f, 0.2f), 1.4f);
        FxSpawner.Burst(transform.position, new Color(1f, 0.85f, 0.2f), 10, 2.5f, 0.05f);
        SoundFx.Play(Sfx.Chime, transform.position, 0.7f);
        Destroy(loot.gameObject);
    }
}
