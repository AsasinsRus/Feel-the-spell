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

    private bool keepInPlace = false;
    public bool KeepInPlace
    {
        get { return keepInPlace; }
        set
        {
            place = transform.position;
            rotation = transform.rotation;

            keepInPlace = value;
        }
    }

    private void Update()
    {
        OnUpdate?.Invoke();

        if(keepInPlace)
        {
            transform.position = place;
            transform.rotation = rotation;
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!destroyOnGround) return;

        if(collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        OnDestroy_?.Invoke(this);
    }
}
