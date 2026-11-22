using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the player camera. Every frame it casts a ray from the centre of the screen;
/// if it hits an Interactable within range it shows the prompt, and E calls Interact().
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("How far away (metres) you can interact from.")]
    public float range = 3f;

    [Tooltip("Layers the ray can hit. Exclude the player's own layer.")]
    public LayerMask mask = ~(1 << 2); // everything except Ignore Raycast

    public KeyCode interactKey = KeyCode.E;

    [Tooltip("UI text that shows \"Press E to talk to ...\". Hidden when not looking at anything.")]
    public Text promptLabel;

    public Interactable Current { get; private set; }

    void Update()
    {
        Current = FindTarget();

        if (promptLabel != null)
        {
            bool show = Current != null;
            if (promptLabel.gameObject.activeSelf != show)
                promptLabel.gameObject.SetActive(show);
            if (show)
                promptLabel.text = Current.PromptText;
        }

        if (Current != null && Input.GetKeyDown(interactKey))
            Current.Interact();
    }

    Interactable FindTarget()
    {
        var ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<Interactable>();
        return null;
    }

    void OnDisable()
    {
        Current = null;
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * range);
    }
}
