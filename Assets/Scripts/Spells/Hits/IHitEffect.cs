using UnityEngine;

public interface IHitEffect
{
    void Apply(Collider target, SpellObject spell);
}
