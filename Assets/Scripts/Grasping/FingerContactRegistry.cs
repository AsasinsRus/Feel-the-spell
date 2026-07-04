using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Handles fingers contact with <see cref="XRGrabInteractable"/>.
/// </summary>
public sealed class FingerContactRegistry
{
    private readonly Dictionary<GameObject, FingerTouchData> fingerByColliderObject = new();
    private readonly Dictionary<FingerTouchData, HashSet<XRGrabInteractable>> touchedByFinger = new();
    private readonly HashSet<XRGrabInteractable> destroyRegistered = new();

    private Action<FingerContactEvent> onFingerTouch;
    private Action<FingerContactEvent> onFingerUntouch;
    private readonly UnityAction<GameObject> onInteractableDestroyed;

    public IEnumerable<FingerTouchData> Fingers => touchedByFinger.Keys;

    public FingerContactRegistry(
        IEnumerable<FingerTouchData> fingers,
        Action<FingerContactEvent> onFingerTouch,
        Action<FingerContactEvent> onFingerUntouch,
        UnityAction<GameObject> onInteractableDestroyed)
    {
        this.onFingerTouch = onFingerTouch;
        this.onFingerUntouch = onFingerUntouch; 
        this.onInteractableDestroyed = onInteractableDestroyed;

        foreach(var finger in fingers)
        {
            if (finger == null || finger.fingerTipCollider == null)
                continue;

            fingerByColliderObject[finger.fingerTipCollider.gameObject] = finger;
            touchedByFinger[finger] = new();
        }
    }

    public void Bind()
    {
        foreach(var finger in touchedByFinger.Keys)
        {
            if (!finger.fingerTipCollider.TryGetComponent<ColliderEvents>(out var events))
                events = finger.fingerTipCollider.gameObject.AddComponent<ColliderEvents>();

            events.OnTriggerEnter_ += OnTouch;
            events.OnTriggerExit_ += OnUntouch;
        }
    }

    public void Unbind()
    {
        foreach (var finger in touchedByFinger.Keys)
        {
            if (!finger.fingerTipCollider.TryGetComponent<ColliderEvents>(out var events))
                continue;

            events.OnTriggerEnter_ -= OnTouch;
            events.OnTriggerExit_ -= OnUntouch;
        }
    }

    private void OnTouch(Collider other, GameObject source)
    {
        if (!fingerByColliderObject.TryGetValue(source, out var finger))
            return;

        var interactable = other.GetComponentInParent<XRGrabInteractable>();
        if (!interactable) return;

        if (touchedByFinger[finger].Add(interactable))
        {
            RegisterDestroy(interactable);
            onFingerTouch?.Invoke(new FingerContactEvent(finger, interactable, other));
        }
    }

    private void OnUntouch(Collider other, GameObject source)
    {
        if (!fingerByColliderObject.TryGetValue(source, out var finger))
            return;

        var interactable = other.GetComponentInParent<XRGrabInteractable>();
        if (!interactable) return;

        if (touchedByFinger[finger].Remove(interactable))
            onFingerUntouch?.Invoke(new FingerContactEvent(finger, interactable, other));
    }

    public bool TryGetFinger(GameObject colliderObject, out FingerTouchData finger)
        => fingerByColliderObject.TryGetValue(colliderObject, out finger);

    public IReadOnlyCollection<XRGrabInteractable> GetTouches(FingerTouchData finger)
        => touchedByFinger.TryGetValue(finger, out var interactables)
        ? interactables.ToArray() : Array.Empty<XRGrabInteractable>();

    public bool AddTouch(FingerTouchData finger, XRGrabInteractable interactable)
    {
        if(!touchedByFinger.TryGetValue(finger, out var interactables))
            return false;

        return interactables.Add(interactable);
    }

    public bool RemoveTouch(FingerTouchData finger, XRGrabInteractable interactable)
    {
        if (!touchedByFinger.TryGetValue(finger, out var interactables))
            return false;

        return interactables.Remove(interactable);
    }

    public bool IsTouching(FingerTouchData finger, XRGrabInteractable interactable)
        => touchedByFinger.TryGetValue(finger, out var interactables)
        && interactables.Contains(interactable);

    public bool AnyFingerTouches(XRGrabInteractable interactable)
        => touchedByFinger.Values.Any(interactables => interactables.Contains(interactable));

    public HashSet<XRGrabInteractable> GetAllTouches()
    {
        HashSet<XRGrabInteractable> result = new HashSet<XRGrabInteractable>();

        foreach (var interactables in touchedByFinger.Values)
            foreach(var interactable in interactables)
                result.Add(interactable);

        return result;
    }

    public void RemoveInteractableEverywhere(XRGrabInteractable interactable)
    {
        destroyRegistered.Remove(interactable);

        foreach (var interactables in touchedByFinger.Values)
            interactables.Remove(interactable);
    }

    public void RegisterDestroy(XRGrabInteractable interactable)
    {
        if (!destroyRegistered.Add(interactable))
            return;

        if(!interactable.TryGetComponent<DestroyNotifier>(out var notifier))
            notifier = interactable.gameObject.AddComponent<DestroyNotifier>();

        notifier.OnDestroy_.RemoveListener(onInteractableDestroyed);
        notifier.OnDestroy_.AddListener(onInteractableDestroyed);
    }
}

public readonly struct FingerContactEvent
{
    public FingerTouchData Finger { get; }
    public XRGrabInteractable Interactable { get; }
    public Collider OtherCollider { get; }

    public FingerContactEvent(FingerTouchData finger, XRGrabInteractable interactable, Collider otherCollider)
    {
        Finger = finger;
        Interactable = interactable;
        OtherCollider = otherCollider;
    }
}