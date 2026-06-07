using UnityEngine;

public class DamageOnCollision : MonoBehaviour
{
    [SerializeField]
    private int damage = 20;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent(typeof(IDamagable), out var component))
        {
            var damagable = component as IDamagable;

            damagable.Damage(damage);
        }
    }
}
