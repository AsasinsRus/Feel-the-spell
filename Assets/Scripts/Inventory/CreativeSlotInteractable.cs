using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CreativeSlotInteractable : XRGrabInteractable
{
    public GameObject prefab;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        CloneInHand(args.interactorObject);

        interactionManager.SelectExit(args.interactorObject, this);
    }

    private void CloneInHand(IXRSelectInteractor interactor)
    {
        var clone = Instantiate(prefab, interactor.transform.position, interactor.transform.rotation);

        if(clone.TryGetComponent(typeof(XRGrabInteractable), out var interactable))
        {
            interactionManager.SelectEnter(interactor, (interactable as XRGrabInteractable));
        }
    }
}
