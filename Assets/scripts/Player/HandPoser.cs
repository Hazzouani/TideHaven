using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bends the fingers of a VR hand model (VR_Hands.blend) with the controller buttons.
/// Put it on the hand's root object (palm centre, palm facing this object's -Y, fingers +Z).
/// - grip (0..1)    -> middle finger + thumb close
/// - trigger (0..1) -> index finger curls
/// Analog: half pressed = half bent. Angles match the poses made in Blender.
/// </summary>
public class HandPoser : MonoBehaviour
{
    [Tooltip("Grip button value 0..1. XRI Starter Assets: 'XRI Right Interaction/Select Value' (or Left).")]
    public InputActionProperty grip;
    [Tooltip("Trigger value 0..1. XRI Starter Assets: 'XRI Right Interaction/Activate Value' (or Left).")]
    public InputActionProperty trigger;

    [Tooltip("How quickly the fingers follow the buttons.")]
    public float smoothing = 18f;

    [Header("Curl in degrees (joint 1, joint 2)")]
    public Vector2 indexOpen = new Vector2(6f, 6f);
    public Vector2 indexTrigger = new Vector2(60f, 75f);
    public Vector2 middleOpen = new Vector2(9f, 9f);
    public Vector2 middleGrip = new Vector2(80f, 95f);
    public Vector2 thumbOpen = new Vector2(5f, 5f);
    public Vector2 thumbGrip = new Vector2(35f, 40f);

    [Header("Testing without a controller")]
    public bool useTestValues;
    [Range(0f, 1f)] public float testGrip;
    [Range(0f, 1f)] public float testTrigger;

    class Finger { public Transform b1, b2; public Quaternion r1, r2; public Vector3 axis; }
    Finger index, middle, thumb;
    float g, t;
    bool ready;

    public float CurrentGrip => g;
    public float CurrentTrigger => t;

    void Awake()
    {
        index = Make("Index", false);
        middle = Make("Middle", false);
        thumb = Make("Thumb", true);
        ready = index != null && middle != null && thumb != null;
        if (!ready) Debug.LogWarning("HandPoser: finger bones (Index_1/2, Middle_1/2, Thumb_1/2) not found under " + name, this);
    }

    Transform FindBone(string prefix)
    {
        foreach (var tr in GetComponentsInChildren<Transform>(true))
            if (tr.name.StartsWith(prefix)) return tr;
        return null;
    }

    Finger Make(string n, bool isThumb)
    {
        var b1 = FindBone(n + "_1"); var b2 = FindBone(n + "_2");
        if (b1 == null || b2 == null) return null;
        var f = new Finger { b1 = b1, b2 = b2, r1 = b1.localRotation, r2 = b2.localRotation };
        Vector3 dir = transform.InverseTransformDirection((b2.position - b1.position).normalized);
        Vector3 towards;
        if (isThumb)
        {
            // the thumb closes towards a point under the front of the palm
            Vector3 basePos = transform.InverseTransformPoint(b1.position);
            towards = new Vector3(0f, -0.03f, 0.05f) - basePos;
        }
        else towards = Vector3.down; // the palm side
        f.axis = Vector3.Cross(dir, towards).normalized;
        return f;
    }

    static float Read(InputActionProperty p)
    {
        var a = p.action;
        return a != null ? a.ReadValue<float>() : 0f;
    }

    void LateUpdate()
    {
        if (!ready) return;
        float gIn = useTestValues ? testGrip : Read(grip);
        float tIn = useTestValues ? testTrigger : Read(trigger);
        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        g = Mathf.Lerp(g, gIn, k);
        t = Mathf.Lerp(t, tIn, k);
        Apply(g, t);
    }

    /// <summary>Sets the finger pose directly (0..1 each). Also used for testing.</summary>
    public void Apply(float gripValue, float triggerValue)
    {
        if (!ready) return;
        Pose(index, Vector2.Lerp(indexOpen, indexTrigger, triggerValue));
        Pose(middle, Vector2.Lerp(middleOpen, middleGrip, gripValue));
        Pose(thumb, Vector2.Lerp(thumbOpen, thumbGrip, gripValue));
    }

    void Pose(Finger f, Vector2 angles)
    {
        Vector3 axisW = transform.TransformDirection(f.axis);
        f.b1.localRotation = Bend(f.b1, axisW, angles.x, f.r1);
        f.b2.localRotation = Bend(f.b2, axisW, angles.y, f.r2);
    }

    static Quaternion Bend(Transform bone, Vector3 axisWorld, float degrees, Quaternion rest)
    {
        Quaternion p = bone.parent != null ? bone.parent.rotation : Quaternion.identity;
        return Quaternion.Inverse(p) * Quaternion.AngleAxis(degrees, axisWorld) * p * rest;
    }
}
