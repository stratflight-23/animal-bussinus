using UnityEngine;
using UnityEngine.Rendering;

// Switches the lighting depending on where the player is. Cheap on Quest: no extra lights, it just
// changes the ambient light, fog and sun.
//   Treehouse Base = bright jungle day (sun on, light green fog far away)
//   Jungle Temple  = dark (sun off, almost no ambient light, dark fog) so only torches/crystals/lava light it
// The first area in the list is used when you're not inside any of the boxes.
public class MapAreaLighting : MonoBehaviour
{
    [System.Serializable]
    public class AreaLook
    {
        public string name = "Area";
        public Vector3 center;
        public Vector3 size = Vector3.one * 100f;
        public Color ambient = Color.grey;
        public bool sunOn = true;
        public float sunIntensity = 1.2f;
        public Color fogColor = Color.grey;
        public float fogStart = 40f;
        public float fogEnd = 160f;
        [Range(0f, 1f)] public float reflections = 1f;
    }

    public Light sun;
    public AreaLook[] areas;

    int current = -1;
    float nextCheck;

    void Start() => Apply(FindArea());

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.2f;
        int i = FindArea();
        if (i != current) Apply(i);
    }

    int FindArea()
    {
        if (areas == null || areas.Length == 0) return -1;
        var cam = Camera.main;
        Vector3 p = cam != null ? cam.transform.position : transform.position;
        for (int i = 0; i < areas.Length; i++)
            if (new Bounds(areas[i].center, areas[i].size).Contains(p)) return i;
        return 0;
    }

    void Apply(int i)
    {
        current = i;
        if (i < 0) return;
        var a = areas[i];

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = a.ambient;
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(a.ambient);
        RenderSettings.ambientProbe = sh;
        RenderSettings.reflectionIntensity = a.reflections;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = a.fogColor;
        RenderSettings.fogStartDistance = a.fogStart;
        RenderSettings.fogEndDistance = a.fogEnd;

        if (sun != null)
        {
            sun.enabled = a.sunOn;
            sun.intensity = a.sunIntensity;
        }
    }
}
