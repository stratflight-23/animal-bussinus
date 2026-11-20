using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds "Gorilla Company > 3. Add Mod Panel" to the Unity menu bar.
public static class GorillaModSetup
{
    [MenuItem("Gorilla Company/3. Add Mod Panel")]
    static void AddModPanel()
    {
        var player = GameObject.Find("GorillaPlayer");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Gorilla Company",
                "Couldn't find GorillaPlayer in this scene.\n\nOpen the GorillaTest scene, or run '2. Build VR Test Scene' first.", "OK");
            return;
        }

        if (player.GetComponent<ModPowers>() == null)
            Undo.AddComponent<ModPowers>(player);

        foreach (var t in player.GetComponentsInChildren<Transform>(true))
            if ((t.name == "Left Hand" || t.name == "Right Hand") && t.GetComponent<HandPoker>() == null)
                Undo.AddComponent<HandPoker>(t.gameObject);

        var existing = Object.FindAnyObjectByType<ModKeypad>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Gorilla Company", "This scene already has a Mod Panel (now selected).", "OK");
            return;
        }

        var panel = new GameObject("Mod Panel");
        Undo.RegisterCreatedObjectUndo(panel, "Add Mod Panel");
        panel.transform.position = player.transform.position + new Vector3(0.8f, 1.1f, 1.2f);
        panel.AddComponent<ModKeypad>();

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeGameObject = panel;
        EditorUtility.DisplayDialog("Gorilla Company",
            "Mod Panel added next to the player.\n\nSave the scene with Ctrl+S, then press Play.\n" +
            "Poke the buttons with your hands, or type the code on the keyboard and press Enter.", "OK");
    }
}
