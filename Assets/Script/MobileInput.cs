using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class MobileInput : MonoBehaviour
{
    [SerializeField] bool debugLog = false; // optional debug

    // dir = -1 (left), +1 (right)
    public static event Action<int> OnSwipe;
    // ↑ yukarı swipe = jump
    public static event Action OnJump;

    [Header("Swipe Settings")]
    [SerializeField] float minSwipePixels = 40f;       // base pixel threshold
    [SerializeField] bool  useDpiScaling  = false;     // scale threshold by device DPI (~5mm)
    [SerializeField] float maxSwipeTime   = 0.8f;      // seconds
    [SerializeField] float verticalTolerance = 0.85f;  // (kullanılmıyor, geriye dönük)
    [SerializeField] float swipeCooldown  = 0.15f;     // prevent double trigger spam
    [SerializeField] float angleThresholdDeg = 35f;    // yatay/dikey için açı eşiği

    int activeFingerId = -1;
    Vector2 startPos;
    float startTime;
    bool pointerOverUIAtStart;
    float lastSwipeTime = -999f;
    bool mouseDown;
    bool mouseConsumed;

    float RequiredPixels()
    {
        float px = minSwipePixels;
        if (useDpiScaling && Screen.dpi > 0f)
        {
            float dpiPx = Screen.dpi * 0.20f; // ~5mm
            if (dpiPx > px) px = dpiPx;
        }
        return px;
    }

    bool IsHorizontalSwipe(Vector2 delta, float requiredPx)
    {
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);
        if (absX < requiredPx) return false;
        // 0° = tam yatay; açı eşik altında ise yatay say
        float angle = Mathf.Rad2Deg * Mathf.Atan2(absY, absX);
        return angle <= angleThresholdDeg;
    }

    bool IsVerticalSwipeUp(Vector2 delta, float requiredPx)
    {
        if (delta.y <= 0f) return false;               // yalnızca YUKARI
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);
        if (absY < requiredPx) return false;
        // 0° = tam dikey; X’e olan açı küçükse dikey say
        float angleFromVertical = Mathf.Rad2Deg * Mathf.Atan2(absX, absY);
        return angleFromVertical <= angleThresholdDeg;
    }

    void ClearInputBuffer()
    {
        activeFingerId = -1;
        startPos = Vector2.zero;
        startTime = 0f;
        pointerOverUIAtStart = false;
    }

    void OnEnable()
    {
        GameManager.OnPauseStateChanged += OnPauseChanged;
    }

    void OnDisable()
    {
        GameManager.OnPauseStateChanged -= OnPauseChanged;
    }

    void OnPauseChanged(bool paused)
    {
        if (paused) ClearInputBuffer(); // discard in-flight gesture
    }

    void Update()
    {
        if (GameManager.IsPaused) return; // no input while paused

        // TOUCH (iOS)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);

                if (t.phase == TouchPhase.Began && activeFingerId == -1)
                {
                    activeFingerId = t.fingerId;
                    startPos = t.position;
                    startTime = Time.unscaledTime;
                    pointerOverUIAtStart = EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                }
                else if (t.fingerId == activeFingerId)
                {
                    if (t.phase == TouchPhase.Moved)
                    {
                        if (Time.unscaledTime - startTime <= maxSwipeTime)
                        {
                            Vector2 delta = t.position - startPos;
                            float req = RequiredPixels();

                            // --- Erken yatay swipe ---
                            if (!pointerOverUIAtStart && IsHorizontalSwipe(delta, req))
                            {
                                if (Time.unscaledTime - lastSwipeTime >= swipeCooldown)
                                {
                                    int dir = delta.x > 0 ? 1 : -1;
                                    lastSwipeTime = Time.unscaledTime;
                                    OnSwipe?.Invoke(dir);
                                    if (debugLog) Debug.Log($"[MobileInput] Early touch {(dir>0?"Right":"Left")}, dx={Mathf.Abs(delta.x):0}");
                                    ClearInputBuffer();
                                }
                            }
                            // --- Erken dikey (YUKARI) swipe → Jump ---
                            else if (!pointerOverUIAtStart && IsVerticalSwipeUp(delta, req))
                            {
                                if (Time.unscaledTime - lastSwipeTime >= swipeCooldown)
                                {
                                    lastSwipeTime = Time.unscaledTime;
                                    OnJump?.Invoke();
                                    if (debugLog) Debug.Log($"[MobileInput] Early touch JUMP, dy={Mathf.Abs(delta.y):0}");
                                    ClearInputBuffer();
                                }
                            }
                        }
                    }
                    if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
                    {
                        TryEndSwipe(t.position);
                        activeFingerId = -1;
                    }
                }
            }
        }

        // MOUSE (Editor/macOS) – early detect while dragging
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            startTime = Time.unscaledTime;
            pointerOverUIAtStart = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            mouseDown = true;
            mouseConsumed = false;
        }
        if (mouseDown && Input.GetMouseButton(0) && !mouseConsumed)
        {
            Vector2 delta = (Vector2)Input.mousePosition - startPos;
            float req = RequiredPixels();

            if (!pointerOverUIAtStart && IsHorizontalSwipe(delta, req) && (Time.unscaledTime - lastSwipeTime) >= swipeCooldown)
            {
                int dir = delta.x > 0 ? 1 : -1;
                lastSwipeTime = Time.unscaledTime;
                OnSwipe?.Invoke(dir);
                if (debugLog) Debug.Log($"[MobileInput] Early mouse {(dir>0?"Right":"Left")}, dx={Mathf.Abs(delta.x):0}");
                mouseConsumed = true;
                ClearInputBuffer();
            }
            else if (!pointerOverUIAtStart && IsVerticalSwipeUp(delta, req) && (Time.unscaledTime - lastSwipeTime) >= swipeCooldown)
            {
                lastSwipeTime = Time.unscaledTime;
                OnJump?.Invoke();
                if (debugLog) Debug.Log($"[MobileInput] Early mouse JUMP, dy={Mathf.Abs(delta.y):0}");
                mouseConsumed = true;
                ClearInputBuffer();
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (!mouseConsumed)
                TryEndSwipe((Vector2)Input.mousePosition);
            mouseDown = false;
            mouseConsumed = false;
        }
    }

    void TryEndSwipe(Vector2 endPos)
    {
        if (GameManager.IsPaused) return; // never fire while paused
        if (pointerOverUIAtStart) return; // started on UI → ignore

        float dt = Time.unscaledTime - startTime;
        if (dt > maxSwipeTime) return; // too slow → ignore

        Vector2 delta = endPos - startPos;
        float req = RequiredPixels();

        bool fired = false;

        // Önce dikeyi (jump) kontrol et, sonra yatayı (lane)
        if (IsVerticalSwipeUp(delta, req) && (Time.unscaledTime - lastSwipeTime) >= swipeCooldown)
        {
            lastSwipeTime = Time.unscaledTime;
            OnJump?.Invoke();
            if (debugLog) Debug.Log($"[MobileInput] End swipe JUMP, dy={Mathf.Abs(delta.y):0}");
            fired = true;
        }
        else if (IsHorizontalSwipe(delta, req) && (Time.unscaledTime - lastSwipeTime) >= swipeCooldown)
        {
            int dir = delta.x > 0 ? 1 : -1;
            lastSwipeTime = Time.unscaledTime;
            OnSwipe?.Invoke(dir);
            if (debugLog) Debug.Log($"[MobileInput] End swipe {(dir>0?"Right":"Left")}, dx={Mathf.Abs(delta.x):0}");
            fired = true;
        }

        if (fired) ClearInputBuffer();
    }
}
