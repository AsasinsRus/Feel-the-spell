using UnityEngine;

public class SpawnOnGroundOnHit : MonoBehaviour, IHitEffect
{
    [SerializeField]
    private GameObject toSpawn;
    [SerializeField]
    private float groundLevel = .1f;
    public void Apply(Collider target, SpellObject spell)
    {
        Vector3 groundLevelCoord = new Vector3(transform.position.x, groundLevel, transform.position.z);

        Instantiate(toSpawn, groundLevelCoord, toSpawn.transform.rotation);
    }
}
