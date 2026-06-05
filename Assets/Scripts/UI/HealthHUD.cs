using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [SerializeField]
    private Health health;

    [SerializeField]
    private RectTransform healthBarRectTransform;

    [SerializeField]
    private Canvas healthCanvas;

    [SerializeField]
    private Transform observer;

    private float originalBarWidth;

    private void Start()
    {
        health.OnHealthChange += OnHealthChange;
        originalBarWidth = healthBarRectTransform.sizeDelta.x;
    }

    private void Update()
    {
        var cam = Camera.main.transform;
        transform.LookAt(cam);
    }

    private void OnHealthChange()
    {
        float healthRatio = health.currentHealth / health.maxHealth;

        healthBarRectTransform.sizeDelta = new Vector2(healthRatio * originalBarWidth, healthBarRectTransform.sizeDelta.y);
    }
}
