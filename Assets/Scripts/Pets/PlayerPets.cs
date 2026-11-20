using UnityEngine;

// Keeps track of YOUR pet. Added to GorillaPlayer automatically when the game starts.
// One pet out at a time; your choice is saved (PlayerPrefs) and it comes back next time you play.
//
// MULTIPLAYER NOTE: your pet choice must be synced to other players (like cosmetics, see PlayerAvatar),
// and each player's game spawns the pet next to its owner so everyone can see it.
public class PlayerPets : MonoBehaviour
{
    public static PlayerPets Local { get; private set; }
    const string SaveKey = "Pet";

    public PetData Current { get; private set; }
    PetFollower follower;

    void Awake() => Local = this;
    void OnDestroy() { if (Local == this) Local = null; }

    void Start()
    {
        string id = PlayerPrefs.GetString(SaveKey, "");
        var lib = PetLibrary.Load();
        if (id.Length > 0 && lib != null)
        {
            var saved = lib.Find(id);
            if (saved != null) Summon(saved, false);
        }
    }

    public void Summon(PetData pet, bool save = true)
    {
        if (pet == null) return;
        PutAway(false);
        Current = pet;
        follower = PetFollower.Spawn(pet, transform);
        if (save) { PlayerPrefs.SetString(SaveKey, pet.id); PlayerPrefs.Save(); }
    }

    public void PutAway(bool save = true)
    {
        if (follower != null)
        {
            FxSpawner.Burst(follower.transform.position + Vector3.up * 0.2f, Color.white, 8, 1.5f, 0.05f);
            Destroy(follower.gameObject);
        }
        follower = null;
        Current = null;
        if (save) { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); }
    }
}
