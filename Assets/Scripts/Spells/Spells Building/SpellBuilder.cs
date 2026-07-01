using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class SpellBuilder : MonoBehaviour
{
    [Header("Input info")]
    [SerializeField]
    private InputActionProperty activateSpellbuilder;

    [Header("Spell desk")]
    [SerializeField]
    private SpellSurface spellSurface;

    [HideInInspector]
    public GameObject lastCreatedSpell;

    private Dictionary<string, SpellSO> commandToSpell = new();

    public static SpellBuilder instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        foreach(var spell in SpellsRegistry.Instance.Spells)
        {
            commandToSpell.Add(spell.spellActivationCommand, spell);
        }
    }

    /// <summary>
    /// Tries to cast concrete spell using <paramref name="spellCastingCommand"/>.
    /// </summary>
    /// <param name="spellCastingCommand">Spell to cast</param>
    /// <returns>true - if spell was casted, false - otherwise</returns>
    public bool TryBuildSpell(string spellCastingCommand)
    {
        if(!commandToSpell.TryGetValue(spellCastingCommand, out SpellSO spellSO)) return false;
        if (!HasAllComponents(spellSurface, commandToSpell[spellCastingCommand])) return false;

        if (lastCreatedSpell)
            Destroy(lastCreatedSpell);

        lastCreatedSpell = Instantiate(spellSO.prefab, spellSurface.spellSpawnpoint.position, spellSurface.spellSpawnpoint.rotation);
        lastCreatedSpell.GetComponent<XRGrabInteractable>().selectEntered.AddListener(OnPickUp);

        Debug.Log("Creted " + spellSO.spellName);

        return true;
    }

    public bool TryDestroyCreatedSpell()
    {
        if (lastCreatedSpell)
        {
            Destroy(lastCreatedSpell);
            lastCreatedSpell = null;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to cast a spell using components on <see cref="spellSurface"/>.
    /// </summary>
    /// <returns>true - if spell was casted, false - otherwise</returns>
    public bool TryBuildSpell()
    {
        if(spellSurface == null) return false;

        foreach (SpellSO spellSO in SpellsRegistry.Instance.Spells)
        {
            if(HasAllComponents(spellSurface, spellSO))
            {
                Debug.Log("Creted " + spellSO.spellName);

                if(lastCreatedSpell)
                    Destroy(lastCreatedSpell);

                lastCreatedSpell = Instantiate(spellSO.prefab, spellSurface.spellSpawnpoint.position, spellSurface.spellSpawnpoint.rotation);
                lastCreatedSpell.GetComponent<XRGrabInteractable>().selectEntered.AddListener(OnPickUp);

                return true;
            }
            else
            {
                if(lastCreatedSpell)
                {
                    Destroy(lastCreatedSpell);
                    lastCreatedSpell = null;
                }
            }
        }

        return false;
    }

    private bool HasAllComponents(SpellSurface spellSurface, SpellSO spell)
    {
        var itemSOs = spellSurface.itemOnDesk.Select(x => x.ItemSO).ToList();

        if(spellSurface.itemOnDesk.Count != spell.materialComponents.Length) return false;

        if (spell.strictOrder)
        {
            for(int i = 0; i < spell.materialComponents.Length; i++)
            {

                if (itemSOs[i] != spell.materialComponents[i])
                    return false;
            }
        }
        else
        {
            List<ItemSO> spellItemSOs = new List<ItemSO>(spell.materialComponents);

            foreach(ItemSO itemSO in itemSOs)
            {
                if(spellItemSOs.Contains(itemSO))
                    spellItemSOs.Remove(itemSO);
                else return false;
            }

            if(spellItemSOs.Count > 0)
                return false;
        }

        return true;
    }

    private void OnPickUp(SelectEnterEventArgs args)
    {
        args.interactableObject.selectEntered.RemoveListener(OnPickUp);

        lastCreatedSpell = null;

        spellSurface.ConsumeItems();
    }
}
