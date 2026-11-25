using UnityEngine;

/// <summary>
/// Put this on the HoldSocket (child of the camera). Makes whatever you hold feel alive:
/// - look sway: it lags behind the mouse and springs back
/// - walk bob: figure-8 bob while walking, scaled by speed
/// - strafe lean: leans a bit when moving sideways
/// - jump/land: lifts while falling, dips on landing
/// - idle breathing: tiny up/down when standing still
/// </summary>
public class HeldItemSway : MonoBehaviour
{
    [Header("Look sway (mouse)")]
    [Tooltip("Degrees of lag per unit of mouse movement.")]
    public float swayAmount = 3f;
    public float maxSway = 8f;
    [Tooltip("How fast it springs back. Lower = floatier.")]
    public float swaySmooth = 7f;

    [Header("Walk bob")]
    public float walkSpeedForFullBob = 5f;
    public float bobFrequency = 9f;
    public float bobAmountX = 0.025f;
    public float bobAmountY = 0.02f;
    [Tooltip("Degrees the tip rocks while walking.")]
    public float bobRotation = 1.5f;

    [Header("Strafe lean")]
    public float strafeLean = 5f;

    [Header("Jump / land")]
    public float fallLift = 0.006f;
    public float maxFallOffset = 0.05f;

    [Header("Idle breathing")]
    public float idleAmount = 0.006f;
    public float idleSpeed = 1.6f;

    [Tooltip("Player's Rigidbody (found automatically).")]
    public Rigidbody body;
    [Tooltip("Player's movement script, used for the ground check (found automatically).")]
    public PlayerMovement mover;

    Vector3 basePos;
    Quaternion baseRot;
    Quaternion swayRot = Quaternion.identity;
    Vector3 offset;
    float bobTimer;
    float lean;

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        if (mover == null) mover = FindAnyObjectByType<PlayerMovement>();
        if (body == null && mover != null) body = mover.GetComponent<Rigidbody>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- movement info (in camera space, so "sideways" is relative to where you look)
        Vector3 vel = body != null ? body.linearVelocity : Vector3.zero;
        Vector3 flat = new Vector3(vel.x, 0f, vel.z);
        Vector3 localVel = transform.parent != null ? transform.parent.InverseTransformDirection(flat) : flat;
        float speed01 = Mathf.Clamp01(flat.magnitude / walkSpeedForFullBob);
        bool grounded = mover == null || mover.IsGrounded;

        // --- look sway: lag behind the mouse
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        float swayYaw = Mathf.Clamp(-mx * swayAmount, -maxSway, maxSway);
        float swayPitch = Mathf.Clamp(my * swayAmount, -maxSway, maxSway);

        // --- strafe lean
        float targetLean = Mathf.Clamp(-localVel.x / walkSpeedForFullBob, -1f, 1f) * strafeLean;
        lean = Mathf.Lerp(lean, targetLean, 8f * dt);

        // --- walk bob (figure 8)
        float bobWeight = grounded ? speed01 : 0f;
        if (bobWeight > 0.05f) bobTimer += dt * bobFrequency * Mathf.Lerp(0.7f, 1f, speed01);
        Vector3 bob = new Vector3(Mathf.Cos(bobTimer) * bobAmountX, Mathf.Sin(bobTimer * 2f) * bobAmountY, 0f) * bobWeight;
        float bobRot = Mathf.Sin(bobTimer) * bobRotation * bobWeight;

        // --- idle breathing (fades out while walking)
        Vector3 idle = new Vector3(0f, Mathf.Sin(Time.time * idleSpeed) * idleAmount, 0f) * (1f - speed01);

        // --- jump / land: lifts while falling, dips when going up / landing
        float fall = grounded ? 0f : Mathf.Clamp(-vel.y * fallLift, -maxFallOffset, maxFallOffset);
        Vector3 jump = new Vector3(0f, fall, 0f);

        // --- smooth everything
        Quaternion targetRot = Quaternion.Euler(swayPitch, swayYaw + bobRot, lean);
        swayRot = Quaternion.Slerp(swayRot, targetRot, swaySmooth * dt);
        offset = Vector3.Lerp(offset, bob + idle + jump, 10f * dt);

        transform.localPosition = basePos + offset;
        transform.localRotation = baseRot * swayRot;
    }
}
