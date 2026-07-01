using UnityEngine;

[CreateAssetMenu(fileName = "SpellSO", menuName = "Scriptable Objects/SpellSO")]
public class SpellSO : ScriptableObject
{
    public string spellName;
    public string description;

    public string spellActivationCommand;

    public int damage;

    public ItemSO[] materialComponents;
    public bool strictOrder;

    public GameObject prefab;
}
