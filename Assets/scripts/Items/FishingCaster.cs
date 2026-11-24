using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Casting with the rod you are holding (put this on the player camera, next to PlayerHand).
/// - hold left mouse: the rod tilts back over your shoulder; the longer you hold, the further back (power bar fills)
/// - release: the rod swings forward and the bobber flies out in an arc at the moment of the swing
/// - a fishing line runs from the rod tip to the bobber (taut in the air, sags once landed)
/// - click again while the line is out: reel it back in
/// The rod motion is done here (no Blender animation), around the grip, so it never jiggles.
/// Lands on any collider; floats with a gentle bob when it lands on something named/layered "Water".
/// </summary>
public class FishingCaster : MonoBehaviour
{
    public enum State { Idle, Charging, WindUp, Flying, Landed, Reeling }

    [Header("References")]
    public PlayerHand hand;
    [Tooltip("Bobber prefab (no collider needed). Spawned on cast.")]
    public GameObject bobberPrefab;
    [Tooltip("Material for the fishing line.")]
    public Material lineMaterial;
    [Tooltip("Filled image that shows the cast power (optional).")]
    public Image powerFill;
    [Tooltip("Parent object of the power bar; shown only while charging.")]
    public GameObject powerBar;

    [Header("Charging")]
    [Tooltip("Seconds to go from 0 to full power.")]
    public float chargeTime = 1.2f;
    [Tooltip("Power below this still gives a small cast.")]
    public float minPower = 0.15f;

    [Header("Rod motion (around the grip)")]
    [Tooltip("How far back the rod tilts at full power (degrees).")]
    public float windBackAngle = 100f;
    [Tooltip("How quickly the rod follows while you hold the button.")]
    public float windBackFollow = 14f;
    [Tooltip("Duration of the forward swing after releasing (seconds).")]
    public float swingTime = 0.16f;
    [Tooltip("How far the rod swings past its normal position (degrees, forward/down).")]
    public float swingOvershoot = 22f;
    [Tooltip("The bobber leaves when the swing passes this angle (0 = the normal holding angle).")]
    public float releaseAtAngle = -10f;
    [Tooltip("How quickly the rod settles back to its normal position after the swing.")]
    public float settleSpeed = 7f;

    [Header("Cast")]
    public float minSpeed = 6f;
    public float maxSpeed = 17f;
    [Tooltip("Degrees the cast goes up from where you look.")]
    public float launchAngle = 22f;
    public float gravity = 9.81f;
    [Tooltip("Gives up and reels in if the bobber flies longer than this.")]
    public float maxFlightTime = 6f;
    public LayerMask landOn = ~(1 << 2); // everything except Ignore Raycast (player + held rod)

    [Header("Reel")]
    public float reelSpeed = 9f;

    [Header("Line")]
    public float lineWidth = 0.012f;
    public Color lineColor = new Color(0.92f, 0.92f, 0.88f);
    [Range(4, 64)] public int lineSegments = 28;
    [Tooltip("How much the line hangs down once the bobber has landed (fraction of its length).")]
    public float slack = 0.04f;
    [Tooltip("Extra width at the bobber end per metre of line, so a long cast stays visible.")]
    public float farWidthPerMetre = 0.0022f;

    [Header("Water bobbing")]
    public float bobHeight = 0.03f;
    public float bobSpeed = 2.2f;

    public State Current { get; private set; } = State.Idle;
    public float Power { get; private set; }
    public Transform Bobber { get { return bobber; } }
    /// <summary>Current tilt of the rod relative to its normal holding pose (negative = back).</summary>
    public float RodAngle { get { return rodAngle; } }

    Transform bobber;
    LineRenderer line;
    Vector3 velocity;
    float timer, chargeStart;
    bool onWater;
    Vector3 landedPos;
    Transform tip;
    PickupItem tipOwner;

    // rod motion
    PickupItem poseOwner;
    Vector3 holdPos;
    Quaternion holdRot;
    float rodAngle, swingFrom, swingT = -1f;
    bool launchPending;

    void Awake()
    {
        if (hand == null) hand = GetComponent<PlayerHand>();
        if (powerBar != null) powerBar.SetActive(false);
    }

    bool HoldingRod()
    {
        return hand != null && hand.Held != null && hand.Held.GetComponent<RodAnimator>() != null;
    }

