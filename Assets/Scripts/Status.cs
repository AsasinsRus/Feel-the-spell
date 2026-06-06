using System;
using UnityEngine;

public class Status
{
    public bool exitCooldownStarted;
    public float exitCooldown;
    public float exitTimer;
    public float cooldown;
    private float cooldownTimer;

    public event Action OnStatusEnd;
    public event Action OnStatusTic;

    public GameObject source;

    public Status(GameObject source, float cooldown, float exitCooldown, Action OnStatusEnd, Action OnStatusTic)
    {
        this.source = source;
        this.cooldown = cooldown;
        this.exitCooldown = exitCooldown;

        this.OnStatusEnd = OnStatusEnd;
        this.OnStatusTic = OnStatusTic;
    }

    public void Tic()
    {
        if(exitCooldownStarted) exitTimer += Time.deltaTime;

        if(exitTimer >= exitCooldown)
        {
            exitTimer = 0;
            OnStatusEnd?.Invoke();
            return;
        }

        cooldownTimer += Time.deltaTime;

        if(cooldownTimer >= cooldown)
        {
            cooldownTimer = 0;
            OnStatusTic?.Invoke();
        }
    }
}
