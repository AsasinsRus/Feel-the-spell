using UnityEngine;
/// <summary>
/// Spawns a game object (for example animation) after spell hits the target
/// </summary>
public class SpawnOnHit : MonoBehaviour, IHitEffect
{
    [SerializeField]
    private GameObject toSpawn;
    public void Apply(Collider target, SpellObject spell)
    {
        Instantiate(toSpawn, transform.position, toSpawn.transform.rotation);
    }
}
