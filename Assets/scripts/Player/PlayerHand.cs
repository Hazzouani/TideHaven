using UnityEngine;

/// <summary>
/// Put this on the player camera. Holds one PickupItem at a time on the HoldSocket
/// (right side of the view). Q puts the held item back where it came from.
/// </summary>
public class PlayerHand : MonoBehaviour
{
    [Tooltip("Where held items go. Move this child in the editor to change the hold position.")]
    public Transform holdSocket;

    public KeyCode putBackKey = KeyCode.Q;

    public PickupItem Held { get; private set; }

    void Awake()
    {
        if (holdSocket == null)
        {
            holdSocket = new GameObject("HoldSocket").transform;
            holdSocket.SetParent(transform, false);
            holdSocket.localPosition = new Vector3(0.4f, -0.45f, 0.7f);
        }
    }

    public void PickUp(PickupItem item)
    {
        if (item == null || item == Held) return;
        if (Held != null) Held.ReturnToOrigin(); // only one at a time
        Held = item;
        item.AttachTo(holdSocket);
    }

    public void PutBack()
    {
        if (Held == null) return;
        Held.ReturnToOrigin();
        Held = null;
    }

    void Update()
    {
        if (Held != null && Input.GetKeyDown(putBackKey))
            PutBack();
    }
}
