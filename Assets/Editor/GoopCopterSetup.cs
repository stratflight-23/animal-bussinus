using TMPro;
using UnityEditor;
using UnityEngine;

// Gorilla Company > 6. Add Goop Copter
// Builds the Goop Copter from simple shapes and places it next to the player.
public static class GoopCopterSetup
{
    static readonly Color Goop = new Color(0.24f, 0.86f, 0.35f);
    static readonly Color GoopDark = new Color(0.12f, 0.54f, 0.23f);
    static readonly Color Hoop = new Color(0.3f, 0.3f, 0.33f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.25f);
    static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
    static readonly Color Red = new Color(0.9f, 0.22f, 0.2f);
    static readonly Color Metal = new Color(0.72f, 0.75f, 0.78f);
    static readonly Color Dash = new Color(0.12f, 0.12f, 0.15f);

    const float FloorY = 0.55f;       // cabin floor height
    const float BarrelRadius = 0.75f;
    const float BarrelHeight = 0.85f;

    [MenuItem("Gorilla Company/6. Add Goop Copter")]
    static void AddCopter()
    {
        var player = GorillaCombatSetup.EnsurePlayer();
        Vector3 pos = player != null
            ? GorillaCombatSetup.GroundAt(player.transform.position + player.transform.forward * 3f + player.transform.right * 2.5f)
            : Vector3.zero;

        var copter = Build(pos);
        GorillaCombatSetup.FaceTowards(copter.transform, player);
        copter.transform.Rotate(0f, 90f, 0f); // side-on to the player, so the levers are easy to reach
        Undo.RegisterCreatedObjectUndo(copter, "Add Goop Copter");
        GorillaCombatSetup.Finish(copter,
            "Goop Copter added next to the player!\n\n" +
            "Get in: walk up to it and grab a red lever knob (grip).\n" +
            "Left stick = fly, right stick left/right = turn, right stick up/down = up/down.\n" +
            "B or Y = get out. Fuel refills slowly on the ground.\n\n" +
            "Save the scene with Ctrl+S.");
    }

