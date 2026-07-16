using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AlchemyCircleSlotLayout : MonoBehaviour
{
    /// <summary>
    /// Radius of the circle
    /// </summary>
    [SerializeField]
    private float radius = .5f;

    private Dictionary<Item, Vector3> slots = new();

    /// <summary>
    /// A dictionary that maps the items to their positions on the circle.
    /// </summary>
    public IReadOnlyDictionary<Item, Vector3> Slots => slots;

    /// <summary>
    /// Count of slots
    /// </summary>
    public int Count => slots.Count;

    /// <summary>
    /// Adds a new slot to the circle.
    /// </summary>
    /// <param name="item">The item that is added to a new slot</param>
    public void Add(Item item)
    {
        slots.Add(item, Vector3.zero);
        item.OnDestroy_ += Remove;

        Recalculate();
    }

    /// <summary>
    /// Removes the slot that has the item
    /// </summary>
    /// <param name="item">The item the slot of which needs to be removed</param>
    public void Remove(Item item)
    {
        if (!slots.ContainsKey(item)) return;

        item.OnDestroy_ -= Remove;
        slots.Remove(item);

        Recalculate();
    }

    /// <summary>
    /// Checks if there exists a slot with the item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(Item item) => slots.ContainsKey(item);

    /// <summary>
    /// Removes all slots
    /// </summary>
    public void Clear()
    {
        foreach (var item in slots.Keys.ToList())
            item.OnDestroy_ -= Remove;
        
        slots.Clear();
    }

    /// <summary>
    /// Gives the world coordinates of the item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Vector3 GetWorldTarget(Item item)
        => transform.TransformPoint(slots[item]);

    /// <summary>
    /// Calculates the vector betwen the item and its follower for speed calculation.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
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
