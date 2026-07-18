using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowBehavior : ThrowBehavior
{
    private XRPullInteractable pullInteractable;
    [SerializeField]
    private float maxLaunchForce = 10;

    [SerializeField]
    private HandGraspDetector rightHand;
    [SerializeField]
    private HandGraspDetector leftHand;

    [SerializeField]
    private float pullThreashold = .4f;

    private float pullAmount;

    private void Awake()
    {
        pullInteractable = GetComponentInChildren<XRPullInteractable>();

        if (pullInteractable)
            pullInteractable.PullActionReleased += OnPullRelease;
    }

    private IEnumerator RotateWithVelocity(Rigidbody rb)
    {
        while(true)
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude > .01f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity, transform.up) * Quaternion.Euler(0f, 90f, 0f);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public override void OnRelease(SpellObject spell, SelectExitEventArgs args)
    {
        spell.rb.isKinematic = false;

        if (pullAmount < pullThreashold)
        {
            spell.rb.useGravity = true;
            return;
        }

        StartCoroutine(Launch(spell.rb));
    }

    private void OnPullRelease(float pullAmount)
    {
        this.pullAmount = pullAmount;
    }

    protected IEnumerator Launch(Rigidbody rb)
    {
        yield return null;

        if(pullInteractable == null || rb == null || rb.isKinematic) yield break;

        Vector3 forceToAdd = transform.TransformDirection(Vector3.left) * maxLaunchForce * pullAmount;

        rb.AddForce(forceToAdd, ForceMode.Impulse);

        Debug.Log("Launch - " + pullAmount);

        StartCoroutine(RotateWithVelocity(rb));
        StartCoroutine(Boost(rb));
    }

    public void ReleaseSecondHand(SelectExitEventArgs args)
    {
        if (pullAmount < pullThreashold)
            return;

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
        if (pullInteractable)
            pullInteractable.PullActionReleased -= OnPullRelease;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * maxLaunchForce);
    }
}
