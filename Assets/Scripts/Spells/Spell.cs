//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Deprecated

//public class Spell : MonoBehaviour
//{
//    public event Action OnHit;
//    public event Action OnPickUp;

//    public SpellSO spellSO;

//    private Rigidbody rb;
//    protected XRGrabInteractable interactable;

//    [SerializeField]
//    private float hitCollisionRadius = 1;

//    [SerializeField]
//    private float forceMultiplier = 2.5f;

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//        interactable = GetComponent<XRGrabInteractable>();

//        rb.useGravity = false;
//        rb.isKinematic = true;

//        interactable.selectExited.AddListener(OnSelectExit);
//        interactable.selectEntered.AddListener(OnSelectEnter);
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        OnHit?.Invoke();

//        var collisions = Physics.OverlapSphere(transform.position, hitCollisionRadius);

//        foreach(var _collision in collisions)
//        {
//            if (_collision.gameObject.TryGetComponent(typeof(IDamagable), out var component))
//            {
//                Debug.Log(_collision.gameObject.name + " was damaged by " + spellSO.damage);

//                (component as IDamagable).Damage(spellSO.damage);
//            }
//        }

//        Destroy(gameObject);
//    }

//    private void OnSelectExit(SelectExitEventArgs args)
//    {
//        rb.useGravity = true;
//        rb.isKinematic = false;

//        StartCoroutine(ApplyMultpiplier());
//    }

//    private IEnumerator ApplyMultpiplier()
//    {
//        yield return null;

//        rb.linearVelocity *= forceMultiplier;
//    }

//    private void OnSelectEnter(SelectEnterEventArgs args)
//    {
//        OnPickUp?.Invoke();
//    }

//    private void OnDestroy()
//    {
//        interactable.selectExited.RemoveAllListeners();
//        interactable.selectEntered.RemoveAllListeners();

//        OnHit = null;
//        OnPickUp = null;
//    }

//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;
//        Gizmos.DrawWireSphere(transform.position, hitCollisionRadius);
//    }
//}
