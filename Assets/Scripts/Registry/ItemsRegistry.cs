using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry for all items.
/// </summary>
[CreateAssetMenu(menuName = "Registries/Items")]
public class ItemsRegistry : ScriptableObject
{
    public static ItemsRegistry Instance { get; private set; }

    void OnEnable()
    {
        Instance = this;
        Debug.Log($"[Registry] {GetType().Name} loaded — {_items.Count} items");
    }
    void OnDisable() => Instance = null;

    [SerializeField]
    private List<ItemSO> _items = new();
    public IReadOnlyList<ItemSO> Items => _items;

#if UNITY_EDITOR
    [ContextMenu("Auto Populate")]
    public void AutoPopulate()
    {
        _items.Clear();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item != null) _items.Add(item);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
