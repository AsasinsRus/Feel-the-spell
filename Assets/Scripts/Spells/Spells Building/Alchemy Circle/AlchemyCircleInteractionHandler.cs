using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(AlchemyCircleVisual)), RequireComponent(typeof(AlchemyCircleSlotLayout))]
public class AlchemyCircleInteractionHandler : MonoBehaviour
{
    [HideInInspector]
    public AlchemyCircleVisual visual;
    [HideInInspector]
    public AlchemyCircleSlotLayout slotLayout;

    public event Action<Item> OnAnimationEnd;
    public event Action<Item> AfterItemGrabbed;

    [SerializeField]
    private float minAnimationVelocity = 5f;

    private Dictionary<Item, Coroutine> activeAnimations = new();

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
        if (!visual.isCircleVisible) return;
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
        if (!visual.isCircleVisible) return;
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

        TryStopAnimation(item);

        interactable.selectEntered.RemoveListener(OnItemGrabbed);
        interactable.selectExited.AddListener(OnItemReleaseAfterGrab);
        interactable.selectExited.AddListener(OnItemDrop);

        item.KeepInPlace = false;
        slotLayout.Remove(item);
        AnimateAllToSlots();
        
        AfterItemGrabbed?.Invoke(item);
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
        TryStopAnimation(item);

        Spline spline = BuildSpline(item, ref velocity);

        float duration = Mathf.Clamp(spline.GetLength() / Mathf.Max(velocity.magnitude, minAnimationVelocity), 0.3f, 2f);
        StartAnimation(item, spline, duration);
    }

    private void StartAnimation(Item item, Spline spline, float duration)
    {
        var animation = StartCoroutine(AnimateAlongSpline(item, spline, duration));
        activeAnimations.Add(item, animation);
    }

    private void TryStopAnimation(Item item)
    {
        if (activeAnimations.TryGetValue(item, out var existing))
            StopCoroutine(existing);
        activeAnimations.Remove(item);
    }

    private Spline BuildSpline(Item item, ref Vector3 velocity)
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

        return spline;
    }

    private IEnumerator AnimateAlongSpline(Item item, Spline spline, float duration)
    {
        var rb = item.transform.GetComponent<Rigidbody>();

        item.KeepInPlace = false;

        float handoffPoint = .75f;
        float splineLengt = spline.GetLength();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            //float easedT = t <= .9f ? 1f - Mathf.Pow(1f - t, 3) : t;
            //float easedT = 1f - Mathf.Pow(1f - t, 3);

            //if(t < handoffPoint)
            //{
            //    easedT = t;
            //}
            //else
            //{
            //    float localT = (t - handoffPoint) / (1f - handoffPoint);
            //    float easedLocal = 1f - Mathf.Pow(1f - localT, 3f);

            //    easedT = handoffPoint + easedLocal * (1f - handoffPoint);
            //}

            float easedT = t * t * (3f - 2f * t);
            float startBias = Mathf.Lerp(1f, 3f, Mathf.Clamp01(duration / 1.5f));
            easedT = Mathf.Pow(easedT, 1f / startBias);

            spline.Evaluate(easedT, out float3 pos, out float3 tangent, out float3 up);

            item.transform.position = pos;
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, tangent, easedT);

            yield return null;
        }

        item.transform.position = slotLayout.GetWorldTarget(item);
        item.KeepInPlace = true;
        activeAnimations.Remove(item);
        
        OnAnimationEnd?.Invoke(item);
    }
}
