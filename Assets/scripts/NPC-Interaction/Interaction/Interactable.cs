using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on anything the player can interact with (E key).
/// Needs a Collider on this object or a child so the PlayerInteractor raycast can hit it.
/// The actual interaction (dialogue, shop, ...) is hooked into OnInteract later.
/// </summary>
public class Interactable : MonoBehaviour
{
    [Tooltip("Name shown in the prompt, e.g. \"Mina\"")]
    public string npcName = "NPC";

    [Tooltip("Prompt text. {name} is replaced by npcName.")]
    public string promptFormat = "Press E to talk to {name}";

    [Tooltip("Called when the player presses E while looking at this object.")]
    public UnityEvent OnInteract = new UnityEvent();

    public string PromptText => promptFormat.Replace("{name}", npcName);

    public void Interact()
    {
        Debug.Log($"Interacting with {npcName}", this);
        OnInteract?.Invoke();
    }
}
