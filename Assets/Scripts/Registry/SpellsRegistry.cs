using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry for all spells.
/// </summary>
[CreateAssetMenu(menuName = "Registries/Spells")]
public class SpellsRegistry : ScriptableObject
{
    public static SpellsRegistry Instance { get; private set; }

    void OnEnable()
    {
        Instance = this;
        Debug.Log($"[Registry] {GetType().Name} loaded — {_spells.Count} spells");
    }
    void OnDisable() => Instance = null;

    [SerializeField]
    private List<SpellSO> _spells = new();
    public IReadOnlyList<SpellSO> Spells => _spells;

#if UNITY_EDITOR
    [ContextMenu("Auto Populate")]
    public void AutoPopulate()
    {
        _spells.Clear();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SpellSO");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var spell = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellSO>(path);
            if (spell != null) _spells.Add(spell);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
