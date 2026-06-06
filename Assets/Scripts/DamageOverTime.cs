using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class DamageOverTime : MonoBehaviour
{
    [SerializeField]
    private float exitCooldown = 3;

    [SerializeField]
    private float cooldown = 1;

    [SerializeField]
    private int damage = 5;

    private void Start()
    {
        var collider = GetComponent<SphereCollider>();

        foreach (var c in Physics.OverlapSphere(collider.transform.position, collider.radius))
        {
            OnCollisionEnter_(c.gameObject);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionEnter_(collision.gameObject);
    }

    private void OnCollisionEnter_(GameObject gameObject)
    {
        // has to be remade with IDamagable, maybe create IStatusable
        if (gameObject.TryGetComponent(typeof(Health), out var component))
        {
            var health = component as Health;

            var existedStatus = health.GetStatusBySource(gameObject);

            if (existedStatus != null)
            {
                existedStatus.exitCooldownStarted = false;
                existedStatus.exitTimer = 0;
            }
            else
            {
                health.AddStatus(new Status
                    (
                        gameObject,
                        cooldown,
                        exitCooldown,
                        null,
                        () =>
                        {
                            health.Damage(damage);
                        }
                    ));
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        OnCollisionExit_(collision.gameObject);
    }

    private void OnCollisionExit_(GameObject gameObject)
    {
        // has to be remade with IDamagable, maybe create IStatusable
        if (gameObject.TryGetComponent(typeof(Health), out var component))
        {
            var health = component as Health;

            var existedStatus = health.GetStatusBySource(gameObject);

            Debug.LogWarning("On coll exit");

            existedStatus.exitCooldownStarted = true;
        }
    }

    private void OnDestroy()
    {
        var collider = GetComponent<SphereCollider>();

        foreach (var c in Physics.OverlapSphere(collider.transform.position, collider.radius))
        {
            OnCollisionExit_(c.gameObject);
        }
    }

}
