using System.Collections.Generic;
using UnityEngine;

// A spot in the temple where loot appears. Tier = how deep/dangerous the spot is (better loot deeper inside):
//   1 = near the entrance (Goop Jars, Coin Piles, Toy Robots, sometimes a Golden Banana)
//   2 = middle rooms     (Coin Piles, Toy Robots, Golden Bananas, Shiny Gems)
//   3 = deep / upstairs  (Golden Bananas, Shiny Gems, Crystal Skulls)
//   4 = deepest room     (Temple Idol only)
// Loot appears when the game starts and is re-rolled every time a player walks into the temple
// through the portal (see TemplePortal). Loot you carried out of the temple is never removed.
public class LootSpawnPoint : MonoBehaviour
{
    [Range(1, 4)] public int tier = 1;

    public LootItem Current { get; private set; }

    static readonly List<LootSpawnPoint> all = new List<LootSpawnPoint>();

    static readonly LootType[][] Tables =
    {
        new LootType[0],
        new[] { LootType.GoopJar, LootType.GoopJar, LootType.OldCoinPile, LootType.OldCoinPile, LootType.LostToyRobot, LootType.GoldenBanana },
        new[] { LootType.OldCoinPile, LootType.LostToyRobot, LootType.GoldenBanana, LootType.GoldenBanana, LootType.ShinyGem },
        new[] { LootType.GoldenBanana, LootType.ShinyGem, LootType.ShinyGem, LootType.CrystalSkull },
        new[] { LootType.TempleIdol },
    };

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Start()
    {
        if (Current == null) SpawnNew();
    }

    // Removes this spot's old loot if it's still lying in the temple, then puts down a fresh one.
    public void Respawn(Bounds templeArea)
    {
        if (Current != null && !Current.IsHeld && templeArea.Contains(Current.transform.position))
            Destroy(Current.gameObject);
        SpawnNew();
    }

    void SpawnNew()
    {
        var table = Tables[Mathf.Clamp(tier, 1, 4)];
        var type = table[Random.Range(0, table.Length)];
        Current = LootItem.Create(type, transform.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
    }

    public static void RespawnAll(Bounds templeArea)
    {
        foreach (var p in all.ToArray())
            if (p != null) p.Respawn(templeArea);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = tier == 4 ? new Color(1f, 0.8f, 0.1f) : tier == 3 ? new Color(0.8f, 0.3f, 1f)
                     : tier == 2 ? new Color(0.3f, 0.8f, 1f) : new Color(0.6f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.15f, Vector3.one * 0.3f);
    }
}
