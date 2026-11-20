using UnityEditor;
using UnityEngine;

// Gorilla Company > 8. Add Spawn Safe Zone
// Puts a safe zone (radius 3.05 m = 10 ft) at the player's respawn point, or where GorillaPlayer starts.
// Also makes sure GorillaPlayer has Health (max health is in its Inspector) and the wrist health display.
public static class SafeZoneSetup
{
    [MenuItem("Gorilla Company/8. Add Spawn Safe Zone")]
    static void AddSafeZone()
    {
        var player = GorillaCombatSetup.EnsurePlayer();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Gorilla Company",
                "Couldn't find GorillaPlayer in this scene.\n\nOpen the GorillaTest scene, or run '2. Build VR Test Scene' first.", "OK");
            return;
        }
        if (player.GetComponent<PlayerHealthHud>() == null) Undo.AddComponent<PlayerHealthHud>(player);

        var existing = Object.FindAnyObjectByType<SpawnSafeZone>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Gorilla Company",
                "This scene already has a Spawn Safe Zone (now selected).\nChange its size with 'Radius' in the Inspector.", "OK");
            return;
        }

        var death = player.GetComponent<PlayerDeathPhysics>();
        Transform spot = death != null && death.respawnPoint != null ? death.respawnPoint : player.transform;
        Vector3 pos = GorillaCombatSetup.GroundAt(spot.position);

        var zone = new GameObject("Spawn Safe Zone");
        Undo.RegisterCreatedObjectUndo(zone, "Add Spawn Safe Zone");
        zone.transform.position = pos;
        zone.AddComponent<SpawnSafeZone>();

        GorillaCombatSetup.Finish(zone,
            "Spawn Safe Zone added " + (spot == player.transform ? "where the player starts" : "at the respawn point") + ".\n\n" +
            "You'll see a green circle in the Scene view. In Play mode there's a glowing ring and a faint dome.\n" +
            "Inside it, players can't be hurt by anything.\n\n" +
            "Change the size with 'Radius' (meters) in the Inspector. Save the scene with Ctrl+S.");
    }
}
