using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Plays the fish flop animations without needing an Animator Controller.
// Put this on the fish (the object that has the Animator), drag in the two clips
// from the fish's FBX, then call OnFloor() / OnRod() / StopFlopping() from your own code.
[RequireComponent(typeof(Animator))]
public class FishFlop : MonoBehaviour
{
    public enum State { None, Floor, Rod }

    [Header("Clips (from the fish FBX)")]
    public AnimationClip floorClip;   // Flop_Floor
    public AnimationClip rodClip;     // Flop_Rod

    [Header("Settings")]
    public State startState = State.None;
    [Range(0.1f, 3f)] public float speed = 1f;
    [Tooltip("Seconds to blend from one animation to the other")]
    public float blendTime = 0.25f;
    [Tooltip("Start at a random point so a group of fish doesn't move in sync")]
    public bool randomStart = true;

    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    float floorWeight, rodWeight;
    float floorTarget, rodTarget;

    public State Current { get; private set; } = State.None;

    void Awake()
    {
        var animator = GetComponent<Animator>();
        animator.applyRootMotion = false;

        graph = PlayableGraph.Create(name + "_FishFlop");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(graph, "Fish", animator);

        mixer = AnimationMixerPlayable.Create(graph, 2);
        output.SetSourcePlayable(mixer);

        if (floorClip != null) Connect(floorClip, 0);
        if (rodClip != null) Connect(rodClip, 1);
    }

    void Connect(AnimationClip clip, int input)
    {
        var p = AnimationClipPlayable.Create(graph, clip);
        if (randomStart) p.SetTime(Random.Range(0f, clip.length));
        graph.Connect(p, 0, mixer, input);
        mixer.SetInputWeight(input, 0f);
    }

    void Start()
    {
        SetState(startState, instant: true);
    }

    public void OnFloor() => SetState(State.Floor);
    public void OnRod() => SetState(State.Rod);
    public void StopFlopping() => SetState(State.None);

    public void SetState(State state, bool instant = false)
    {
        Current = state;
        floorTarget = state == State.Floor ? 1f : 0f;
        rodTarget = state == State.Rod ? 1f : 0f;

        if (state != State.None && !graph.IsPlaying()) graph.Play();

        if (instant)
        {
            floorWeight = floorTarget;
            rodWeight = rodTarget;
            ApplyWeights();
        }
    }

    void Update()
    {
        if (!graph.IsValid()) return;

        float step = blendTime <= 0f ? 1f : Time.deltaTime / blendTime;
        floorWeight = Mathf.MoveTowards(floorWeight, floorTarget, step);
        rodWeight = Mathf.MoveTowards(rodWeight, rodTarget, step);
        ApplyWeights();

        for (int i = 0; i < mixer.GetInputCount(); i++)
        {
            var input = mixer.GetInput(i);
            if (input.IsValid()) input.SetSpeed(speed);
        }

        // nothing playing anymore -> stop the graph so the fish stays in its normal pose
        if (Current == State.None && floorWeight == 0f && rodWeight == 0f && graph.IsPlaying())
            graph.Stop();
    }

    void ApplyWeights()
    {
        mixer.SetInputWeight(0, floorWeight);
        mixer.SetInputWeight(1, rodWeight);
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
