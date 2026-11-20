using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gorilla Company > 14. Build Player Model
// Builds our original low-poly gorilla and saves it as Assets/Characters/LowPolyGorilla.prefab,
// then puts it on GorillaPlayer in the open scene (replacing any older player model).
//
// "Low-poly faceted" = few, big, flat faces. The shapes (chunky balls, 6-sided tubes, rings and the C-shaped claw)
// are generated here with FLAT SHADING: every triangle gets its own normal, so each face is one flat color.
// Colors: #7aa9c2 main, #5d8aa3 and #4c7489 darker facets (shade 0.76 / 0.62 of the body color).
//
// Command line: -executeMethod GorillaModelBuilder.BuildFromCommandLine (also puts it in JungleTemple.unity)
public static class GorillaModelBuilder
{
    public const string Folder = "Assets/Characters";
    public const string PrefabPath = Folder + "/LowPolyGorilla.prefab";
    const string MeshFolder = Folder + "/Meshes";
    const string MaterialPath = Folder + "/LowPolyGorilla.mat";

    static readonly Color Steel = new Color(0x7a / 255f, 0xa9 / 255f, 0xc2 / 255f);   // #7aa9c2
    const float Mid = 0.76f;    // #5d8aa3
    const float Dark = 0.62f;   // #4c7489
    static readonly Color EyeWhite = new Color(0.92f, 0.94f, 0.95f);
    static readonly Color Pupil = new Color(0.1f, 0.13f, 0.16f);

    static Mesh sphere, tube, ring, claw, cube;
    static Material material;

    // ================================================================ menu / command line

