using System;
using UnityEngine;

/// <summary>
/// Describes item as an instance.
/// </summary>
public class Item : MonoBehaviour
{
    /// <summary>
    /// General item description.
    /// </summary>
    [SerializeField, Tooltip("Item description")]
    public ItemSO ItemSO;

    [SerializeField]
    public bool destroyOnGround;

    public event Action OnUpdate;
    public event Action<Item> OnDestroy_;

    private Vector3 place;
    private Quaternion rotation;

    private void Update()
    {
        OnUpdate?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!destroyOnGround) return;

        if(collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    public void KeepInPlace(bool keep, Vector3 place)
    {
        if(keep)
        { 
            this.place = place;
            this.rotation = transform.rotation;
            OnUpdate += KeepInPlace;
        }
        else
        {
            OnUpdate -= KeepInPlace;
        }
    }

    private void KeepInPlace()
    {
        transform.position = place;
        transform.rotation = rotation;
    }

    private void OnDestroy()
    {
        OnDestroy_?.Invoke(this);
    }
}
