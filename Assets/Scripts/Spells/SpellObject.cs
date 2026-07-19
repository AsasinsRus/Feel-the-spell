using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
/// <summary>
/// Describes the spell.
/// </summary>
public class SpellObject : MonoBehaviour
{
    [SerializeField]
    private SpellSO spellSO;

    /// <summary>
    /// Defines behaviour of the spell, for example the throwing behaviour.
    /// </summary>
    private ISpellBehaviour behaviour;

    /// <summary>
    /// Stores all spell effects that occur when spell hits a target.
    /// </summary>
    private IHitEffect[] hitEffects;

    protected XRBaseInteractable interactable;

    private HapticImpulsePlayer hapticImpulsePlayer;
    private VibrationProvider vibrationProvider;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        
        behaviour = GetComponent<ISpellBehaviour>();
        hitEffects = GetComponents<IHitEffect>();

        rb.useGravity = false;
        rb.isKinematic = true;

        interactable.selectEntered.AddListener(args => behaviour.OnPickUp(this));
        interactable.selectExited.AddListener(args => behaviour.OnRelease(this, args));

        interactable.selectEntered.AddListener(ActivateVibration);
        interactable.selectExited.AddListener(args => DisactivateVibration());
    }

    private void Update()
    {
        vibrationProvider?.Update();
    }

    private void ActivateVibration(SelectEnterEventArgs args)
    {
        hapticImpulsePlayer = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>()
            ?? args.interactorObject.transform.GetComponentInChildren<HapticImpulsePlayer>();

        if (hapticImpulsePlayer != null)
            vibrationProvider = new VibrationProvider
                (
                    spellSO.vibrationPattern,
                    spellSO.duration,
                    spellSO.loop,
                    new HapticImpulsePlayer[] { hapticImpulsePlayer }
                );
    }

    private void DisactivateVibration()
    {
        hapticImpulsePlayer = null;
        vibrationProvider = null;
    }

    /// <summary>
    /// Triggers all the hit effects on all the hit objects.
    /// </summary>
    /// <param name="colliders"></param>
    public void TriggerHit(Collider[] colliders)
    {
        foreach (Collider collider in colliders)
        {
            foreach(IHitEffect hitEffect in hitEffects)
            {
                hitEffect.Apply(collider, this);
            }
        }
    }

    public SpellSO Data => spellSO;
    public Rigidbody rb => GetComponent<Rigidbody>();
}
