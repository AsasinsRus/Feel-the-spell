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

    private void OnCollisionEnter(Collision collision)
    {
        if (!destroyOnGround) return;

        if(collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
