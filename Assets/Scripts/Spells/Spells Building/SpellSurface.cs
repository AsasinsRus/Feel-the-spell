using System.Collections.Generic;
using UnityEngine;

public class SpellSurface : MonoBehaviour
{
    [SerializeField]
    public List<Item> itemOnDesk;

    public Transform spellSpawnpoint;

    private SpellBuilder spellBuilder;

    private void Start()
    {
        spellBuilder = FindAnyObjectByType<SpellBuilder>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(typeof(Item), out var item))
        {
            itemOnDesk.Add(item as Item);

            spellBuilder.TryBuildSpell();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(typeof(Item), out var item))
        {
            itemOnDesk.Remove(item as Item);

            spellBuilder.TryBuildSpell();
        }
    }
}
