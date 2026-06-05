using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Spell : MonoBehaviour
{
    public event Action OnHit;
    public event Action OnPickUp;

    public SpellSO spellSO;

    private Rigidbody rb;
    private XRGrabInteractable interactable;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<XRGrabInteractable>();

        rb.useGravity = false;
        rb.isKinematic = true;

        interactable.selectExited.AddListener(OnSelectExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnHit?.Invoke();

        Destroy(gameObject);
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        rb.useGravity = true;
        rb.isKinematic = false;
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        OnPickUp?.Invoke();
    }

    private void OnDestroy()
    {
        interactable.selectExited.RemoveAllListeners();
        interactable.selectEntered.RemoveAllListeners();

        OnHit = null;
        OnPickUp = null;
    }
}
