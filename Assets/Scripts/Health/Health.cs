using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.XR.CoreUtils.Collections;

public class Health : MonoBehaviour, IDamagable
{
    public int maxHealth;
    public int currentHealth;

    public event Action OnHealthChange;
    public event Action OnDie;

    [SerializeField]
    private List<Status> statuses = new();
    private List<Status> toRemoveStatuses = new();

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        StatusUpdate();
    }

    private void StatusUpdate()
    {
        foreach (var status in statuses)
        {
            status.Tic();
        }

        foreach (var status in toRemoveStatuses)
            statuses.Remove(status);

        toRemoveStatuses.Clear();
    }

    public void AddStatus(Status status)
    {
        Debug.Log("New status was added to " + gameObject.name);

        status.OnStatusEnd += () => toRemoveStatuses.Add(status);
        statuses.Add(status);
    }

    public Status GetStatusBySource(GameObject source)
    {
        return statuses.Find(status => status.source == source);
    }

    public void Damage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChange?.Invoke();
    }

    public void Die()
    {
        OnDie?.Invoke();

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnHealthChange = null;
        OnDie = null;
    }
}