    void Update()
    {
        bool holding = HoldingRod();

        // rod put away or swapped while busy: clean up
        if (Current != State.Idle && (!holding || hand.Held != tipOwner)) { ResetLine(); }
        if (poseOwner != null && (!holding || hand.Held != poseOwner)) { poseOwner = null; rodAngle = 0f; swingT = -1f; }
        if (!holding) return;

        switch (Current)
        {
            case State.Idle:
                if (Input.GetMouseButtonDown(0)) BeginCharge();
                break;
            case State.Charging:
                Power = Mathf.Clamp01((Time.time - chargeStart) / chargeTime);
                if (powerFill != null) powerFill.fillAmount = Power;
                if (Input.GetMouseButtonUp(0)) Release(Power);
                break;
            case State.Landed:
            case State.Flying:
                if (Input.GetMouseButtonDown(0)) StartReel();
                break;
        }
        Tick(Time.deltaTime);
    }

    /// <summary>Starts charging (normally: left mouse down).</summary>
    public void BeginCharge()
    {
        if (!HoldingRod()) return;
        tipOwner = hand.Held;
        tip = FindTip(tipOwner.transform);
        CapturePose();
        Current = State.Charging;
        chargeStart = Time.time;
        Power = 0f;
        swingT = -1f;
        if (powerBar != null) powerBar.SetActive(true);
        if (powerFill != null) powerFill.fillAmount = 0f;
    }

    /// <summary>For tests: sets the charge level directly (0..1) while charging.</summary>
    public void SetCharge(float power)
    {
        if (Current != State.Charging) return;
        Power = Mathf.Clamp01(power);
        chargeStart = Time.time - Power * chargeTime;
        if (powerFill != null) powerFill.fillAmount = Power;
    }

    /// <summary>Swings the rod forward and casts with the given power 0..1 (normally: left mouse up).</summary>
    public void Release(float power)
    {
        if (!HoldingRod()) return;
        if (tip == null) { tipOwner = hand.Held; tip = FindTip(tipOwner.transform); }
        CapturePose();
        Power = Mathf.Max(minPower, Mathf.Clamp01(power));
        if (powerBar != null) powerBar.SetActive(false);
        swingFrom = Mathf.Min(rodAngle, -15f);   // even a quick tap gets a small swing
        swingT = 0f;
        launchPending = true;
        Current = State.WindUp;
    }

    /// <summary>Reels the bobber back in.</summary>
    public void StartReel()
    {
        if (bobber == null) { ResetLine(); return; }
        Current = State.Reeling;
    }

    /// <summary>Advances everything by dt seconds. Public so it can be tested without the game clock.</summary>
    public void Tick(float dt)
    {
        UpdateRodMotion(dt);

        switch (Current)
        {
            case State.Flying:
            {
                timer += dt;
                Vector3 from = bobber.position;
                velocity += Vector3.down * gravity * dt;
                Vector3 to = from + velocity * dt;
                RaycastHit hit;
                if (Physics.Linecast(from, to, out hit, landOn, QueryTriggerInteraction.Ignore)) Land(hit);
                else
                {
                    bobber.position = to;
                    if (timer > maxFlightTime) StartReel();
                }
                break;
            }
            case State.Landed:
                if (onWater) bobber.position = landedPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                break;
            case State.Reeling:
            {
                Vector3 target = tip != null ? tip.position : transform.position;
                bobber.position = Vector3.MoveTowards(bobber.position, target, reelSpeed * dt);
                if ((bobber.position - target).sqrMagnitude < 0.04f) ResetLine();
                break;
            }
        }
        DrawLine();
    }

    // ---------- rod motion ----------

    void CapturePose()
    {
        if (hand == null || hand.Held == null) return;
        if (poseOwner == hand.Held) return;          // already have its normal pose
        poseOwner = hand.Held;
        holdPos = poseOwner.transform.localPosition; // grip sits on the socket origin
        holdRot = poseOwner.transform.localRotation;
        rodAngle = 0f;
    }

    void UpdateRodMotion(float dt)
    {
        if (poseOwner == null) return;

        if (Current == State.Charging)
        {
            float target = -windBackAngle * (1f - (1f - Power) * (1f - Power)); // eases out near full power
            rodAngle = Mathf.Lerp(rodAngle, target, 1f - Mathf.Exp(-windBackFollow * dt));
        }
        else if (swingT >= 0f && swingT < swingTime)
        {
            swingT += dt;
            float k = Mathf.Clamp01(swingT / swingTime);
            rodAngle = Mathf.Lerp(swingFrom, swingOvershoot, k * k);          // accelerating forward swing
            if (launchPending && rodAngle >= releaseAtAngle) { launchPending = false; Launch(); }
        }
        else
        {
            if (launchPending) { launchPending = false; Launch(); }            // safety: never lose a cast
            rodAngle = Mathf.Lerp(rodAngle, 0f, 1f - Mathf.Exp(-settleSpeed * dt));
            if (Mathf.Abs(rodAngle) < 0.05f) rodAngle = 0f;
        }
        ApplyRodPose();
    }

