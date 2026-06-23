using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AlchemyCircleSlotLayout : MonoBehaviour
{
    [SerializeField]
    private float radius = .5f;

    private Dictionary<Item, Vector3> slots = new();

    public IReadOnlyDictionary<Item, Vector3> Slots => slots;
    public int Count => slots.Count;

    public void Add(Item item)
    {
        slots.Add(item, Vector3.zero);
        item.OnDestroy_ += Remove;

        Recalculate();
    }

    public void Remove(Item item)
    {
        if (!slots.ContainsKey(item)) return;

        item.OnDestroy_ -= Remove;
        slots.Remove(item);

        Recalculate();
    }

    public bool Contains(Item item) => slots.ContainsKey(item);

    public void Clear()
    {
        foreach (var item in slots.Keys.ToList())
            item.OnDestroy_ -= Remove;
        
        slots.Clear();
    }

    public Vector3 GetWorldTarget(Item item)
        => transform.TransformPoint(slots[item]);

    public Vector3 GetVelocityHint(Item item)
    {
        var keys = slots.Keys.ToList();
        int index = keys.IndexOf(item);

        if(index < 0 || index >= keys.Count - 1) return Vector3.zero;

        return slots[keys[index + 1]] - slots[item];
    }

    private void Recalculate()
    {
        int i = 0;
        foreach (var item in slots.Keys.ToList())
            slots[item] = CirclePosition(radius, i++, Count);
    }

    private Vector3 CirclePosition(float radius, int index, int count)
    {
        float angle = (2 * Mathf.PI / count) * index;

        float x = radius * Mathf.Cos(angle);
        float z = radius * Mathf.Sin(angle);

        return new Vector3(x, 0, z);
    }
}
