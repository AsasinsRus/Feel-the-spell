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
        var fireOnGround = Instantiate(afterHit, transform.position, afterHit.transform.rotation);
        
        fireOnGround.GetComponent<DestroyAnim>().Destroy();
    }
}
