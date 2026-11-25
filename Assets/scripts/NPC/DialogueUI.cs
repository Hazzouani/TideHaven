using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The dialogue box at the bottom of the screen. Types the line out letter by letter
/// with a little "voice" blip (pitch per NPC), then hides after a while
/// or when you walk away from the NPC.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("References")]
    public GameObject panel;
    public Text nameLabel;
    public Text bodyLabel;
    [Tooltip("The player (or camera), used to close the box when you walk away.")]
    public Transform listener;

    [Header("Typing")]
    public float charsPerSecond = 38f;
    [Tooltip("Seconds the finished line stays on screen, plus a bit extra per character.")]
    public float holdTime = 2.5f;
    public float holdPerChar = 0.04f;
    [Tooltip("Close the box when you are this far from the NPC.")]
    public float closeDistance = 5f;

    [Header("Voice")]
    public bool voice = true;
    [Range(0f, 1f)] public float voiceVolume = 0.35f;
    [Tooltip("Play a blip every N letters.")]
    public int blipEvery = 2;

    public bool IsTyping { get; private set; }
    public Transform Speaker { get; private set; }

    string fullText = "";
    float shown;
    float hideAt;
    float pitch = 1f;
    int lastBlip = -1;
    AudioSource source;
    AudioClip blip;

    void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        blip = MakeBlip();
        if (panel != null) panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string speakerName, string text, Color nameColor, float voicePitch, Transform speaker)
    {
        Speaker = speaker;
        fullText = text ?? "";
        pitch = voicePitch;
        shown = 0f;
        lastBlip = -1;
        IsTyping = true;
        nameLabel.text = speakerName;
        nameLabel.color = nameColor;
        bodyLabel.text = "";
        panel.SetActive(true);
    }

    public void FinishTyping()
    {
        shown = fullText.Length;
        bodyLabel.text = fullText;
        IsTyping = false;
        hideAt = Time.time + holdTime + fullText.Length * holdPerChar;
    }

    public void Hide()
    {
        IsTyping = false;
        Speaker = null;
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Speaker == null || (listener != null && Vector3.Distance(listener.position, Speaker.position) > closeDistance))
        {
            Hide();
            return;
        }

        if (IsTyping)
        {
            shown += charsPerSecond * Time.deltaTime;
            int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(shown));
            bodyLabel.text = fullText.Substring(0, count);

            if (voice && count > 0 && count - 1 != lastBlip)
            {
                char c = fullText[count - 1];
                if ((count - 1) % Mathf.Max(1, blipEvery) == 0 && char.IsLetterOrDigit(c))
                {
                    source.pitch = pitch * Random.Range(0.88f, 1.12f);
                    source.PlayOneShot(blip, voiceVolume);
                }
                lastBlip = count - 1;
            }

            if (count >= fullText.Length) FinishTyping();
        }
        else if (Time.time >= hideAt)
        {
            Hide();
        }
    }

    // A short, soft "bup" sound, generated in code so no audio file is needed.
    static AudioClip MakeBlip()
    {
        const int rate = 44100;
        int samples = Mathf.RoundToInt(rate * 0.06f);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float k = (float)i / samples;
            float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 4f)) * (1f - k);
            float freq = 520f - 180f * k;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t);
            data[i] = wave * env * 0.5f;
        }
        var clip = AudioClip.Create("VoiceBlip", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
