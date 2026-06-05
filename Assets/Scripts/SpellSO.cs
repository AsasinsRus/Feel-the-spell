using UnityEngine;

[CreateAssetMenu(fileName = "SpellSO", menuName = "Scriptable Objects/SpellSO")]
public class SpellSO : ScriptableObject
{
    public string spellName;
    public string description;

    public int damage;

    public ItemSO[] materialComponents;
    public bool strictOrder;

    public GameObject prefab;
}
