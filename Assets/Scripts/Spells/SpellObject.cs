using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SpellObject : MonoBehaviour
{
    [SerializeField]
    private SpellSO spellSO;

    private ISpellBehaviour behaviour;
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
