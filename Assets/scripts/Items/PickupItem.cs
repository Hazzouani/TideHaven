using UnityEngine;

/// <summary>
/// Makes an object pick-up-able with E (via Interactable + PlayerInteractor).
/// When picked up it is attached to the PlayerHand's hold socket like a fishing rod:
/// tip pointing forward/up, reel (the part that sticks out near the bottom) hanging underneath.
/// Q (or picking up something else) puts it back exactly where it came from.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class PickupItem : MonoBehaviour
{
    public string itemName = "Item";

    [Header("How it is held (relative to the camera)")]
    [Tooltip("Degrees the tip points up from straight ahead.")]
    public float holdPitch = 28f;
    [Tooltip("Degrees the tip turns left (-) or right (+). Slightly negative points it into the middle of the view.")]
    public float holdYaw = 4f;
    [Tooltip("Roll around the rod. 0 = reel straight down.")]
    public float holdRoll = 0f;

    [Tooltip("Where you grab it, as a fraction of its length from the butt (0 = butt, 1 = tip). Reel is at ~0.22.")]
    [Range(0f, 1f)] public float gripHeight = 0.25f;

    public bool IsHeld { get; private set; }

    Interactable interactable;
    Collider[] colliders;

    // where it came from
    Transform originParent;
    Vector3 originLocalPos;
    Quaternion originLocalRot;
    Vector3 originLocalScale;
    int originLayer;

    // shape info, in this object's local space
    Vector3 gripLocal;
    Quaternion shapeFrameLocal; // z = butt->tip, y = away from the reel

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.npcName = itemName;
        interactable.promptFormat = "Press E to pick up {name}";
        interactable.OnInteract.AddListener(OnInteracted);

        colliders = GetComponentsInChildren<Collider>(true);

        originParent = transform.parent;
        originLocalPos = transform.localPosition;
        originLocalRot = transform.localRotation;
        originLocalScale = transform.localScale;
        originLayer = gameObject.layer;

        AnalyseShape();
    }

    /// <summary>
    /// Works out (from the mesh, as it stands upright on the rack) which way is butt->tip,
    /// which side the reel sticks out, and where the grip is.
    /// </summary>
    /// <summary>World-space vertices of the (skinned or normal) mesh, in its current pose. Null if unavailable.</summary>
    Vector3[] GetWorldVertices()
    {
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && smr.sharedMesh != null)
        {
            var baked = new Mesh();
            smr.BakeMesh(baked, true); // includes bone pose + scale, relative to the renderer's position/rotation
            var v = baked.vertices;
            var t = smr.transform;
            for (int i = 0; i < v.Length; i++) v[i] = t.position + t.rotation * v[i];
            Destroy(baked);
            return v;
        }

        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
        {
            var v = mf.sharedMesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = mf.transform.TransformPoint(v[i]);
            return v;
        }
        return null;
    }

    void AnalyseShape()
    {
        Vector3 axisW = Vector3.up;          // standing upright: butt at the bottom
        Vector3 reelW = transform.forward;   // fallback
        Vector3 gripW = transform.position;
        var mf = GetComponentInChildren<MeshFilter>();
        Vector3[] w = GetWorldVertices();

        if (w != null && w.Length > 0)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < w.Length; i++)
            {
                minY = Mathf.Min(minY, w[i].y);
                maxY = Mathf.Max(maxY, w[i].y);
            }
            float h = maxY - minY;

            // rod centre line = average x/z of the (thin) top half
            Vector2 axis = Vector2.zero; int na = 0;
            foreach (var p in w) if (p.y > minY + h * 0.5f) { axis += new Vector2(p.x, p.z); na++; }
            if (na > 0) axis /= na;

            // reel = whatever sticks out furthest in the bottom 40%
            float maxD = 0f;
            foreach (var p in w)
                if (p.y < minY + h * 0.4f) maxD = Mathf.Max(maxD, (new Vector2(p.x, p.z) - axis).magnitude);
            Vector2 rd = Vector2.zero;
            foreach (var p in w)
            {
                if (p.y >= minY + h * 0.4f) continue;
                var o = new Vector2(p.x, p.z) - axis;
                if (o.magnitude > maxD * 0.6f) rd += o;
            }
            if (rd.sqrMagnitude > 1e-6f) { rd.Normalize(); reelW = new Vector3(rd.x, 0f, rd.y); }

            gripW = new Vector3(axis.x, minY + h * gripHeight, axis.y);
        }
        else
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                gripW = new Vector3(b.center.x, b.min.y + b.size.y * gripHeight, b.center.z);
            }
            if (mf != null && mf.sharedMesh != null && !mf.sharedMesh.isReadable)
                Debug.LogWarning(itemName + ": enable Read/Write on the model import settings for an exact hold.", this);
        }

        gripLocal = transform.InverseTransformPoint(gripW);
        Quaternion inv = Quaternion.Inverse(transform.rotation);
        shapeFrameLocal = Quaternion.LookRotation(inv * axisW, inv * -reelW);
    }

    void OnInteracted()
    {
        var hand = FindAnyObjectByType<PlayerHand>();
        if (hand != null) hand.PickUp(this);
        else Debug.LogWarning("No PlayerHand in the scene, can't pick up " + itemName, this);
    }

    public void AttachTo(Transform socket)
    {
        IsHeld = true;
        foreach (var c in colliders) c.enabled = false;
        SetLayer(2); // Ignore Raycast, so it never blocks the interaction ray

        transform.SetParent(socket, true); // keep world scale
        ApplyHoldPose(socket);
    }

    /// <summary>Re-applies the hold angle/position (call after changing the hold values at runtime).</summary>
    public void ApplyHoldPose(Transform socket)
    {
        // Done in the socket's LOCAL space, so sway on the socket never gets baked in.
        // Target frame: z = where the tip points, y = up (so the reel hangs down).
        transform.localRotation = Quaternion.Euler(-holdPitch, holdYaw, holdRoll) * Quaternion.Inverse(shapeFrameLocal);
        // Put the grip point exactly on the socket's origin.
        transform.localPosition = -(transform.localRotation * Vector3.Scale(transform.localScale, gripLocal));
    }

    public void ReturnToOrigin()
    {
        IsHeld = false;
        transform.SetParent(originParent, false);
        transform.localPosition = originLocalPos;
        transform.localRotation = originLocalRot;
        transform.localScale = originLocalScale;
        SetLayer(originLayer);
        foreach (var c in colliders) c.enabled = true;
    }

    void OnValidate()
    {
        // live-tweak the hold pose in Play mode from the Inspector
        if (Application.isPlaying && IsHeld && transform.parent != null)
            ApplyHoldPose(transform.parent);
    }

    void SetLayer(int layer)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
