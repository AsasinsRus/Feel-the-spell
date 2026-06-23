using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Mathematics;
using System.Collections;

[RequireComponent(typeof(AlchemyCircleVisual)), RequireComponent(typeof(AlchemyCircleSlotLayout))]
public class AlchemyCircleInteractionHandler : MonoBehaviour
{
    [HideInInspector]
    public AlchemyCircleVisual visual;
    [HideInInspector]
    public AlchemyCircleSlotLayout slotLayout;

    private void Awake()
    {
        visual = GetComponent<AlchemyCircleVisual>();
        slotLayout = GetComponent<AlchemyCircleSlotLayout>();
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            visual.Show();
        }
        else
        {
            foreach (Item item in slotLayout.Slots.Keys.ToList())
            {
                item.KeepInPlace = false;
                item.GetComponent<Rigidbody>().useGravity = true;
            }

            slotLayout.Clear();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<Item>(out var item)) return;
        if (slotLayout.Contains(item)) return;
        if (!other.TryGetComponent<XRGrabInteractable>(out var interactable)) return;

        if (interactable.isSelected)
            interactable.selectExited.AddListener(OnItemDrop);
        else
            EnqueueItem(interactable);

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<XRGrabInteractable>(out var interactable)) return;

        interactable.selectExited.RemoveListener(OnItemDrop);
    }

    private void OnItemDrop(SelectExitEventArgs args)
    {
        args.interactableObject.selectExited.RemoveListener(OnItemDrop);

        EnqueueItem(args.interactableObject as XRGrabInteractable);
    }

    private void EnqueueItem(XRGrabInteractable interatable)
    {
        Item item = interatable.transform.GetComponent<Item>();
        Rigidbody rb = interatable.transform.GetComponent<Rigidbody>();

        // disable physics for item
        Vector3 catchVelocity = rb.linearVelocity;
        rb.useGravity = false;

        // animate adding and add
        slotLayout.Add(item);
        AnimateAllToSlots();
        AnimateTo(item, catchVelocity);

        // if item will be took back
        interatable.selectEntered.AddListener(OnItemGrabbed);
    }

    private void OnItemGrabbed(SelectEnterEventArgs args)
    {
        Item item = args.interactableObject.transform.GetComponent<Item>();
        XRGrabInteractable interactable = args.interactableObject as XRGrabInteractable;

        interactable.selectEntered.RemoveListener(OnItemGrabbed);
        interactable.selectExited.AddListener(OnItemReleaseAfterGrab);

        item.KeepInPlace = false;
        slotLayout.Remove(item);
        AnimateAllToSlots();
    }

    private void OnItemReleaseAfterGrab(SelectExitEventArgs args)
    {
        args.interactableObject.selectExited.RemoveListener(OnItemReleaseAfterGrab);
        args.interactableObject.transform.GetComponent<Rigidbody>().useGravity = true;
    }

    private void AnimateAllToSlots()
    {
        foreach (var item in slotLayout.Slots.Keys.ToList())
            AnimateTo(item, slotLayout.GetVelocityHint(item));
    }

    private void AnimateTo(Item item, Vector3 velocity)
    {
        Spline spline = new Spline();

        BezierKnot startingKnot = new BezierKnot(item.transform.position);
        float tangentStrength = velocity.magnitude * .3f;
        startingKnot.TangentIn = (float3)(velocity.normalized * tangentStrength);
        startingKnot.TangentOut = -(float3)(velocity.normalized * tangentStrength);

        BezierKnot endKnot = new BezierKnot(slotLayout.GetWorldTarget(item));
        endKnot.TangentIn = new float3(0, -0.3f, 0);
        endKnot.TangentOut = new float3(0, -0.3f, 0);

        spline.Add(startingKnot, TangentMode.AutoSmooth);
        spline.Add(endKnot, TangentMode.AutoSmooth);

        StartCoroutine(AnimateAlongSpline(item, spline, Mathf.Clamp(velocity.magnitude * .2f, .5f, 2f)));
    }

    private IEnumerator AnimateAlongSpline(Item item, Spline spline, float duration)
    {
        item.KeepInPlace = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);
            item.transform.position = pos;
            
            yield return null;
        }

        item.KeepInPlace = true;
    }
}
