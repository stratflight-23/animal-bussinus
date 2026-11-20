using System;
using UnityEngine;

// A button you press by touching it with a VR hand (needs a trigger collider).
public class PokeButton : MonoBehaviour
{
    public Action onPressed;
    public float cooldown = 0.35f;

    float nextPress;
    float flashUntil;
    Renderer rend;
    Color baseColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) baseColor = rend.material.color;
    }

    void OnTriggerEnter(Collider other)
    {
        var hand = other.GetComponent<HandPoker>();
        if (hand == null) return;                 // only hands can press, not your body
        if (Time.time < nextPress) return;        // stop double presses
        nextPress = Time.time + cooldown;

        if (rend != null)
        {
            rend.material.color = Color.white;
            flashUntil = Time.time + 0.12f;
        }
        hand.Buzz();
        onPressed?.Invoke();
    }

    void Update()
    {
        if (flashUntil > 0f && Time.time >= flashUntil)
        {
            if (rend != null) rend.material.color = baseColor;
            flashUntil = 0f;
        }
    }
}
