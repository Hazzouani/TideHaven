using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRHapticTrigger : MonoBehaviour
{
    [Header("Haptische Instellingen")]
    [Range(0f, 1f)]
    [SerializeField] private float amplitude = 0.6f;
    [SerializeField] private float tijdsduur = 0.12f;

    private XRBaseInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, tijdsduur);
            Debug.Log($"[Haptics] Haptische impuls van {amplitude} gestuurd voor {tijdsduur}s.");
        }
    }

    // Handmatige trigger voor custom events (bv. socket locking)
    public void GeefImpuls(XRBaseInputInteractor interactor)
    {
        if (interactor != null)
        {
            interactor.SendHapticImpulse(amplitude, tijdsduur);
        }
    }
}
