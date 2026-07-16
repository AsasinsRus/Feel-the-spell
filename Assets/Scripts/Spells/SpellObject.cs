using UnityEngine;
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

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        
        behaviour = GetComponent<ISpellBehaviour>();
        hitEffects = GetComponents<IHitEffect>();

        rb.useGravity = false;
        rb.isKinematic = true;

        interactable.selectEntered.AddListener(args => behaviour.OnPickUp(this));
        interactable.selectExited.AddListener(args => behaviour.OnRelease(this, args));
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
