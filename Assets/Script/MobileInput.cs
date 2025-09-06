using UnityEngine;
using System;

public class MobileInput : MonoBehaviour
{
    public static event Action OnTap;

    [SerializeField] float tapCooldown = 0.08f;
    float lastTapTime = -999f;

    void Update()
    {
        bool tapped = false;

        // Touch
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) tapped = true;
        }

        // Mouse (Editor/macOS)
        if (Input.GetMouseButtonDown(0)) tapped = true;

        // Keyboard fallback (macOS test için)
        if (Input.GetKeyDown(KeyCode.Space)) tapped = true;

        if (tapped) TryTap();
    }

    void TryTap()
    {
        if (Time.time - lastTapTime < tapCooldown) return;
        lastTapTime = Time.time;
        Debug.Log("[MobileInput] TAP!");
        OnTap?.Invoke();
    }
}