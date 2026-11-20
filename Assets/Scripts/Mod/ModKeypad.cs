using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The in-game Mod Panel. Put this on an empty object; it builds its own buttons when the game starts.
// Enter the code to unlock mod powers (fly, spawn, coins). Also works with the keyboard for testing:
// number keys, Enter = OK, Backspace = clear.
public class ModKeypad : MonoBehaviour
{
    [Tooltip("The secret code that unlocks mod powers.")]
    [SerializeField] string modCode = "0927";
    public int maxWrongTries = 5;
    public float lockoutSeconds = 30f;

    TextMeshPro display;
    string typed = "";
    int wrongTries;
    float lockedUntil;
    float messageUntil;
    bool showingMessage;
    ModPowers mod;

    readonly List<GameObject> keypadButtons = new List<GameObject>();
    readonly List<GameObject> modButtons = new List<GameObject>();

    static readonly Key[] digitKeys =
        { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
    static readonly Key[] numpadKeys =
        { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

    static readonly Color Grey = new Color(0.35f, 0.35f, 0.4f);
    static readonly Color Green = new Color(0.2f, 0.7f, 0.3f);
    static readonly Color Red = new Color(0.75f, 0.2f, 0.2f);
    static readonly Color Gold = new Color(0.9f, 0.7f, 0.15f);
    static readonly Color Blue = new Color(0.2f, 0.45f, 0.85f);

    void Start()
    {
        mod = ModPowers.Instance != null ? ModPowers.Instance : FindAnyObjectByType<ModPowers>();
        EnsureHandPokers();
        BuildPanel();
        ShowKeypad(true);
        RefreshDisplay();
        CurrencyWallet.Changed += OnCoinsChanged;
    }

    void OnDestroy()
    {
        CurrencyWallet.Changed -= OnCoinsChanged;
    }

    void OnCoinsChanged(int coins) => RefreshDisplay();

    void Update()
    {
        if (showingMessage && Time.time >= messageUntil)
        {
            showingMessage = false;
            RefreshDisplay();
        }

        // Keyboard entry for testing on a flat screen
        var kb = Keyboard.current;
        if (kb == null || (mod != null && mod.IsMod)) return;
        for (int d = 0; d < 10; d++)
            if (kb[digitKeys[d]].wasPressedThisFrame || kb[numpadKeys[d]].wasPressedThisFrame)
                OnKey(d.ToString());
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) OnKey("OK");
        if (kb.backspaceKey.wasPressedThisFrame) OnKey("C");
    }

    // ------------------------------------------------------------ keypad logic

    void OnKey(string key)
    {
        if (Time.time < lockedUntil)
        {
            Flash("LOCKED " + Mathf.CeilToInt(lockedUntil - Time.time) + "s");
            return;
        }

        if (key == "C") typed = "";
        else if (key == "OK") { Submit(); return; }
        else if (typed.Length < 8) typed += key;
        RefreshDisplay();
    }

    void Submit()
    {
        if (typed == modCode)
        {
            typed = "";
            wrongTries = 0;
            if (mod == null)
            {
                Flash("NO ModPowers ON PLAYER");
                return;
            }
            mod.Unlock();
            ShowKeypad(false);
            Flash("MOD UNLOCKED");
        }
        else
        {
            typed = "";
            wrongTries++;
            if (wrongTries >= maxWrongTries)
            {
                wrongTries = 0;
                lockedUntil = Time.time + lockoutSeconds;
                Flash("TOO MANY TRIES");
            }
            else
            {
                Flash("WRONG CODE");
            }
        }
    }

    // ------------------------------------------------------------ mod menu actions

    void OnFly()
    {
        mod.ToggleFly();
        Flash(mod.IsFlying ? "FLY ON" : "FLY OFF");
    }

    void OnSpawn()
    {
        var obj = mod.Spawn();
        Flash(obj != null ? "SPAWNED " + mod.CurrentSpawnName : "NOTHING TO SPAWN");
    }

    void OnNext()
    {
        mod.NextSpawn();
        Flash("NEXT: " + mod.CurrentSpawnName);
    }

    void OnCoins()
    {
        mod.GiveCoins(mod.coinButtonAmount);
        Flash("+" + mod.coinButtonAmount + " COINS");
    }

    void OnLock()
    {
        mod.Lock();
        ShowKeypad(true);
        Flash("MOD LOCKED");
    }

    // ------------------------------------------------------------ display

    void Flash(string message)
    {
        display.text = message;
        messageUntil = Time.time + 1.2f;
        showingMessage = true;
    }

    void RefreshDisplay()
    {
        if (display == null || showingMessage) return;

        if (mod != null && mod.IsMod)
            display.text = "MOD  |  " + CurrencyWallet.Coins + " coins\n<size=60%>Spawn: " + mod.CurrentSpawnName + "</size>";
        else
            display.text = typed.Length == 0 ? "ENTER CODE" : new string('*', typed.Length);
    }

    void ShowKeypad(bool show)
    {
        foreach (var b in keypadButtons) b.SetActive(show);
        foreach (var b in modButtons) b.SetActive(!show);
        RefreshDisplay();
    }

    // ------------------------------------------------------------ building the panel

    void EnsureHandPokers()
    {
        foreach (var handName in new[] { "Left Hand", "Right Hand" })
        {
            var hand = GameObject.Find(handName);
            if (hand != null && hand.GetComponent<HandPoker>() == null)
                hand.AddComponent<HandPoker>();
        }
    }

    void BuildPanel()
    {
        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Backing";
        back.transform.SetParent(transform, false);
        back.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        back.transform.localScale = new Vector3(0.42f, 0.62f, 0.02f);
        SetColor(back, new Color(0.12f, 0.12f, 0.15f));

        display = MakeText("Display", transform, new Vector3(0f, 0.24f, -0.01f), 0.45f, new Vector2(0.4f, 0.1f));

        // Number pad
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" };
        for (int i = 0; i < keys.Length; i++)
        {
            string k = keys[i];
            int row = i / 3, col = i % 3;
            var pos = new Vector3((col - 1) * 0.1f, 0.12f - row * 0.1f, 0f);
            Color c = k == "OK" ? Green : k == "C" ? Red : Grey;
            keypadButtons.Add(MakeButton(k, pos, new Vector2(0.08f, 0.08f), c, () => OnKey(k)));
        }

        // Mod menu (shown after unlocking)
        var wide = new Vector2(0.17f, 0.08f);
        modButtons.Add(MakeButton("FLY", new Vector3(-0.095f, 0.12f, 0f), wide, Blue, OnFly));
        modButtons.Add(MakeButton("SPAWN", new Vector3(0.095f, 0.12f, 0f), wide, Green, OnSpawn));
        modButtons.Add(MakeButton("NEXT", new Vector3(-0.095f, 0.02f, 0f), wide, Grey, OnNext));
        modButtons.Add(MakeButton("+COINS", new Vector3(0.095f, 0.02f, 0f), wide, Gold, OnCoins));
        modButtons.Add(MakeButton("LOCK", new Vector3(0f, -0.14f, 0f), wide, Red, OnLock));
    }

    GameObject MakeButton(string label, Vector3 localPos, Vector2 size, Color color, System.Action onPress)
    {
        var root = new GameObject("Button " + label);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPos;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Press";
        cube.transform.SetParent(root.transform, false);
        cube.transform.localScale = new Vector3(size.x, size.y, 0.03f);
        cube.GetComponent<Collider>().isTrigger = true;
        SetColor(cube, color);
        cube.AddComponent<PokeButton>().onPressed = onPress;

        var text = MakeText("Label", root.transform, new Vector3(0f, 0f, -0.02f), 0.28f, size);
        text.text = label;
        return root;
    }

    TextMeshPro MakeText(string objName, Transform parent, Vector3 localPos, float fontSize, Vector2 size)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = size;
        tmp.text = "";
        return tmp;
    }

    static void SetColor(GameObject go, Color color)
    {
        go.GetComponent<Renderer>().material.color = color;
    }
}
