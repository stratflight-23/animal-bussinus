using System.Collections.Generic;
using UnityEngine;

// The list of every cosmetic in the game. Lives in Assets/Resources/GorillaCompany so the game can find it
// while running (Resources.Load). Filled by Gorilla Company > 10. Generate Cosmetics.
[CreateAssetMenu(menuName = "Gorilla Company/Cosmetic Library", fileName = "CosmeticLibrary")]
public class CosmeticLibrary : ScriptableObject
{
    public const string ResourcePath = "GorillaCompany/CosmeticLibrary";

    public List<CosmeticData> all = new List<CosmeticData>();

    static CosmeticLibrary cached;
    public static CosmeticLibrary Load()
    {
        if (cached == null) cached = Resources.Load<CosmeticLibrary>(ResourcePath);
        return cached;
    }

    public List<CosmeticData> ForSlot(CosmeticSlot slot)
    {
        var list = new List<CosmeticData>();
        foreach (var c in all)
            if (c != null && c.slot == slot) list.Add(c);
        return list;
    }

    public CosmeticData Find(string id)
    {
        foreach (var c in all)
            if (c != null && c.id == id) return c;
        return null;
    }
}
