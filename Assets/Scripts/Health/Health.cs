using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamagable
{
    public int maxHealth;
    public int currentHealth;
    public event Action OnHealthChange;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    public void Damage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChange?.Invoke();
    }
}