    void ApplyRodPose()
    {
        if (poseOwner == null) return;
        // rotate around the grip (socket origin) about the socket's sideways axis; negative = tip goes back
        Quaternion tilt = Quaternion.AngleAxis(-rodAngle, Vector3.left);
        poseOwner.transform.localRotation = tilt * holdRot;
        poseOwner.transform.localPosition = tilt * holdPos;
    }

    // ---------- bobber + line ----------

    void Launch()
    {
        Vector3 start = tip != null ? tip.position : transform.position + transform.forward * 0.5f;
        if (bobber == null)
        {
            var go = bobberPrefab != null ? Instantiate(bobberPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bobber";
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            if (bobberPrefab == null) go.transform.localScale = Vector3.one * 0.06f;
            bobber = go.transform;
        }
        bobber.position = start;
        Vector3 fwd = transform.forward; fwd.y = Mathf.Max(fwd.y, -0.2f);
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        Vector3 dir = Quaternion.AngleAxis(-launchAngle, right) * fwd.normalized;
        velocity = dir * Mathf.Lerp(minSpeed, maxSpeed, Power);
        timer = 0f;
        onWater = false;
        EnsureLine();
        Current = State.Flying;
    }

    void Land(RaycastHit hit)
    {
        onWater = hit.collider.gameObject.layer == LayerMask.NameToLayer("Water")
                  || hit.collider.name.ToLower().Contains("water") || hit.collider.name.ToLower().Contains("ocean");
        landedPos = hit.point + hit.normal * 0.02f;
        bobber.position = landedPos;
        velocity = Vector3.zero;
        Current = State.Landed;
    }

    void ResetLine()
    {
        if (bobber != null) Destroy(bobber.gameObject);
        bobber = null;
        if (line != null) line.enabled = false;
        if (powerBar != null) powerBar.SetActive(false);
        launchPending = false;
        Current = State.Idle;
        Power = 0f;
        tipOwner = null;
        tip = null;
    }

    void EnsureLine()
    {
        if (line == null)
        {
            var go = new GameObject("FishingLine");
            go.layer = 2;
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
        }
        line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        line.startWidth = lineWidth;
        line.endWidth = lineWidth * 1.6f;
        line.startColor = line.endColor = lineColor;
        line.positionCount = lineSegments + 1;
        line.enabled = true;
    }

    void DrawLine()
    {
        if (line == null || !line.enabled || bobber == null || tip == null) return;
        Vector3 a = tip.position, b = bobber.position;
        float len = Vector3.Distance(a, b);
        // keep the far end visible: a real 1 cm line is less than a pixel wide at 20 m
        line.endWidth = Mathf.Max(lineWidth * 1.6f, len * farWidthPerMetre);
        // taut while flying / reeling, a soft sag once it has landed.
        // The control point never goes below the lower end, so the curve never dips under the bobber or the ground.
        float want = Current == State.Landed ? len * slack : len * 0.015f;
        float sag = Mathf.Min(want, Mathf.Max(0f, Mathf.Abs(a.y - b.y) * 0.5f - 0.01f));
        Vector3 mid = (a + b) * 0.5f + Vector3.down * sag;
        for (int i = 0; i <= lineSegments; i++)
        {
            float t = i / (float)lineSegments;
            Vector3 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b; // quadratic curve
            line.SetPosition(i, p);
        }
    }

    /// <summary>Finds (or creates) the line attachment point at the tip of the rod.</summary>
    static Transform FindTip(Transform rod)
    {
        var existing = rod.GetComponentsInChildren<Transform>(true);
        foreach (var t in existing) if (t.name == "LineTip") return t;

        // the deepest Bend_ bone is the last segment; put the tip at the far end of the mesh along the rod
        Transform last = null, first = null;
        foreach (var t in existing)
        {
            if (!t.name.StartsWith("Bend_")) continue;
            if (first == null || string.CompareOrdinal(t.name, first.name) < 0) first = t;
            if (last == null || string.CompareOrdinal(t.name, last.name) > 0) last = t;
        }
        Vector3 tipWorld;
        var smr = rod.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && last != null && first != null)
        {
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            Vector3 axis = (last.position - first.position).normalized;
            float best = float.MinValue; tipWorld = last.position;
            foreach (var v in baked.vertices)
            {
                Vector3 w = smr.transform.position + smr.transform.rotation * v;
                float d = Vector3.Dot(w - first.position, axis);
                if (d > best) { best = d; tipWorld = w; }
            }
            Destroy(baked);
        }
        else
        {
            var r = rod.GetComponentInChildren<Renderer>();
            tipWorld = r != null ? r.bounds.center + Vector3.up * r.bounds.extents.y : rod.position;
        }
        var tipGo = new GameObject("LineTip");
        tipGo.transform.SetParent(last != null ? last : rod, true);
        tipGo.transform.position = tipWorld;
        return tipGo.transform;
    }
}
