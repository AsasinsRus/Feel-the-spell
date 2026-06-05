using UnityEngine;

/// <summary>
/// General item description (not instance).
/// </summary>
[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public string description;

    public GameObject prefab;
}
