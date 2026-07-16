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
    private readonly Dictionary<FingerTouchData, HashSet<XRBaseInteractable>> touchedByFinger = new();
    private readonly HashSet<XRBaseInteractable> destroyRegistered = new();

    public Action<FingerContactEvent> onFingerTouch;
    public Action<FingerContactEvent> onFingerUntouch;
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

        var interactable = other.GetComponentInParent<XRBaseInteractable>();
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

        var interactable = other.GetComponentInParent<XRBaseInteractable>();
        if (!interactable) return;

        if (touchedByFinger[finger].Remove(interactable))
            onFingerUntouch?.Invoke(new FingerContactEvent(finger, interactable, other));
    }

    public bool TryGetFinger(GameObject colliderObject, out FingerTouchData finger)
        => fingerByColliderObject.TryGetValue(colliderObject, out finger);

    public IReadOnlyCollection<XRBaseInteractable> GetTouches(FingerTouchData finger)
        => touchedByFinger.TryGetValue(finger, out var interactables)
        ? interactables.ToArray() : Array.Empty<XRBaseInteractable>();

    public bool AddTouch(FingerTouchData finger, XRBaseInteractable interactable)
    {
        if(!touchedByFinger.TryGetValue(finger, out var interactables))
            return false;

        return interactables.Add(interactable);
    }

    public bool RemoveTouch(FingerTouchData finger, XRBaseInteractable interactable)
    {
        if (!touchedByFinger.TryGetValue(finger, out var interactables))
            return false;

        return interactables.Remove(interactable);
    }

    public bool IsTouching(FingerTouchData finger, XRBaseInteractable interactable)
        => touchedByFinger.TryGetValue(finger, out var interactables)
        && interactables.Contains(interactable);

    public bool AnyFingerTouches(XRBaseInteractable interactable)
        => touchedByFinger.Values.Any(interactables => interactables.Contains(interactable));

    public HashSet<XRBaseInteractable> GetAllTouches()
    {
        HashSet<XRBaseInteractable> result = new HashSet<XRBaseInteractable>();

        foreach (var interactables in touchedByFinger.Values)
            foreach(var interactable in interactables)
                result.Add(interactable);

        return result;
    }

    public void RemoveInteractableEverywhere(XRBaseInteractable interactable)
    {
        destroyRegistered.Remove(interactable);

        foreach (var interactables in touchedByFinger.Values)
            interactables.Remove(interactable);
    }

    public void RegisterDestroy(XRBaseInteractable interactable)
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
    public XRBaseInteractable Interactable { get; }
    public Collider OtherCollider { get; }

    public FingerContactEvent(FingerTouchData finger, XRBaseInteractable interactable, Collider otherCollider)
    {
        Finger = finger;
        Interactable = interactable;
        OtherCollider = otherCollider;
    }
}