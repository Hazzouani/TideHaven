using UnityEngine;

/// <summary>
/// Plays the Blender fishing-rod animations through this rod's Animator.
/// Idle (and the decoration loop) play on their own; the actions are triggers:
/// Cast, Bite, Hook_Set, Reel_Fight, Release.
/// Call the public methods from your fishing system later, e.g. GetComponent&lt;RodAnimator&gt;().Bite().
/// </summary>
[RequireComponent(typeof(Animator))]
public class RodAnimator : MonoBehaviour
{
    [Tooltip("Left mouse button casts while you hold this rod.")]
    public bool castWithLeftClick = true;

    [Tooltip("Test keys while held: 1 Cast, 2 Bite, 3 Hook Set, 4 Reel Fight, 5 Release. Turn off when the real fishing system exists.")]
    public bool debugKeys = true;

    static readonly int CastId = Animator.StringToHash("Cast");
    static readonly int BiteId = Animator.StringToHash("Bite");
    static readonly int HookSetId = Animator.StringToHash("Hook_Set");
    static readonly int ReelFightId = Animator.StringToHash("Reel_Fight");
    static readonly int ReleaseId = Animator.StringToHash("Release");
    static readonly int[] All = { CastId, BiteId, HookSetId, ReelFightId, ReleaseId };

    Animator anim;
    PickupItem pickup;

    void Awake()
    {
        anim = GetComponent<Animator>();
        pickup = GetComponent<PickupItem>();
    }

    public void Cast() { Fire(CastId); }
    public void Bite() { Fire(BiteId); }
    public void HookSet() { Fire(HookSetId); }
    public void ReelFight() { Fire(ReelFightId); }
    public void Release() { Fire(ReleaseId); }

    void Fire(int id)
    {
        foreach (var t in All) anim.ResetTrigger(t); // no queued-up leftovers
        anim.SetTrigger(id);
    }

    void Update()
    {
        if (pickup != null && !pickup.IsHeld) return;

        if (castWithLeftClick && Input.GetMouseButtonDown(0)) Cast();

        if (debugKeys)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Cast();
            if (Input.GetKeyDown(KeyCode.Alpha2)) Bite();
            if (Input.GetKeyDown(KeyCode.Alpha3)) HookSet();
            if (Input.GetKeyDown(KeyCode.Alpha4)) ReelFight();
            if (Input.GetKeyDown(KeyCode.Alpha5)) Release();
        }
    }
}
