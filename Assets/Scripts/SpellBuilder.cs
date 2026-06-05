using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class SpellBuilder : MonoBehaviour
{
    [Header("Input info")]
    [SerializeField]
    private InputActionProperty activateSpellbuilder;

    [Header("Spell desk")]
    [SerializeField]
    private SpellSurface spellSurface;

    private GameObject createdSpell;


    public bool TryBuildSpell()
    {
        if(spellSurface == null) return false;

        foreach (SpellSO spellSO in SpellsRegistry.Instance.Spells)
        {
            if(HasAllComponents(spellSurface, spellSO))
            {
                createdSpell = Instantiate(spellSO.prefab, spellSurface.spellSpawnpoint.position, spellSurface.spellSpawnpoint.rotation);
                createdSpell.GetComponent<Spell>().OnPickUp += OnPickUp;
            }
            else
            {
                if(createdSpell)
                {
                    Destroy(createdSpell);
                    createdSpell = null;
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

    private void OnPickUp()
    {
        createdSpell = null;

        foreach (Item item in spellSurface.itemOnDesk)
        {
            
            Destroy(item.gameObject);
        }

        spellSurface.itemOnDesk.Clear();
    }
}
