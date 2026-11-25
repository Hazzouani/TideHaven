using UnityEngine;

/// <summary>
/// Put this next to an Interactable. Pressing E shows the NPC's next line in the DialogueUI.
/// Pressing E while the text is still typing finishes it right away.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class NpcDialogue : MonoBehaviour
{
    public NpcLines lines;

    Interactable interactable;
    bool greeted;
    int next;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnInteract.AddListener(Talk);
    }

    void OnDestroy()
    {
        if (interactable != null) interactable.OnInteract.RemoveListener(Talk);
    }

    public void Talk()
    {
        if (lines == null || DialogueUI.Instance == null) return;

        // Still typing this NPC's line? Then E just finishes it.
        if (DialogueUI.Instance.IsTyping && DialogueUI.Instance.Speaker == transform)
        {
            DialogueUI.Instance.FinishTyping();
            return;
        }

        string line = NextLine();
        if (string.IsNullOrEmpty(line)) return;
        DialogueUI.Instance.Show(interactable.npcName, line, lines.nameColor, lines.voicePitch, transform);
    }

    string NextLine()
    {
        if (!greeted && !string.IsNullOrEmpty(lines.greeting))
        {
            greeted = true;
            return lines.greeting;
        }
        greeted = true;
        if (lines.lines == null || lines.lines.Length == 0) return lines.greeting;
        string line = lines.lines[next % lines.lines.Length];
        next++;
        return line;
    }
}
