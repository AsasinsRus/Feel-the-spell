using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Defines the throwing behaviour of spells.
/// </summary>
public class ThrowBehavior : MonoBehaviour, ISpellBehaviour
{
    /// <summary>
    /// The value by which the speed of spell is multiplied.
    /// </summary>
    [SerializeField]
    private float forceMultiplier = 2.5f;

    /// <summary>
    /// The radius of spell's effect.
    /// </summary>
    [SerializeField]
    private float hitCollisionRadius = 2.5f;

    /// <summary>
    /// Spell, for which the behaviour is defined
    /// </summary>
    private SpellObject spell;

/// <summary>
/// Saves the spell attribute.
/// </summary>
/// <param name="spell"> The spell to be saved. </param>
    public void OnPickUp(SpellObject spell)
    {
        this.spell = spell;
    }

    /// <summary>
    /// Throws the spell.
    /// </summary>
    /// <param name="spell">The spell </param>
    /// <param name="args"></param>
    public void OnRelease(SpellObject spell, SelectExitEventArgs args)
    {
        spell.rb.useGravity = true;
        spell.rb.isKinematic = false;

        StartCoroutine(Boost(spell.rb));
    }

    /// <summary>
    /// Boosts the speed of a thrown spell by multiplying the speed by forceMultiplier.
    /// </summary>
    /// <param name="rb">The Rigidbody of the spell </param>
    /// <returns></returns>
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

    /// <summary>
    /// Gizmos to see the area of impact of the spell.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitCollisionRadius);
    }
}
