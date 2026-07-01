using UnityEngine;

public class DamageOnHit : MonoBehaviour, IHitEffect
{
    public void Apply(Collider target, SpellObject spell)
    {
        if(target.TryGetComponent<IDamagable>(out var damagable))
        {
            damagable.Damage(spell.Data.damage);
        }
    }
}