    public static GameObject Build(Vector3 position)
    {
        var root = new GameObject("Goop Copter");
        root.transform.position = position;

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 80f;
        rb.linearDamping = 0.2f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var body = Empty("Body", root.transform, Vector3.zero);

        // ---- barrel: floor + ring of staves + two metal hoops
        Disc(body, "Floor", new Vector3(0f, FloorY, 0f), BarrelRadius * 2f + 0.05f, 0.08f, GoopDark, true);
        const int staves = 14;
        float staveWidth = 2f * Mathf.PI * BarrelRadius / staves + 0.02f;
        for (int i = 0; i < staves; i++)
        {
            float a = i * 360f / staves;
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            Part(body, PrimitiveType.Cube, "Stave", dir * BarrelRadius + Vector3.up * (FloorY + BarrelHeight / 2f),
                 new Vector3(staveWidth, BarrelHeight, 0.06f), new Vector3(0f, a, 0f), i % 2 == 0 ? Goop : GoopDark, true);
            foreach (float h in new[] { 0.2f, 0.7f })
                Part(body, PrimitiveType.Cube, "Hoop", dir * (BarrelRadius + 0.035f) + Vector3.up * (FloorY + h),
                     new Vector3(staveWidth, 0.05f, 0.02f), new Vector3(0f, a, 0f), Hoop, false);
        }

        // ---- seat
        Part(body, PrimitiveType.Cube, "Seat", new Vector3(0f, FloorY + 0.3f, -0.3f), new Vector3(0.5f, 0.1f, 0.4f), Vector3.zero, Red, true);
        Part(body, PrimitiveType.Cube, "Seat Back", new Vector3(0f, FloorY + 0.6f, -0.52f), new Vector3(0.5f, 0.55f, 0.08f), new Vector3(-10f, 0f, 0f), Red, true);

        // ---- roll cage that holds the top propeller high above your head
        foreach (float x in new[] { -0.84f, 0.84f })
            Post(body, "Mast", x, 0f, FloorY, 2.55f, 0.08f, Metal);
        Bar(body, "Cross Bar", -0.84f, 0.84f, 2.55f, 0f, 0.08f, Metal);
        Part(body, PrimitiveType.Sphere, "Rotor Hub", new Vector3(0f, 2.62f, 0f), Vector3.one * 0.16f, Vector3.zero, Yellow, false);

        var mainRotor = Empty("Main Rotor", body, new Vector3(0f, 2.68f, 0f));
        Part(mainRotor, PrimitiveType.Cube, "Blade", Vector3.zero, new Vector3(3.6f, 0.03f, 0.22f), Vector3.zero, Yellow, false);
        Part(mainRotor, PrimitiveType.Cube, "Blade", Vector3.zero, new Vector3(3.6f, 0.03f, 0.22f), new Vector3(0f, 90f, 0f), Orange, false);
        Part(mainRotor, PrimitiveType.Sphere, "Cap", Vector3.up * 0.04f, Vector3.one * 0.12f, Vector3.zero, Red, false);

        // ---- tail boom, fin and tail propeller
        Rod(body, "Tail Boom", -BarrelRadius, -2.6f, FloorY + 0.45f, 0.18f, GoopDark);
        Part(body, PrimitiveType.Cube, "Tail Fin", new Vector3(0f, FloorY + 0.75f, -2.55f), new Vector3(0.05f, 0.5f, 0.35f), Vector3.zero, Goop, false);
        var tailRotor = Empty("Tail Rotor", body, new Vector3(0.1f, FloorY + 0.75f, -2.6f));
        Part(tailRotor, PrimitiveType.Cube, "Blade", Vector3.zero, new Vector3(0.02f, 0.7f, 0.08f), Vector3.zero, Yellow, false);
        Part(tailRotor, PrimitiveType.Cube, "Blade", Vector3.zero, new Vector3(0.02f, 0.7f, 0.08f), new Vector3(90f, 0f, 0f), Orange, false);

        // ---- landing skids
        foreach (float x in new[] { -0.6f, 0.6f })
        {
            Rod(body, "Skid", 1.0f, -1.0f, 0.05f, 0.08f, Metal, x, true);
            Part(body, PrimitiveType.Cube, "Skid Tip", new Vector3(x, 0.1f, 1.05f), new Vector3(0.08f, 0.08f, 0.15f), new Vector3(-35f, 0f, 0f), Metal, false);
            foreach (float z in new[] { -0.5f, 0.5f })
                Post(body, "Strut", x, z, 0.05f, FloorY, 0.06f, Metal);
        }

        // ---- two hand levers (grab the red knob to get in)
        foreach (float x in new[] { -0.45f, 0.45f })
        {
            Post(body, "Lever", x, 0.25f, FloorY, FloorY + 0.95f, 0.035f, Metal);
            var knob = Part(body, PrimitiveType.Sphere, "Lever Knob", new Vector3(x, FloorY + 0.98f, 0.25f), Vector3.one * 0.12f, Vector3.zero, Red, true);
            var col = knob.GetComponent<SphereCollider>();
            col.isTrigger = true; // you reach for it; it doesn't block you
            col.radius = 1.1f;
            knob.AddComponent<GripHandle>();
        }

        // ---- dashboard in front of the seat
        var dash = Empty("Dashboard", body, new Vector3(0f, FloorY + 0.85f, 0.55f));
        dash.localRotation = Quaternion.Euler(30f, 0f, 0f);
        Part(dash, PrimitiveType.Cube, "Panel", Vector3.zero, new Vector3(0.62f, 0.3f, 0.04f), Vector3.zero, Dash, false);
        var textGO = new GameObject("Dashboard Text");
        textGO.transform.SetParent(dash, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.025f);
        var tmp = textGO.AddComponent<TextMeshPro>();
        tmp.text = "FUEL\nHEIGHT";
        tmp.fontSize = 0.6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(0.58f, 0.27f);

        // ---- seat anchor: NOT under Body, so it only turns left/right with the copter (no tilting your view)
        var seat = Empty("Pilot Seat", root.transform, new Vector3(0f, FloorY + 0.04f, -0.05f));

        var copter = root.AddComponent<GoopCopter>();
        copter.seat = seat;
        copter.body = body;
        copter.mainRotor = mainRotor;
        copter.tailRotor = tailRotor;
        copter.dashboard = tmp;
        return root;
    }

    // ------------------------------------------------------------ shape helpers

    static Transform Empty(string name, Transform parent, Vector3 localPos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Color color, bool collider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.AddComponent<PrimitiveColor>().color = color;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        else if (type == PrimitiveType.Cylinder)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<BoxCollider>();
        }
        return go;
    }

    static void Disc(Transform parent, string name, Vector3 pos, float diameter, float thickness, Color color, bool collider) =>
        Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, thickness / 2f, diameter), Vector3.zero, color, collider);

    static void Post(Transform parent, string name, float x, float z, float y0, float y1, float diameter, Color color) =>
        Part(parent, PrimitiveType.Cylinder, name, new Vector3(x, (y0 + y1) / 2f, z), new Vector3(diameter, (y1 - y0) / 2f, diameter), Vector3.zero, color, false);

    static void Bar(Transform parent, string name, float x0, float x1, float y, float z, float diameter, Color color) =>
        Part(parent, PrimitiveType.Cylinder, name, new Vector3((x0 + x1) / 2f, y, z), new Vector3(diameter, (x1 - x0) / 2f, diameter), new Vector3(0f, 0f, 90f), color, false);

    static void Rod(Transform parent, string name, float z0, float z1, float y, float diameter, Color color, float x = 0f, bool collider = false) =>
        Part(parent, PrimitiveType.Cylinder, name, new Vector3(x, y, (z0 + z1) / 2f), new Vector3(diameter, Mathf.Abs(z1 - z0) / 2f, diameter), new Vector3(90f, 0f, 0f), color, collider);
}
