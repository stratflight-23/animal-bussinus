using System.Collections.Generic;
using UnityEngine;

// The list of every pet. Lives in Assets/Resources/GorillaCompany so the game can find it while running.
// Filled by Gorilla Company > 12. Generate Pets.
[CreateAssetMenu(menuName = "Gorilla Company/Pet Library", fileName = "PetLibrary")]
public class PetLibrary : ScriptableObject
{
    public const string ResourcePath = "GorillaCompany/PetLibrary";

    public List<PetData> all = new List<PetData>();

    static PetLibrary cached;
    public static PetLibrary Load()
    {
        if (cached == null) cached = Resources.Load<PetLibrary>(ResourcePath);
        return cached;
    }

    public PetData Find(string id)
    {
        foreach (var p in all)
            if (p != null && p.id == id) return p;
        return null;
    }
}
