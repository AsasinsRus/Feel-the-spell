using System.Collections.Generic;
using UnityEngine;

public class SpellSurface : MonoBehaviour
{
    [SerializeField]
    public List<Item> itemOnDesk;

    public Transform spellSpawnpoint;

    private SpellBuilder spellBuilder;

    [SerializeField]
    private MovementRecognizer movementRecognizer;

    [HideInInspector]
    public CircleAnimation circleAnimation;

    private void Start()
    {
        spellBuilder = FindAnyObjectByType<SpellBuilder>();
        circleAnimation = GetComponent<CircleAnimation>();
    }

    private void OnEnable()
    {
        movementRecognizer.OnRecognition.AddListener(OnRecognition);
    }

    private void OnDisable()
    {
        movementRecognizer.OnRecognition.RemoveAllListeners();
    }

    private void OnRecognition(string gestureClass, Vector3[] points)
    {
        if (gestureClass != "O") return;

        transform.position = GetCentroid(points);
        transform.rotation = GetRotationBasedOnTreeEquidistantPoints(points);

        circleAnimation.SetActive(false);
        circleAnimation.SetActive(true);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(typeof(Item), out var item))
        {
            itemOnDesk.Add(item as Item);

            circleAnimation.SetSpellReady(spellBuilder.TryBuildSpell());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(typeof(Item), out var item))
        {
            itemOnDesk.Remove(item as Item);

            circleAnimation.SetSpellReady(spellBuilder.TryBuildSpell());
        }
    }
}
