using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowBehavior : ThrowBehavior
{
    private XRPullInteractable pullInteractable;
    [SerializeField]
    private float maxLaunchForce = 1000;

    [SerializeField]
    private HandGraspDetector rightHand;
    [SerializeField]
    private HandGraspDetector leftHand;

    private void Awake()
    {
        pullInteractable = GetComponentInChildren<XRPullInteractable>();
    }

    public override void OnRelease(SpellObject spell, SelectExitEventArgs args)
    {
        spell.rb.useGravity = true;
        spell.rb.isKinematic = false;

        StartCoroutine(Launch(spell.rb));
    }

    protected IEnumerator Launch(Rigidbody rb)
    {
        yield return null;

        if(pullInteractable == null || rb == null || rb.isKinematic) yield break;

        Vector3 forceToAdd = transform.TransformDirection(Vector3.left) * maxLaunchForce * pullInteractable.pullAmount;

        rb.AddForce(forceToAdd, ForceMode.Impulse);

        Debug.Log("Launch");

        StartCoroutine(Boost(rb));
    }

    public void ReleaseSecondHand(SelectExitEventArgs args)
    {

        var thisInteractable = GetComponent<XRBaseInteractable>();

        if (rightHand == null || leftHand == null || pullInteractable == null)
            return;

        leftHand.IgnoreInteractable.Add(pullInteractable);
        leftHand.IgnoreInteractable.Add(thisInteractable);
        leftHand.ForceRelease();

        rightHand.IgnoreInteractable.Add(pullInteractable);
        rightHand.IgnoreInteractable.Add(thisInteractable);
        rightHand.ForceRelease();
    }

    private void OnDestroy()
    {
        if(leftHand.IgnoreInteractable.Contains(pullInteractable))
            leftHand.IgnoreInteractable.Remove(pullInteractable);
        if(rightHand.IgnoreInteractable.Contains(pullInteractable))
            rightHand.IgnoreInteractable.Remove(pullInteractable);
    }
}
