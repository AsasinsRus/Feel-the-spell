using UnityEngine;

public class SpawnOnHit : MonoBehaviour, IHitEffect
{
    [SerializeField]
    private GameObject toSpawn;
    public void Apply(Collider target, SpellObject spell)
    {
        Instantiate(toSpawn, transform.position, toSpawn.transform.rotation);
    }
}
