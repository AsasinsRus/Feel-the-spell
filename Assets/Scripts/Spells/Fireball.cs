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
        Destroy(Instantiate(afterHit, transform.position, afterHit.transform.rotation), 5);
    }
}
