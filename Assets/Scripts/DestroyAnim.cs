using System;
using System.Collections;
using UnityEngine;

public class DestroyAnim : MonoBehaviour, IDestructable
{
    [SerializeField]
    private float timeBeforeDestruction = 6;
    [SerializeField]
    private float timeBeforeAnim = 5;
    public void Destroy()
    {
        var particalSystems = GetComponentsInChildren<ParticleSystem>();

        StartCoroutine(WaitForSeconds(timeBeforeAnim, () =>
        {
            foreach (var ps in particalSystems)
            {
                var emission = ps.emission;

                emission.rateOverTime = 0;
            }
        }));

        Destroy(gameObject, timeBeforeDestruction);
    }

    private IEnumerator WaitForSeconds(float seconds, Action callback)
    {
        yield return new WaitForSeconds(seconds);

        callback?.Invoke();
    }
}
