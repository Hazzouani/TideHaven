using UnityEngine;

/// <summary>
/// The dialogue of one NPC. Create via Assets > Create > TideHaven > NPC Lines.
/// The scene NPC and its prefab can share the same asset, so you only edit lines in one place.
/// </summary>
[CreateAssetMenu(fileName = "NpcLines", menuName = "TideHaven/NPC Lines")]
public class NpcLines : ScriptableObject
{
    [Tooltip("Colour of the name in the dialogue box.")]
    public Color nameColor = new Color(1f, 0.8f, 0.4f);

    [Tooltip("Pitch of the voice blips. Lower = deeper voice.")]
    [Range(0.4f, 2f)] public float voicePitch = 1f;

    [Tooltip("Said the first time you talk to this NPC.")]
    [TextArea(2, 4)] public string greeting;

    [Tooltip("Said on every next talk, one after another (loops).")]
    [TextArea(2, 4)] public string[] lines;
}
