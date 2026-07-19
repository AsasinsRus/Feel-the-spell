using UnityEngine;

[CreateAssetMenu(fileName = "SpellSO", menuName = "Scriptable Objects/SpellSO")]
public class SpellSO : ScriptableObject
{
    [Header("Spell Description")]
    public string spellName;
    public string description;

    [Header("Spell properties")]
    public string spellActivationCommand;

    public int damage;

    [Header("Spell creation")]
    public ItemSO[] materialComponents;
    public bool strictOrder;

    public GameObject prefab;

    [Header("Vibration")]
    public AnimationCurve vibrationPattern;
    public float duration;
    public bool loop;
}
