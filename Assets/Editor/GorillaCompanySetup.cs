using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Adds a "Gorilla Company" menu to the top bar of the Unity editor.
//   1. Install VR Packages   -> installs XR Plugin Management + OpenXR
//   2. Build VR Test Scene   -> makes a level with a VR gorilla player, climbing walls and lava
public static class GorillaCompanySetup
{
    const string ScenePath = "Assets/Scenes/GorillaTest.unity";
    static AddAndRemoveRequest packageRequest;

    // ---------------------------------------------------------------- packages

    [MenuItem("Gorilla Company/1. Install VR Packages")]
    static void InstallPackages()
    {
        packageRequest = Client.AddAndRemove(new[] { "com.unity.xr.management", "com.unity.xr.openxr" }, null);
        EditorApplication.update += WatchPackages;
        Debug.Log("[Gorilla Company] Installing XR Plugin Management + OpenXR... (this can take a minute)");
    }

    static void WatchPackages()
    {
        if (packageRequest == null || !packageRequest.IsCompleted) return;
        EditorApplication.update -= WatchPackages;

        if (packageRequest.Status == StatusCode.Success)
        {
            EditorUtility.DisplayDialog("Gorilla Company",
                "VR packages installed!\n\n" +
                "Last step: Edit > Project Settings > XR Plug-in Management.\n" +
                "- Android tab: tick OpenXR, then turn on the Meta Quest feature group.\n" +
                "- Desktop tab: tick OpenXR.\n\n" +
                "Then run Gorilla Company > 2. Build VR Test Scene.", "OK");
        }
        else
        {
            Debug.LogError("[Gorilla Company] Package install failed: " + packageRequest.Error.message);
        }
        packageRequest = null;
    }

    // ---------------------------------------------------------------- scene

    [MenuItem("Gorilla Company/2. Build VR Test Scene")]
    static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // We make our own VR camera, so remove the default one
        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);

        var floorMat = LoadMat("Material_Grass");
        var wallMat = LoadMat("Material_Simple_BlueLight");
        var towerMat = LoadMat("Material_Simple_Purple");
        var lavaMat = LoadMat("Material_Simple_Red");

        // ---- Level
        MakeBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floorMat);
        MakeBox("Climb Wall North", new Vector3(0f, 3f, 10f), new Vector3(12f, 6f, 1f), wallMat);
        MakeBox("Climb Wall East", new Vector3(10f, 4f, 0f), new Vector3(1f, 8f, 12f), wallMat);
        MakeBox("Tower", new Vector3(-8f, 5f, -6f), new Vector3(3f, 10f, 3f), towerMat);
        MakeBox("Low Block", new Vector3(4f, 0.75f, -5f), new Vector3(3f, 1.5f, 3f), wallMat);

        var lava = MakeBox("Lava (Kill Zone)", new Vector3(-6f, 0.05f, 6f), new Vector3(4f, 0.1f, 4f), lavaMat);
        lava.GetComponent<Collider>().isTrigger = true;
        lava.AddComponent<KillZone>();

        // ---- Player
        var player = CreatePlayer(new Vector3(0f, 0.05f, 0f), Quaternion.identity);

        // ---- Save and make it the scene that gets built
        EditorSceneManager.SaveScene(scene, ScenePath);

        var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != ScenePath)
                buildScenes.Add(new EditorBuildSettingsScene(s.path, false)); // keep old scenes, but turned off
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Selection.activeGameObject = player;
        EditorUtility.DisplayDialog("Gorilla Company",
            "Test scene built and saved as Assets/Scenes/GorillaTest.unity.\n\n" +
            "It's now the first scene in the build.\n" +
            "Left stick = walk, A = jump, grip = grab walls, red lava = death + respawn.", "OK");
    }

    // ---------------------------------------------------------------- player

    // Builds the VR gorilla player (Rigidbody + capsule + movement + death, camera and two tracked hands).
    // Used by "2. Build VR Test Scene" and by the Jungle Temple map builder.
    public static GameObject CreatePlayer(Vector3 position, Quaternion rotation)
    {
        var player = new GameObject("GorillaPlayer");
        player.transform.SetPositionAndRotation(position, rotation);

        var rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.4f;
        capsule.radius = 0.3f;
        capsule.center = new Vector3(0f, 0.7f, 0f);
        capsule.sharedMaterial = GetNoFriction();

        player.AddComponent<gorillalocomotion>();
        player.AddComponent<PlayerDeathPhysics>();
        player.AddComponent<FloorTracking>();

        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(player.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offset.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f); // headset tracking replaces this at runtime
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        camGO.AddComponent<AudioListener>();
        AddTracking(camGO, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

        MakeHand("Left Hand", offset.transform, "LeftHand", -0.2f);
        MakeHand("Right Hand", offset.transform, "RightHand", 0.2f);

        GorillaModelBuilder.AttachIfBuilt(player);   // our low-poly gorilla, if menu 14 has been run
        return player;
    }

    // ---------------------------------------------------------------- helpers

    static GameObject MakeBox(string name, Vector3 position, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void MakeHand(string name, Transform parent, string usage, float x)
    {
        var hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = name;
        Object.DestroyImmediate(hand.GetComponent<Collider>()); // hands shouldn't bump into your own body
        hand.transform.SetParent(parent, false);
        hand.transform.localPosition = new Vector3(x, 1.1f, 0.3f);
        hand.transform.localScale = Vector3.one * 0.1f;
        AddTracking(hand,
            "<XRController>{" + usage + "}/devicePosition",
            "<XRController>{" + usage + "}/deviceRotation");
    }

    static void AddTracking(GameObject go, string positionPath, string rotationPath)
    {
        var tpd = go.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(
            new InputAction(go.name + " Position", InputActionType.Value, positionPath, expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(
            new InputAction(go.name + " Rotation", InputActionType.Value, rotationPath, expectedControlType: "Quaternion"));
    }

    static Material LoadMat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");
    }

    static PhysicsMaterial GetNoFriction()
    {
        const string path = "Assets/Materials/NoFriction.asset";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial("NoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
