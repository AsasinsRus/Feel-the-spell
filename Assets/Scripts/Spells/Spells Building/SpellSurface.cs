using System.Collections.Generic;
using UnityEngine;

public class SpellSurface : MonoBehaviour
{
    [SerializeField]
    public List<Item> itemOnDesk;

    public Transform spellSpawnpoint;

    [SerializeField]
    private MovementRecognizer movementRecognizer;

    [HideInInspector]
    public AlchemyCircleInteractionHandler circleInteractionHandler;

    private void Awake()
    {
        circleInteractionHandler = GetComponent<AlchemyCircleInteractionHandler>();
    }

    private void OnEnable()
    {
        movementRecognizer.OnRecognition.AddListener(OnRecognition);

        circleInteractionHandler.OnAnimationEnd += AddItem;
        circleInteractionHandler.AfterItemGrabbed += RemoveItem;
    }

    private void OnDisable()
    {
        movementRecognizer.OnRecognition.RemoveListener(OnRecognition);

        circleInteractionHandler.OnAnimationEnd -= AddItem;
        circleInteractionHandler.AfterItemGrabbed -= RemoveItem;
    }

    private void OnRecognition(string gestureClass, Vector3[] points)
    {
        if (gestureClass != "O") return;

        transform.position = GetCentroid(points);
        transform.rotation = GetRotationBasedOnTreeEquidistantPoints(points);

        circleInteractionHandler.SetActive(false);
        circleInteractionHandler.SetActive(true);   
    }

    private Vector3 GetCentroid(Vector3[] points)
    {
        Vector3 result = Vector3.zero;

        foreach (Vector3 point in points)
            result += point;

        result /= points.Length;

        return result;
    }

    private Quaternion GetRotationBasedOnTreeEquidistantPoints(Vector3[] points)
    {
        Vector3 a = points[0];
        Vector3 b = points[points.Length / 3];
        Vector3 c = points[2 * points.Length / 3];

        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        Vector3 centroid = (a + b + c) / 3f;
        Vector3 toA = (a - centroid).normalized;

        return Quaternion.LookRotation(toA, normal);
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.TryGetComponent(typeof(Item), out var item))
    //    {
    //        AddItem(item);
    //    }
    //}

    private void AddItem(Item item)
    {
        if(!itemOnDesk.Contains(item))
        {
            itemOnDesk.Add(item);

            circleInteractionHandler.visual.SetSpellReady(SpellBuilder.instance.TryBuildSpell());
        }
    }

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.TryGetComponent(typeof(Item), out var item))
    //    {
    //        RemoveItem(item);
    //    }
    //}

    private void RemoveItem(Item item)
    {
        itemOnDesk.Remove(item);

        circleInteractionHandler.visual.SetSpellReady(SpellBuilder.instance.TryBuildSpell());
    }

    public void ConsumeItems()
    {
        foreach (Item item in itemOnDesk)
        {
            Destroy(item.gameObject);
        }
        itemOnDesk.Clear();

        circleInteractionHandler.slotLayout.Clear();
        circleInteractionHandler.visual.SetSpellReady(false);
    }
}