    [MenuItem("Gorilla Company/14. Build Player Model")]
    static void BuildMenu()
    {
        var prefab = BuildPrefab();
        var player = GameObject.Find("GorillaPlayer");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Gorilla Company", "Gorilla model saved as " + PrefabPath +
                ".\n\nThis scene has no GorillaPlayer, so it wasn't added. Open JungleTemple (or GorillaTest) and run this again.", "OK");
            return;
        }
        AttachTo(player, prefab);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = player;
        EditorUtility.DisplayDialog("Gorilla Company",
            "Low-poly gorilla built and put on GorillaPlayer!\n\nSave the scene (Ctrl+S), press Play and walk up to the Wardrobe mirror.", "OK");
    }

    public static void BuildFromCommandLine()
    {
        var prefab = BuildPrefab();
        foreach (var path in new[] { "Assets/Scenes/JungleTemple.unity", "Assets/Scenes/GorillaTest.unity" })
        {
            if (!File.Exists(path)) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var player = GameObject.Find("GorillaPlayer");
            if (player == null) continue;
            AttachTo(player, prefab);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Gorilla Model] Added to " + path);
        }
    }

    // Used by the scene builders: if the model has been built, every new GorillaPlayer gets it.
    public static void AttachIfBuilt(GameObject player)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null) AttachTo(player, prefab);
    }

    public static void AttachTo(GameObject player, GameObject prefab)
    {
        // remove any older player model first
        foreach (var old in player.GetComponentsInChildren<GorillaModel>(true))
            Object.DestroyImmediate(old.gameObject);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
        model.name = "LowPolyGorilla";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
    }

    // ================================================================ building the prefab

    public static GameObject BuildPrefab()
    {
        EnsureFolder(Folder);
        EnsureFolder(MeshFolder);
        MakeMeshes();
        material = GetMaterial();

        var root = new GameObject("LowPolyGorilla");
        try
        {
            var gm = root.AddComponent<GorillaModel>();
            gm.bodyColor = Steel;
            gm.tintParts = new List<GorillaModel.TintPart>();

            // ---------------- HEAD (big, pushed forward, heavy brow, bulging eyes, boxy muzzle)
            var head = Child(root.transform, "Head", new Vector3(0f, 1.5f, 0.06f));
            gm.head = head;
            Part(gm, head, "Skull", sphere, V(0f, 0f, 0f), V(0.33f, 0.29f, 0.31f), V(0, 0, 0), 1f);
            Part(gm, head, "Crown", cube, V(0f, 0.11f, -0.04f), V(0.15f, 0.05f, 0.15f), V(10, 45, 0), 0.88f);
            Part(gm, head, "Brow Ridge", cube, V(0f, 0.065f, 0.125f), V(0.31f, 0.075f, 0.1f), V(-12, 0, 0), Mid);
            Part(gm, head, "Brow Left", cube, V(-0.09f, 0.07f, 0.13f), V(0.12f, 0.06f, 0.09f), V(-12, 0, 10), Dark);
            Part(gm, head, "Brow Right", cube, V(0.09f, 0.07f, 0.13f), V(0.12f, 0.06f, 0.09f), V(-12, 0, -10), Dark);
            foreach (var x in new[] { -0.065f, 0.065f })
            {
                Part(gm, head, "Eye", sphere, V(x, 0.01f, 0.14f), U(0.075f), V(0, 0, 0), -1f, EyeWhite);
                Part(gm, head, "Pupil", sphere, V(x, 0.01f, 0.176f), U(0.032f), V(0, 0, 0), -1f, Pupil);
            }
            Part(gm, head, "Muzzle", cube, V(0f, -0.07f, 0.125f), V(0.22f, 0.13f, 0.13f), V(8, 0, 0), Mid);
            Part(gm, head, "Muzzle Top", cube, V(0f, -0.02f, 0.15f), V(0.13f, 0.05f, 0.08f), V(20, 0, 0), 0.88f);
            Part(gm, head, "Nostril", sphere, V(-0.03f, -0.035f, 0.193f), V(0.028f, 0.016f, 0.012f), V(0, 0, 0), -1f, Pupil);
            Part(gm, head, "Nostril", sphere, V(0.03f, -0.035f, 0.193f), V(0.028f, 0.016f, 0.012f), V(0, 0, 0), -1f, Pupil);
            Part(gm, head, "Mouth", cube, V(0f, -0.105f, 0.19f), V(0.13f, 0.01f, 0.006f), V(8, 0, 0), -1f, Pupil);
            Part(gm, head, "Jaw", cube, V(0f, -0.125f, 0.05f), V(0.25f, 0.07f, 0.19f), V(5, 0, 0), 0.85f);
            Part(gm, head, "Ear", cube, V(-0.165f, 0.005f, -0.01f), V(0.045f, 0.075f, 0.05f), V(0, -20, 15), Dark);
            Part(gm, head, "Ear", cube, V(0.165f, 0.005f, -0.01f), V(0.045f, 0.075f, 0.05f), V(0, 20, -15), Dark);
            // attach points: scaled up so the cosmetics (made for a 0.24 m head) fit this bigger head
            gm.headTop = Attach(head, "Head_Top", V(0f, 0.16f, 0f), 1.33f);
            gm.face = Attach(head, "Face", V(0f, 0f, 0.16f), 1.33f);

            // ---------------- BODY (big hunched torso, wide shoulders, leaning forward, tiny leg stubs)
            var body = Child(root.transform, "Body", new Vector3(0f, 1.04f, -0.11f));
            gm.body = body;
            var lean = Child(body, "Lean", Vector3.zero);
            lean.localRotation = Quaternion.Euler(20f, 0f, 0f);    // hunched forward
            Part(gm, lean, "Torso", sphere, V(0f, 0f, 0f), V(0.56f, 0.62f, 0.43f), V(0, 0, 0), 1f);
            Part(gm, lean, "Belly", sphere, V(0f, -0.06f, 0.15f), V(0.36f, 0.4f, 0.16f), V(0, 0, 0), Mid);
            Part(gm, lean, "Back Hump", sphere, V(0f, 0.17f, -0.12f), V(0.44f, 0.3f, 0.3f), V(0, 0, 0), 0.88f);
            Part(gm, body, "Shoulders", sphere, V(0f, 0.2f, 0.04f), V(0.8f, 0.28f, 0.42f), V(0, 0, 0), 0.88f);
            Part(gm, body, "Neck", tube, V(0f, 0.24f, 0.09f), V(0.22f, 0.12f, 0.22f), V(15, 0, 0), Mid);
            Part(gm, body, "Leg Stub", tube, V(-0.13f, -0.44f, 0f), V(0.17f, 0.14f, 0.17f), V(0, 0, 0), Dark);
            Part(gm, body, "Leg Stub", tube, V(0.13f, -0.44f, 0f), V(0.17f, 0.14f, 0.17f), V(0, 0, 0), Dark);
            gm.neck = Attach(body, "Neck", V(0f, 0.32f, 0.1f), 1.5f);
            gm.back = Attach(body, "Back", V(0f, 0.05f, -0.21f), 1.5f);
            gm.chest = Attach(body, "Chest", V(0f, 0.05f, 0.24f), 1.5f);

            // ---------------- ARMS (thick, elbow rings) + C-shaped HANDS - 1.5x normal size
            gm.leftArm = BuildArm(gm, root.transform, "Left", -1f);
            gm.rightArm = BuildArm(gm, root.transform, "Right", 1f);
            gm.leftHandPoint = Attach(gm.leftArm.hand, "LeftHand", Vector3.zero, 1.5f);
            gm.rightHandPoint = Attach(gm.rightArm.hand, "RightHand", Vector3.zero, 1.5f);
            RestPose(gm);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[Gorilla Model] Saved " + PrefabPath);
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static GorillaModel.Arm BuildArm(GorillaModel gm, Transform root, string side, float s)
    {
        var arm = new GorillaModel.Arm();
        // tubes are 1 unit long (y = 0..1); GorillaModel stretches them between shoulder, elbow and wrist
        arm.upperArm = Part(gm, root, side + " Upper Arm", tube, Vector3.zero, Vector3.one, Vector3.zero, 0.88f).transform;
        arm.forearm = Part(gm, root, side + " Forearm", tube, Vector3.zero, Vector3.one, Vector3.zero, 1f).transform;
        arm.elbow = Part(gm, root, side + " Elbow Ring", ring, Vector3.zero, Vector3.one, Vector3.zero, Dark).transform;

        var hand = Child(root, side + " Hand", Vector3.zero);
        arm.hand = hand;
        // the "C": a thick ring with a gap facing forward, standing up like a claw
        Part(gm, hand, "Claw", claw, Vector3.zero, Vector3.one, V(0, 0, 90), 1f);
        Part(gm, hand, "Palm", cube, V(0f, 0f, -0.075f), V(0.1f, 0.13f, 0.08f), Vector3.zero, Mid);
        Part(gm, hand, "Claw Tip", sphere, V(0f, 0.064f, 0.064f), U(0.07f), Vector3.zero, Mid);
        Part(gm, hand, "Claw Tip", sphere, V(0f, -0.064f, 0.064f), U(0.07f), Vector3.zero, Mid);
        Part(gm, hand, "Wrist Cuff", tube, V(0f, 0f, -0.1f), V(0.15f, 0.05f, 0.15f), V(90, 0, 0), Dark);
        return arm;
    }

    // A relaxed standing pose for the prefab (in the game, GorillaModel moves everything every frame).
    static void RestPose(GorillaModel gm)
    {
        foreach (var (arm, side) in new[] { (gm.leftArm, -1f), (gm.rightArm, 1f) })
        {
            Vector3 shoulder = gm.body.TransformPoint(new Vector3(gm.shoulderOffset.x * side, gm.shoulderOffset.y, gm.shoulderOffset.z));
            Vector3 elbow = shoulder + new Vector3(0.12f * side, -0.3f, -0.05f);
            Vector3 wrist = elbow + new Vector3(0.03f * side, -0.25f, 0.2f);
            arm.hand.position = wrist + new Vector3(0f, 0f, 0.1f);
            Seg(arm.upperArm, shoulder, elbow, gm.upperArmThickness);
            Seg(arm.forearm, elbow, wrist, gm.forearmThickness);
            arm.elbow.SetPositionAndRotation(elbow, Quaternion.FromToRotation(Vector3.up, (wrist - shoulder).normalized));
        }
    }

    static void Seg(Transform t, Vector3 a, Vector3 b, float thick)
    {
        Vector3 d = b - a;
        t.SetPositionAndRotation(a, Quaternion.FromToRotation(Vector3.up, d.normalized));
        t.localScale = new Vector3(thick, d.magnitude, thick);
    }

    // ---------------------------------------------------------------- parts

    // shade > 0: tinted part (follows the body color, darker if shade < 1). shade < 0: fixed color (eyes etc.)
    static GameObject Part(GorillaModel gm, Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Vector3 rot,
                           float shade, Color fixedColor = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // cheaper on Quest
        Color c = shade > 0f ? Steel * shade : fixedColor;
        c.a = 1f;
        go.AddComponent<PrimitiveColor>().color = c;
        if (shade > 0f) gm.tintParts.Add(new GorillaModel.TintPart { renderer = r, shade = shade });
        return go;
    }

    static Transform Child(Transform parent, string name, Vector3 localPos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    static Transform Attach(Transform parent, string name, Vector3 localPos, float scale)
    {
        var t = Child(parent, name, localPos);
        t.localScale = Vector3.one * scale;
        return t;
    }

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 U(float s) => Vector3.one * s;

    // ================================================================ flat-shaded low-poly meshes

    static void MakeMeshes()
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = temp.GetComponent<MeshFilter>().sharedMesh;   // Unity's cube is already flat-shaded
        Object.DestroyImmediate(temp);

        sphere = SaveMesh(LowSphere(5, 8), "LowPoly_Ball");
        tube = SaveMesh(Prism(6), "LowPoly_Tube");
        ring = SaveMesh(Torus(0.1f, 0.035f, 8, 5, 0f, 360f), "LowPoly_ElbowRing");
        claw = SaveMesh(Torus(0.09f, 0.045f, 9, 6, 45f, 315f), "LowPoly_CClaw");
    }

    static Mesh SaveMesh(Mesh m, string name)
    {
        string path = $"{MeshFolder}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            existing.SetVertices(m.vertices);
            existing.SetNormals(m.normals);
            existing.SetTriangles(m.triangles, 0);
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            return existing;
        }
        m.name = name;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Collects triangles. Every triangle gets its own 3 vertices + one normal = flat shading.
    // "inside" is any point inside the shape near that face, used to make sure the face points outward.
    class MeshMaker
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<int> tris = new List<int>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f) { var t = b; b = c; c = t; n = -n; }
            n.Normalize();
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Tri(a, b, c, inside);
            Tri(a, c, d, inside);
        }

        public Mesh ToMesh()
        {
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    // A chunky ball, 1 unit wide.
    static Mesh LowSphere(int rings, int segments)
    {
        var mm = new MeshMaker();
        Vector3 P(int i, int j)
        {
            float theta = Mathf.PI * i / rings;
            float phi = 2f * Mathf.PI * j / segments;
            return 0.5f * new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < segments; j++)
            {
                Vector3 a = P(i, j), b = P(i, j + 1), c = P(i + 1, j + 1), d = P(i + 1, j);
                if (i == 0) mm.Tri(a, c, d, Vector3.zero);
                else if (i == rings - 1) mm.Tri(a, b, c, Vector3.zero);
                else mm.Quad(a, b, c, d, Vector3.zero);
            }
        return mm.ToMesh();
    }

    // A 6-sided tube, 1 unit wide, from y = 0 to y = 1.
    static Mesh Prism(int sides)
    {
        var mm = new MeshMaker();
        Vector3 P(int j, float y)
        {
            float a = 2f * Mathf.PI * j / sides;
            return new Vector3(0.5f * Mathf.Cos(a), y, 0.5f * Mathf.Sin(a));
        }
        var mid = new Vector3(0f, 0.5f, 0f);
        for (int j = 0; j < sides; j++)
        {
            mm.Quad(P(j, 0f), P(j + 1, 0f), P(j + 1, 1f), P(j, 1f), mid);
            mm.Tri(new Vector3(0f, 1f, 0f), P(j, 1f), P(j + 1, 1f), mid);
            mm.Tri(Vector3.zero, P(j, 0f), P(j + 1, 0f), mid);
        }
        return mm.ToMesh();
    }

    // A ring (or part of one, for the C claw) lying flat (around the Y axis). Angles in degrees, 0 = +Z.
    static Mesh Torus(float bigR, float tubeR, int segments, int sides, float fromDeg, float toDeg)
    {
        var mm = new MeshMaker();
        float from = fromDeg * Mathf.Deg2Rad, to = toDeg * Mathf.Deg2Rad;
        Vector3 Center(float u) => new Vector3(bigR * Mathf.Sin(u), 0f, bigR * Mathf.Cos(u));
        Vector3 P(float u, int k)
        {
            float w = 2f * Mathf.PI * k / sides;
            Vector3 outward = new Vector3(Mathf.Sin(u), 0f, Mathf.Cos(u));
            return Center(u) + tubeR * (Mathf.Cos(w) * outward + Mathf.Sin(w) * Vector3.up);
        }
        for (int i = 0; i < segments; i++)
        {
            float u0 = Mathf.Lerp(from, to, (float)i / segments), u1 = Mathf.Lerp(from, to, (float)(i + 1) / segments);
            Vector3 inside = Center((u0 + u1) / 2f);
            for (int k = 0; k < sides; k++)
                mm.Quad(P(u0, k), P(u1, k), P(u1, k + 1), P(u0, k + 1), inside);
        }
        if (toDeg - fromDeg < 359.9f)   // close the two ends of the C
        {
            Vector3 Tangent(float u) => new Vector3(Mathf.Cos(u), 0f, -Mathf.Sin(u));
            Vector3 startIn = Center(from) + Tangent(from) * tubeR * 0.5f;
            Vector3 endIn = Center(to) - Tangent(to) * tubeR * 0.5f;
            for (int k = 0; k < sides; k++)
            {
                mm.Tri(Center(from), P(from, k), P(from, k + 1), startIn);
                mm.Tri(Center(to), P(to, k), P(to, k + 1), endIn);
            }
        }
        return mm.ToMesh();
    }

    // ================================================================ helpers

    static Material GetMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader) { name = "LowPolyGorilla" };
        mat.SetColor("_BaseColor", Color.white);   // each part's color comes from PrimitiveColor
        mat.SetFloat("_Smoothness", 0.12f);
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
