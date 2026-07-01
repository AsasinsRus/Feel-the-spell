using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ThrowBehavior : MonoBehaviour, ISpellBehaviour
{
    [SerializeField]
    private float forceMultiplier = 2.5f;

    [SerializeField]
    private float hitCollisionRadius = 2.5f;

    private SpellObject spell;

    public void OnPickUp(SpellObject spell)
    {
        this.spell = spell;
    }

    public void OnRelease(SpellObject spell, SelectExitEventArgs args)
    {
        spell.rb.useGravity = true;
        spell.rb.isKinematic = false;

        StartCoroutine(Boost(spell.rb));
    }

    private IEnumerator Boost(Rigidbody rb)
    {
        yield return null;

        rb.linearVelocity *= forceMultiplier;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var hits = Physics.OverlapSphere(transform.position, hitCollisionRadius);

        spell.TriggerHit(hits);

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitCollisionRadius);
    }
}
