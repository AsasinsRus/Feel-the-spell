using System.Collections;
using UnityEngine;

public class Fireball : Spell
{
    [SerializeField]
    private GameObject afterHit;

    private void Start()
    {
        OnHit += onHit;
    }

    private void onHit()
    {
        var fireOnGround = Instantiate(afterHit, new Vector3(transform.position.x, 0.1f, transform.position.z), afterHit.transform.rotation);
        
        fireOnGround.GetComponent<DestroyAnim>().Destroy();
    }
}
