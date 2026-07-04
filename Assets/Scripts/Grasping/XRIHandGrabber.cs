using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class XRIHandGrabber
{
    private readonly XRDirectInteractor interactor;
    private readonly Transform attachPoint;

    public XRGrabInteractable SelectedInteractable { get; private set; }

    public XRIHandGrabber (XRDirectInteractor interactor, Transform hand)
    {
        this.interactor = interactor;

        attachPoint = new GameObject("Dynemic Attach Point").transform;
        attachPoint.SetParent(hand, false);

        this.interactor.attachTransform = attachPoint;
    }

    public bool IsHoldingSomething() => SelectedInteractable != null;

    public bool TryGrab(XRGrabInteractable interactable, UnityEvent<XRGrabInteractable> onGrab = null)
    {
        if (!interactor || !interactable)
            return false;

        if (SelectedInteractable == interactable)
            return false;

        if (SelectedInteractable != null)
            Release();

        AllignAttachPointToInteractable(interactable);
        interactor.StartManualInteraction((IXRSelectInteractable)interactable);

        SelectedInteractable = interactable;
        onGrab?.Invoke(interactable);
        return true;
    }

    public bool Release(UnityEvent<XRGrabInteractable> onRelease = null)
    {
        if (!interactor || !SelectedInteractable)
            return false;

        var released = SelectedInteractable;
        interactor.EndManualInteraction();
        SelectedInteractable = null;

        onRelease?.Invoke(released);
        return true;
    }

    public void ForceClearDestroyed(XRGrabInteractable interactable)
    {
        if (SelectedInteractable != interactable)
            return;

        if (interactor)
            interactor.EndManualInteraction();

        SelectedInteractable = null;
    }

    private void AllignAttachPointToInteractable(XRGrabInteractable interactable)
    {
        attachPoint.position = interactable.transform.position;
        attachPoint.rotation = interactable.transform.rotation;
    }
}
