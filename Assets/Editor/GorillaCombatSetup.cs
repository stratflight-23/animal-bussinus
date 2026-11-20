using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// "Gorilla Company > Extras" menu, plus helpers the other setup menus share.
public static class GorillaCombatSetup
{
    [MenuItem("Gorilla Company/Extras/Add Training Dummy")]
    static void AddDummy()
    {
        var player = EnsurePlayer();
        Vector3 pos = PlaceInFront(player, 3f);
        var dummy = TrainingDummy.Create(pos);
        FaceTowards(dummy.transform, player);
        Undo.RegisterCreatedObjectUndo(dummy, "Add Training Dummy");
        Finish(dummy, "Training Dummy added in front of the player.\n\nSave the scene (Ctrl+S), then press Play and hit it!");
    }

    [MenuItem("Gorilla Company/Extras/Add Test Ball (grabbable)")]
    static void AddBall()
    {
        var player = EnsurePlayer();
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Test Ball";
        ball.transform.position = PlaceInFront(player, 1f) + Vector3.up * 1.0f;
        ball.transform.localScale = Vector3.one * 0.2f;
        ball.AddComponent<PrimitiveColor>().SetColor(new Color(1f, 0.55f, 0.1f));
        var rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        ball.AddComponent<Grabbable>();
        Undo.RegisterCreatedObjectUndo(ball, "Add Test Ball");
        Finish(ball, "Test Ball added. In VR: touch it with a hand, hold grip to pick it up, let go while moving to throw.");
    }

    // ------------------------------------------------------------ shared helpers

    // Finds GorillaPlayer and makes sure it has Health and grabbing hands. Returns null if there's no player.
    public static GameObject EnsurePlayer()
    {
        var player = GameObject.Find("GorillaPlayer");
        if (player == null) return null;

        if (player.GetComponent<Health>() == null) Undo.AddComponent<Health>(player);
        if (player.GetComponent<StatusEffects>() == null) Undo.AddComponent<StatusEffects>(player);

        foreach (var t in player.GetComponentsInChildren<Transform>(true))
            if ((t.name == "Left Hand" || t.name == "Right Hand") && t.GetComponent<HandGrabber>() == null)
                Undo.AddComponent<HandGrabber>(t.gameObject);
        return player;
    }

    // A spot on the ground in front of the player (or the scene origin if there's no player).
    public static Vector3 PlaceInFront(GameObject player, float distance)
    {
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
        Vector3 fwd = player != null ? player.transform.forward : Vector3.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        return GroundAt(basePos + fwd.normalized * distance);
    }

    // Finds the ground height under a point (falls back to the point's own height).
    public static Vector3 GroundAt(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            return new Vector3(p.x, hit.point.y, p.z);
        return new Vector3(p.x, p.y, p.z);
    }

    public static void FaceTowards(Transform t, GameObject target)
    {
        if (target == null) return;
        Vector3 d = target.transform.position - t.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(d);
    }

    public static void Finish(GameObject select, string message)
    {
        EditorSceneManager.MarkSceneDirty(select.scene);
        Selection.activeGameObject = select;
        EditorUtility.DisplayDialog("Gorilla Company", message, "OK");
    }
}
